import { decodeJwtClaims, tokenStore, type AuthSessionUser } from '@/auth/tokenStore';

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly details?: unknown;

  constructor(message: string, status: number, code?: string, details?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

export interface LoginRequestPayload {
  email: string;
  password: string;
  tenantId: string;
  campusId?: string;
}

export interface AuthTokenResponse {
  accessToken: string;
  expiresInSeconds: number;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    mustChangePassword: boolean;
    preferredLanguage?: string;
    permissions?: string[];
  };
}

export interface PublicSchoolRequestPayload {
  schoolName: string;
  contactName: string;
  contactEmail: string;
}

let inFlightRefresh: Promise<boolean> | null = null;
let currentLanguage: string = 'en';

export function setApiLanguage(lang: string): void {
  currentLanguage = lang;
}

function buildUserFromAuthResponse(resp: AuthTokenResponse): AuthSessionUser {
  const claims = decodeJwtClaims(resp.accessToken);
  const roleClaim = claims?.role ?? claims?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  const primaryRole = Array.isArray(roleClaim) ? String(roleClaim[0] ?? 'User') : String(roleClaim ?? 'User');
  const tenantId = typeof claims?.tenant_id === 'string' ? claims.tenant_id : null;
  const campusId = typeof claims?.campus_id === 'string' ? claims.campus_id : null;

  return {
    id: resp.user.id,
    email: resp.user.email,
    name: `${resp.user.firstName} ${resp.user.lastName}`.trim() || resp.user.email,
    role: primaryRole,
    tenantId,
    campusId,
    mustChangePassword: resp.user.mustChangePassword,
    permissions: resp.user.permissions ?? [],
    preferredLanguage: resp.user.preferredLanguage,
  };
}

export async function refreshAccessToken(): Promise<boolean> {
  if (inFlightRefresh) return inFlightRefresh;

  inFlightRefresh = (async () => {
    try {
      const response = await fetch('/api/v1/auth/refresh', {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          'Accept-Language': currentLanguage,
          'X-SchoolOS-Lang': currentLanguage,
        },
        body: JSON.stringify({}),
      });

      if (!response.ok) {
        tokenStore.clearSession();
        return false;
      }

      const payload = (await response.json()) as AuthTokenResponse;
      tokenStore.setSession(payload.accessToken, payload.expiresInSeconds, buildUserFromAuthResponse(payload));
      return true;
    } catch {
      return false;
    } finally {
      inFlightRefresh = null;
    }
  })();

  return inFlightRefresh;
}

tokenStore.registerRefreshHandler(refreshAccessToken);

