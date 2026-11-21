import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@/context/ThemeContext';
import { LocaleProvider } from '@/context/LocaleContext';
import { AuthProvider } from '@/context/AuthContext';
import { ToastProvider } from '@/context/ToastContext';
import { AppDataProvider } from '@/context/AppDataContext';
import { AppRoutes } from '@/App';
import { tokenStore } from '@/auth/tokenStore';
import { refreshAccessToken } from '@/api/client';

function renderAt(route: string) {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <LocaleProvider>
        <ThemeProvider>
          <AuthProvider>
            <ToastProvider>
              <AppDataProvider>
                <AppRoutes />
              </AppDataProvider>
            </ToastProvider>
          </AuthProvider>
        </ThemeProvider>
      </LocaleProvider>
    </MemoryRouter>
  );
}

describe('SchoolOS Application Routes, Auth Guard, Onboarding & Branding', () => {
  beforeEach(() => {
    tokenStore.clearSession();
    window.localStorage.clear();
  });

  it('redirects unauthenticated requests on protected routes to Sign In and hides demo preview by default', () => {
    renderAt('/students');
    expect(screen.getByRole('heading', { name: /Sign in/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Enter Preview Session/i })).not.toBeInTheDocument();
  });

  it('renders public school onboarding request form without authentication', () => {
    renderAt('/request-school');
    expect(screen.getByRole('heading', { name: /Request a school/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Submit school request/i })).toBeInTheDocument();
  });

  it('allows authenticated Admin session to navigate to Branding with SVG guard', () => {
    tokenStore.setSession('test-jwt', 3600, {
      id: 'user-1',
      email: 'admin@schoolos.test',
      name: 'School Admin',
      role: 'TenantAdmin',
      tenantId: '01931a74-0000-7000-8000-000000000001',
      campusId: null,
      mustChangePassword: false,
      permissions: ['students:view', 'students:create', 'students:edit'],
      preferredLanguage: 'en',
    });

    renderAt('/org/branding');
    const fileInput = screen.getByLabelText(/Choose logo/i) as HTMLInputElement;
    const svgFile = new File(['<svg><script>alert(1)</script></svg>'], 'evil.svg', {
      type: 'image/svg+xml',
    });
    fireEvent.change(fileInput, { target: { files: [svgFile] } });
    expect(screen.getByRole('alert')).toHaveTextContent(/PNG, JPEG or WebP/i);
    cleanup();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    cleanup();
  });

  it('authenticates against the real backend LoginResponse contract (expiresIn + fullName) and navigates through ProtectedRoute to /admissions', async () => {
    const tenantId = '01931a74-0000-7000-8000-000000000001';
    const jwtPayload = btoa(
      JSON.stringify({
        sub: 'user-admin-1',
        tenant_id: tenantId,
        campus_id: 'campus-1',
        role: 'TenantAdmin',
      })
    )
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
    const fakeJwt = `eyJhbGciOiJIUzI1NiJ9.${jwtPayload}.sig`;

    const realBackendLoginResponse = {
      accessToken: fakeJwt,
      expiresIn: 900,
      user: {
        id: 'user-admin-1',
        email: 'admin@schoolos.local',
        fullName: 'School Administrator',
        tenantId,
        schoolId: 'school-1',
        activeCampusId: 'campus-1',
        userType: 'SchoolAdministrator',
        mustChangePassword: false,
        roles: ['TenantAdmin'],
        permissions: ['admissions:view', 'admissions:create', 'students:view'],
      },
    };

    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
      const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
      if (url.endsWith('/api/v1/auth/login')) {
        return new Response(JSON.stringify(realBackendLoginResponse), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        });
      }
      return new Response(JSON.stringify([]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    });

    renderAt('/auth/login');

    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'admin@schoolos.local' } });
    fireEvent.change(screen.getByLabelText(/^Password/i), { target: { value: 'ChangeMe#LocalDev2026!' } });
    fireEvent.change(screen.getByLabelText(/Tenant ID/i), { target: { value: tenantId } });
    fireEvent.click(screen.getByRole('button', { name: /Sign in/i }));

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Admissions/i })).toBeInTheDocument();
    });

    expect(screen.queryByRole('heading', { name: /^Sign in$/i })).not.toBeInTheDocument();
    expect(tokenStore.isAuthenticated()).toBe(true);
    expect(tokenStore.getUser()?.name).toBe('School Administrator');
    expect(tokenStore.getUser()?.tenantId).toBe(tenantId);
    expect(tokenStore.getUser()?.role).toBe('TenantAdmin');
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/auth/login',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          email: 'admin@schoolos.local',
          password: 'ChangeMe#LocalDev2026!',
          tenantId,
        }),
      })
    );
  });

  it('supports real backend LoginResponse contract (expiresIn + fullName) in refreshAccessToken and rejects invalid expiry', async () => {
    const tenantId = '01931a74-0000-7000-8000-000000000001';
    const jwtPayload = btoa(JSON.stringify({ sub: 'user-admin-1', tenant_id: tenantId, role: 'TenantAdmin' }))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
    const fakeJwt = `eyJhbGciOiJIUzI1NiJ9.${jwtPayload}.sig`;

    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          accessToken: fakeJwt,
          expiresIn: 900,
          user: {
            id: 'user-admin-1',
            email: 'admin@schoolos.local',
            fullName: 'School Administrator',
            tenantId,
            mustChangePassword: false,
            roles: ['TenantAdmin'],
            permissions: ['admissions:view'],
          },
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      )
    );

    const refreshed = await refreshAccessToken();
    expect(refreshed).toBe(true);
    expect(tokenStore.isAuthenticated()).toBe(true);
    expect(tokenStore.getUser()?.name).toBe('School Administrator');

    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          accessToken: fakeJwt,
          user: {
            id: 'user-admin-1',
            email: 'admin@schoolos.local',
            fullName: 'School Administrator',
            mustChangePassword: false,
          },
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      )
    );

    const invalidRefresh = await refreshAccessToken();
    expect(invalidRefresh).toBe(false);
    expect(tokenStore.isAuthenticated()).toBe(false);
  });
});
