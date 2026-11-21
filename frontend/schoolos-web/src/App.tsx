import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { ThemeProvider } from '@/context/ThemeContext';
import { LocaleProvider } from '@/context/LocaleContext';
import { AuthProvider } from '@/context/AuthContext';
import { ToastProvider } from '@/context/ToastContext';
import { AppDataProvider } from '@/context/AppDataContext';
import { AppLayout } from '@/components/layout';
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { Dashboard3Page } from '@/pages/dashboards/Dashboard3Page';
import { LoginPage } from '@/pages/auth/LoginPage';
import { RegisterPage } from '@/pages/auth/RegisterPage';
import { ForgotPasswordPage } from '@/pages/auth/ForgotPasswordPage';
import { ChangePasswordPage } from '@/pages/schoolos/ChangePasswordPage';
import { RequestSchoolPage } from '@/pages/schoolos/RequestSchoolPage';
import { PlatformAdminPage } from '@/pages/schoolos/PlatformAdminPage';
import { SchoolsManagementPage } from '@/pages/schoolos/SchoolsManagementPage';
import { SchoolBrandingPage } from '@/pages/schoolos/SchoolBrandingPage';
import { AccessControlPage } from '@/pages/schoolos/AccessControlPage';
import { AdmissionsPage } from '@/pages/schoolos/AdmissionsPage';
import { StudentsPage } from '@/pages/schoolos/StudentsPage';
import { AttendancePage } from '@/pages/schoolos/AttendancePage';
import { FeesPage } from '@/pages/schoolos/FeesPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/auth/login" element={<LoginPage />} />
      <Route path="/auth/register" element={<RegisterPage />} />
      <Route path="/auth/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/request-school" element={<RequestSchoolPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/auth/change-password" element={<ChangePasswordPage />} />

        <Route element={<AppLayout />}>
          <Route path="/" element={<Navigate to="/students" replace />} />
          <Route path="/students" element={<StudentsPage />} />
          <Route path="/attendance" element={<AttendancePage />} />
          <Route path="/fees" element={<FeesPage />} />
          <Route path="/admissions" element={<AdmissionsPage />} />
          <Route path="/org/schools" element={<SchoolsManagementPage />} />
          <Route path="/org/branding" element={<SchoolBrandingPage />} />
          <Route path="/org/access" element={<AccessControlPage />} />
          <Route path="/platform" element={<PlatformAdminPage />} />
          <Route path="/dashboards/dashboard3" element={<Dashboard3Page />} />
          <Route path="*" element={<Navigate to="/admissions" replace />} />
        </Route>
      </Route>
    </Routes>
  );
}

export default function App() {
  return (
    <BrowserRouter>
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
    </BrowserRouter>
  );
}
