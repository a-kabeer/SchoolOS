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

export interface AcademicYearDto {
  id: string;
  schoolId: string;
  code: string;
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  isActive: boolean;
}

export interface GradeLevelDto {
  id: string;
  schoolId: string;
  code: string;
  name: string;
  sequenceOrder: number;
  isActive: boolean;
}

export interface SectionDto {
  id: string;
  schoolId: string;
  campusId: string;
  academicYearId: string;
  gradeLevelId: string;
  code: string;
  name: string;
  capacity?: number | null;
  isActive: boolean;
}

export interface StudentSummaryDto {
  id: string;
  schoolId: string;
  campusId: string;
  admissionNumber: string;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  dateOfBirth: string;
  gender?: string | null;
  admissionDate: string;
  status: string;
  sourceEnquiryId?: string | null;
}

export interface StudentGuardianLinkDto {
  id: string;
  guardianId: string;
  firstName: string;
  lastName: string;
  primaryPhone: string;
  email?: string | null;
  relationshipType: string;
  isPrimaryContact: boolean;
  isEmergencyContact: boolean;
  hasLegalCustody: boolean;
  canCollectStudent: boolean;
  receivesAcademicNotices: boolean;
  receivesFinancialNotices: boolean;
}

export interface StudentEnrollmentDto {
  id: string;
  campusId: string;
  academicYearId: string;
  gradeLevelId: string;
  sectionId?: string | null;
  rollNumber?: string | null;
  startDate: string;
  endDate?: string | null;
  status: string;
  previousEnrollmentId?: string | null;
  reason?: string | null;
}

export interface StudentStatusHistoryDto {
  id: string;
  fromStatus?: string | null;
  toStatus: string;
  reason?: string | null;
  effectiveDate: string;
  changedBy?: string | null;
  changedAtUtc: string;
}

export interface StudentHouseholdSummaryDto {
  householdId: string;
  householdCode: string;
  displayName: string;
  roleLabel: string;
  isBillingMember: boolean;
}

export interface StudentDetailDto extends StudentSummaryDto {
  notes?: string | null;
  guardians: StudentGuardianLinkDto[];
  enrollments: StudentEnrollmentDto[];
  statusHistory: StudentStatusHistoryDto[];
  households: StudentHouseholdSummaryDto[];
}

export interface GuardianDto {
  id: string;
  firstName: string;
  lastName: string;
  primaryPhone: string;
  secondaryPhone?: string | null;
  email?: string | null;
  occupation?: string | null;
  preferredChannel: string;
  isActive: boolean;
}

export interface HouseholdDto {
  id: string;
  householdCode: string;
  displayName: string;
  primaryGuardianId?: string | null;
  isActive: boolean;
  members: Array<{
    id: string;
    studentId?: string | null;
    guardianId?: string | null;
    roleLabel: string;
    isBillingMember: boolean;
  }>;
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

