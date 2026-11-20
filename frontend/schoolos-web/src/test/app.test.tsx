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

  it('redirects unauthenticated requests on protected routes to Sign In', () => {
    renderAt('/admissions');
    expect(screen.getByRole('heading', { name: /Sign in/i })).toBeInTheDocument();
  });

  it('renders public school onboarding request form without authentication', () => {
    renderAt('/request-school');
    expect(screen.getByRole('heading', { name: /Request a school/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Submit school request/i })).toBeInTheDocument();
  });

  it('allows entering interactive preview session and navigating to Admissions and Branding with SVG guard', () => {
    renderAt('/auth/login');
    fireEvent.click(screen.getByRole('button', { name: /Enter Preview Session/i }));
    expect(screen.getByRole('heading', { name: 'Admissions' })).toBeInTheDocument();

    cleanup();
    renderAt('/org/branding');
    const fileInput = screen.getByLabelText(/Choose logo/i) as HTMLInputElement;
    const svgFile = new File(['<svg><script>alert(1)</script></svg>'], 'evil.svg', {
      type: 'image/svg+xml',
    });
    fireEvent.change(fileInput, { target: { files: [svgFile] } });
    expect(screen.getByRole('alert')).toHaveTextContent(/PNG, JPEG or WebP/i);
  });
});
