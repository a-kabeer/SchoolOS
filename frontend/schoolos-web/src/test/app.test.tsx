import { beforeEach, describe, expect, it } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@/context/ThemeContext';
import { LocaleProvider } from '@/context/LocaleContext';
import { AuthProvider } from '@/context/AuthContext';
import { ToastProvider } from '@/context/ToastContext';
import { AppDataProvider } from '@/context/AppDataContext';
import { AppRoutes } from '@/App';
import { tokenStore } from '@/auth/tokenStore';

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
});