  async logout(): Promise<void> {
    try {
      await apiRequest<void>('/api/v1/auth/logout', {
        method: 'POST',
        skipAuthRefresh: true,
      });
    } catch {
      // Always clear local session even if server session was already expired/revoked
    } finally {
      tokenStore.clearSession();
    }
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

  // Organization Academic Structures
  listAcademicYears(schoolId?: string) {
    const qs = schoolId ? `?schoolId=${encodeURIComponent(schoolId)}` : '';
    return apiRequest<AcademicYearDto[]>(`/api/v1/organization/academics/years${qs}`);
  },

  createAcademicYear(body: { schoolId: string; code: string; name: string; startDate: string; endDate: string; isCurrent?: boolean }) {
    return apiRequest<AcademicYearDto>('/api/v1/organization/academics/years', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  listGradeLevels(schoolId?: string) {
    const qs = schoolId ? `?schoolId=${encodeURIComponent(schoolId)}` : '';
    return apiRequest<GradeLevelDto[]>(`/api/v1/organization/academics/grades${qs}`);
  },

  createGradeLevel(body: { schoolId: string; code: string; name: string; sequenceOrder: number }) {
    return apiRequest<GradeLevelDto>('/api/v1/organization/academics/grades', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  listSections(params?: { campusId?: string; academicYearId?: string; gradeLevelId?: string }) {
    const sp = new URLSearchParams();
    if (params?.campusId) sp.set('campusId', params.campusId);
    if (params?.academicYearId) sp.set('academicYearId', params.academicYearId);
    if (params?.gradeLevelId) sp.set('gradeLevelId', params.gradeLevelId);
    const qs = sp.toString() ? `?${sp.toString()}` : '';
    return apiRequest<SectionDto[]>(`/api/v1/organization/academics/sections${qs}`);
  },

  createSection(body: { campusId: string; academicYearId: string; gradeLevelId: string; code: string; name: string; capacity?: number | null }) {
    return apiRequest<SectionDto>('/api/v1/organization/academics/sections', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  // Canonical Student Lifecycle Module (/api/v1/students)
  listCanonicalStudents(params?: { search?: string; status?: string; campusId?: string; academicYearId?: string; gradeLevelId?: string }) {
    const sp = new URLSearchParams();
    if (params?.search) sp.set('search', params.search);
    if (params?.status) sp.set('status', params.status);
    if (params?.campusId) sp.set('campusId', params.campusId);
    if (params?.academicYearId) sp.set('academicYearId', params.academicYearId);
    if (params?.gradeLevelId) sp.set('gradeLevelId', params.gradeLevelId);
    const qs = sp.toString() ? `?${sp.toString()}` : '';
    return apiRequest<StudentSummaryDto[]>(`/api/v1/students${qs}`);
  },

  getStudentDetail(id: string) {
    return apiRequest<StudentDetailDto>(`/api/v1/students/${id}`);
  },

  registerStudent(body: {
    campusId: string;
    admissionNumber: string;
    firstName: string;
    middleName?: string;
    lastName: string;
    dateOfBirth: string;
    gender?: string;
    admissionDate: string;
    notes?: string;
  }) {
    return apiRequest<StudentSummaryDto>('/api/v1/students', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  updateStudent(id: string, body: { firstName: string; middleName?: string; lastName: string; dateOfBirth: string; gender?: string; notes?: string }) {
    return apiRequest<StudentSummaryDto>(`/api/v1/students/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    });
  },

  changeStudentStatus(id: string, body: { targetStatus: string; reason: string; effectiveDate?: string }) {
    return apiRequest<{ id: string; status: string }>(`/api/v1/students/${id}/status`, {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  listStudentEnrollments(id: string) {
    return apiRequest<StudentEnrollmentDto[]>(`/api/v1/students/${id}/enrollments`);
  },

  enrollStudent(id: string, body: { campusId: string; academicYearId: string; gradeLevelId: string; sectionId?: string | null; rollNumber?: string; startDate: string }) {
    return apiRequest<StudentEnrollmentDto>(`/api/v1/students/${id}/enrollments`, {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  transferStudent(id: string, body: {
    targetCampusId: string;
    academicYearId: string;
    targetGradeLevelId: string;
    targetSectionId?: string | null;
    effectiveDate?: string;
    rollNumber?: string;
    reason: string;
  }) {
    return apiRequest<{ previousEnrollmentId: string; newEnrollmentId: string }>(`/api/v1/students/${id}/transfer`, {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  listGuardians(search?: string) {
    const qs = search ? `?search=${encodeURIComponent(search)}` : '';
    return apiRequest<GuardianDto[]>(`/api/v1/students/guardians${qs}`);
  },

  createGuardian(body: { firstName: string; lastName: string; primaryPhone: string; secondaryPhone?: string; email?: string; occupation?: string; preferredChannel?: string }) {
    return apiRequest<GuardianDto>('/api/v1/students/guardians', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  linkStudentGuardian(studentId: string, body: {
    guardianId: string;
    relationshipType: string;
    isPrimaryContact: boolean;
    isEmergencyContact: boolean;
    hasLegalCustody: boolean;
    canCollectStudent: boolean;
    receivesAcademicNotices: boolean;
    receivesFinancialNotices: boolean;
  }) {
    return apiRequest<{ id: string }>(`/api/v1/students/${studentId}/guardians`, {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  unlinkStudentGuardian(studentId: string, guardianId: string) {
    return apiRequest<void>(`/api/v1/students/${studentId}/guardians/${guardianId}`, {
      method: 'DELETE',
    });
  },

  listHouseholds() {
    return apiRequest<HouseholdDto[]>('/api/v1/students/households');
  },

  createHousehold(body: { householdCode: string; displayName: string; primaryGuardianId?: string | null }) {
    return apiRequest<HouseholdDto>('/api/v1/students/households', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  addHouseholdMember(householdId: string, body: { studentId?: string | null; guardianId?: string | null; roleLabel?: string; isBillingMember?: boolean }) {
    return apiRequest<{ id: string }>(`/api/v1/students/households/${householdId}/members`, {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },
};