export async function apiRequest<T>(
  path: string,
  options: RequestInit & { skipAuthRefresh?: boolean } = {}
): Promise<T> {
  const execute = async (): Promise<Response> => {
    const headers = new Headers(options.headers);
    if (!headers.has('Content-Type') && !(options.body instanceof FormData)) {
      headers.set('Content-Type', 'application/json');
    }
    headers.set('Accept-Language', currentLanguage);
    headers.set('X-SchoolOS-Lang', currentLanguage);

    const token = tokenStore.getAccessToken();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    return fetch(path, {
      ...options,
      headers,
      credentials: 'include',
    });
  };

  let response = await execute();
  if (response.status === 401 && !options.skipAuthRefresh && tokenStore.getAccessToken()) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      response = await execute();
    }
  }

  if (!response.ok) {
    let parsed: Record<string, unknown> | undefined;
    try {
      parsed = (await response.json()) as Record<string, unknown>;
    } catch {
      // ignore non-JSON error body
    }

    const code = typeof parsed?.code === 'string' ? parsed.code : undefined;
    const msg =
      (typeof parsed?.detail === 'string' && parsed.detail) ||
      (typeof parsed?.title === 'string' && parsed.title) ||
      (typeof parsed?.message === 'string' && parsed.message) ||
      `Request failed (${response.status})`;

    throw new ApiError(msg, response.status, code, parsed);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const schoolOsApi = {
  async login(payload: LoginRequestPayload): Promise<AuthSessionUser> {
    const data = await apiRequest<AuthTokenResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
      skipAuthRefresh: true,
    });
    const user = buildUserFromAuthResponse(data);
    tokenStore.setSession(data.accessToken, data.expiresInSeconds, user);
    return user;
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await apiRequest('/api/v1/auth/change-password', {
      method: 'POST',
      body: JSON.stringify({ currentPassword, newPassword }),
    });
    const current = tokenStore.getUser();
    const token = tokenStore.getAccessToken();
    if (current && token) {
      tokenStore.setSession(token, 900, { ...current, mustChangePassword: false });
    }
  },

  submitPublicSchoolRequest(payload: PublicSchoolRequestPayload) {
    return apiRequest<{ id: string; status: string; submittedAtUtc: string }>(
      '/api/v1/public/school-requests',
      { method: 'POST', body: JSON.stringify(payload), skipAuthRefresh: true }
    );
  },

  listSchoolRequests() {
    return apiRequest<Array<{ id: string; schoolName: string; contactName: string; contactEmail: string; status: string; submittedAtUtc: string }>>(
      '/api/v1/platform/school-requests'
    );
  },

  approveSchoolRequest(id: string, body: { slug: string; countryCode: string; dataRegion?: string }) {
    return apiRequest<{ id: string; tenantId: string; status: string }>(
      `/api/v1/platform/school-requests/${id}/approve`,
      { method: 'POST', body: JSON.stringify(body) }
    );
  },

  listCountries() {
    return apiRequest<Array<{ id: string; code: string; name: string; currencyCode: string; defaultLanguage: string; timeZoneId: string }>>(
      '/api/v1/platform/countries'
    );
  },

  listPlans() {
    return apiRequest<Array<{ id: string; code: string; name: string; description: string; moduleCount: number }>>(
      '/api/v1/platform/plans'
    );
  },

  listSubscriptions() {
    return apiRequest<Array<{ id: string; tenantId: string; countryCode: string; status: string; amount: number; currencyCode: string; endDate: string; modules: string[] }>>(
      '/api/v1/platform/subscriptions'
    );
  },

  listInvoices() {
    return apiRequest<Array<{ id: string; tenantId: string; invoiceNumber: string; currencyCode: string; subtotal: number; taxAmount: number; total: number; status: string }>>(
      '/api/v1/platform/invoices'
    );
  },

  listSchools() {
    return apiRequest<Array<{ id: string; tenantId: string; name: string; code: string; isActive: boolean; campuses: Array<{ id: string; name: string; code: string; address?: string }> }>>(
      '/api/v1/organization/schools'
    );
  },

  createSchool(body: { name: string; code: string }) {
    return apiRequest<{ id: string; name: string; code: string }>('/api/v1/organization/schools', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  upsertSchoolBranding(schoolId: string, body: { primaryColorHex: string; secondaryColorHex: string; accentColorHex: string }) {
    return apiRequest(`/api/v1/organization/schools/${schoolId}/branding`, {
      method: 'PUT',
      body: JSON.stringify(body),
    });
  },

  uploadBrandingImage(schoolId: string, slot: 'Logo' | 'Favicon', base64Data: string) {
    return apiRequest<{ id: string; slot: string; contentType: string; sha256Hash: string }>(
      `/api/v1/organization/schools/${schoolId}/branding/images/${slot}`,
      { method: 'PUT', body: JSON.stringify({ base64Data }) }
    );
  },

  listRoles() {
    return apiRequest<Array<{ id: string; schoolId: string; name: string; code: string }>>('/api/v1/identity/roles');
  },

  listUsers() {
    return apiRequest<Array<{ id: string; email: string; firstName: string; lastName: string; userType: string; status: string; preferredLanguage: string }>>(
      '/api/v1/identity/users'
    );
  },

  listEnquiries() {
    return apiRequest<Array<{ id: string; campusId: string; applicantName: string; guardianName: string; guardianPhone: string; targetGrade: string; academicYear: string; status: string; convertedStudentProfileId?: string }>>(
      '/api/v1/admissions/enquiries'
    );
  },

  createEnquiry(body: { campusId: string; applicantName: string; guardianName: string; guardianPhone: string; guardianEmail?: string; targetGrade: string; academicYear: string; notes?: string }) {
    return apiRequest<{ id: string; applicantName: string; status: string }>('/api/v1/admissions/enquiries', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  listStudents() {
    return apiRequest<Array<{ id: string; admissionNumber: string; firstName: string; lastName: string; targetGrade: string; status: string }>>(
      '/api/v1/admissions/students'
    );
  },
};
