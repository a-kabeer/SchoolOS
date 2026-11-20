import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@/context/ThemeContext';
import { LocaleProvider, useLocale } from '@/context/LocaleContext';
import { AuthProvider, useAuth } from '@/context/AuthContext';
import { ToastProvider } from '@/context/ToastContext';
import { AppDataProvider } from '@/context/AppDataContext';
import { StudentsPage } from '@/pages/schoolos/StudentsPage';
import { tokenStore } from '@/auth/tokenStore';
import { schoolOsApi } from '@/api/client';

function renderWithProviders(ui: React.ReactElement) {
  return render(
    <MemoryRouter initialEntries={['/students']}>
      <LocaleProvider>
        <ThemeProvider>
          <AuthProvider>
            <ToastProvider>
              <AppDataProvider>{ui}</AppDataProvider>
            </ToastProvider>
          </AuthProvider>
        </ThemeProvider>
      </LocaleProvider>
    </MemoryRouter>
  );
}

function seedAuthenticatedUser(role: string, permissions: string[] = []) {
  tokenStore.setSession('test-access-token', 3600, {
    id: '01931a74-0000-7000-8000-000000000050',
    email: 'staff@schoolos.test',
    name: 'Test Staff',
    role,
    tenantId: '01931a74-0000-7000-8000-000000000001',
    campusId: null,
    mustChangePassword: false,
    permissions,
    preferredLanguage: 'en',
  });
}

describe('Phase 1 Student Lifecycle UI & Frontend Security', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    tokenStore.clearSession();
    window.localStorage.clear();
  });

  it('enforces default-deny for unknown roles and calls backend logout', async () => {
    seedAuthenticatedUser('UnknownExternalRole', []);
    const logoutSpy = vi.spyOn(schoolOsApi, 'logout').mockImplementation(async () => {
      tokenStore.clearSession();
    });

    function Probe() {
      const { can, hasPermission, logout } = useAuth();
      return (
        <div>
          <span data-testid="can-create">{String(can('create'))}</span>
          <span data-testid="has-students-create">{String(hasPermission('students:create'))}</span>
          <button type="button" onClick={() => void logout()}>
            Sign Out Probe
          </button>
        </div>
      );
    }

    renderWithProviders(<Probe />);
    expect(screen.getByTestId('can-create')).toHaveTextContent('false');
    expect(screen.getByTestId('has-students-create')).toHaveTextContent('false');

    fireEvent.click(screen.getByRole('button', { name: 'Sign Out Probe' }));
    await waitFor(() => expect(logoutSpy).toHaveBeenCalledTimes(1));
    expect(tokenStore.isAuthenticated()).toBe(false);
  });

  it('shows real error and retry state on API failure with zero fabricated fallback data', async () => {
    seedAuthenticatedUser('TenantAdmin', ['students:view', 'students:create', 'students:edit']);

    const listStudentsSpy = vi
      .spyOn(schoolOsApi, 'listCanonicalStudents')
      .mockRejectedValueOnce(new Error('Service temporarily unavailable'))
      .mockResolvedValueOnce([
        {
          id: 'std-1',
          schoolId: 'sch-1',
          campusId: 'cmp-1',
          admissionNumber: 'ADM-2026-101',
          firstName: 'Zara',
          lastName: 'Malik',
          dateOfBirth: '2015-06-14',
          admissionDate: '2026-04-01',
          status: 'Active',
        },
      ]);
    vi.spyOn(schoolOsApi, 'listSchools').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listAcademicYears').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listGradeLevels').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listSections').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listGuardians').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listHouseholds').mockResolvedValue([]);

    renderWithProviders(<StudentsPage />);

    // Error state appears without fake fallback names
    expect(await screen.findByRole('alert')).toHaveTextContent('Service temporarily unavailable');
    expect(screen.queryByText(/Zainab Fatima/i)).not.toBeInTheDocument();

    // Clicking Retry loads the real API student
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByText(/Zara Malik/i)).toBeInTheDocument();
    expect(listStudentsSpy).toHaveBeenCalledTimes(2);
  });

  it('supports student details, guardians with authority flags, enrollment history, and transfer confirmation', async () => {
    seedAuthenticatedUser('TenantAdmin', ['students:view', 'students:create', 'students:edit']);

    vi.spyOn(schoolOsApi, 'listCanonicalStudents').mockResolvedValue([
      {
        id: 'std-1',
        schoolId: 'sch-1',
        campusId: 'cmp-1',
        admissionNumber: 'ADM-2026-101',
        firstName: 'Zara',
        lastName: 'Malik',
        dateOfBirth: '2015-06-14',
        admissionDate: '2026-04-01',
        status: 'Active',
      },
    ]);
    vi.spyOn(schoolOsApi, 'listSchools').mockResolvedValue([
      { id: 'sch-1', tenantId: 't1', name: 'Crescent', code: 'CMS', isActive: true, campuses: [{ id: 'cmp-1', name: 'North', code: 'N' }] },
    ]);
    vi.spyOn(schoolOsApi, 'listAcademicYears').mockResolvedValue([
      { id: 'yr-1', schoolId: 'sch-1', code: '2026-27', name: 'AY 2026-27', startDate: '2026-04-01', endDate: '2027-03-31', isCurrent: true, isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listGradeLevels').mockResolvedValue([
      { id: 'gr-5', schoolId: 'sch-1', code: 'G5', name: 'Grade 5', sequenceOrder: 5, isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listSections').mockResolvedValue([
      { id: 'sec-a', schoolId: 'sch-1', campusId: 'cmp-1', academicYearId: 'yr-1', gradeLevelId: 'gr-5', code: '5A', name: 'Section A', isActive: true },
      { id: 'sec-b', schoolId: 'sch-1', campusId: 'cmp-1', academicYearId: 'yr-1', gradeLevelId: 'gr-5', code: '5B', name: 'Section B', isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listGuardians').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listHouseholds').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'getStudentDetail').mockResolvedValue({
      id: 'std-1',
      schoolId: 'sch-1',
      campusId: 'cmp-1',
      admissionNumber: 'ADM-2026-101',
      firstName: 'Zara',
      lastName: 'Malik',
      dateOfBirth: '2015-06-14',
      admissionDate: '2026-04-01',
      status: 'Active',
      guardians: [
        {
          id: 'link-1',
          guardianId: 'g-1',
          firstName: 'Imran',
          lastName: 'Malik',
          primaryPhone: '+923001110001',
          relationshipType: 'Father',
          isPrimaryContact: true,
          isEmergencyContact: true,
          hasLegalCustody: true,
          canCollectStudent: true,
          receivesAcademicNotices: true,
          receivesFinancialNotices: true,
        },
      ],
      enrollments: [
        {
          id: 'en-1',
          campusId: 'cmp-1',
          academicYearId: 'yr-1',
          gradeLevelId: 'gr-5',
          sectionId: 'sec-a',
          rollNumber: '5A-01',
          startDate: '2026-04-05',
          status: 'Active',
        },
      ],
      statusHistory: [
        { id: 'sh-1', toStatus: 'Active', effectiveDate: '2026-04-01', changedAtUtc: '2026-04-01T00:00:00Z' },
      ],
      households: [],
    });

    const transferSpy = vi.spyOn(schoolOsApi, 'transferStudent').mockResolvedValue({
      previousEnrollmentId: 'en-1',
      newEnrollmentId: 'en-2',
    });

    renderWithProviders(<StudentsPage />);

    // Select Zara Malik
    fireEvent.click(await screen.findByText(/Zara Malik/i));
    expect(await screen.findByText(/ADM-2026-101/i)).toBeInTheDocument();

    // Check Guardians detail tab shows guardian & authority badges
    const guardianTabs = screen.getAllByRole('tab', { name: 'Guardians' });
    fireEvent.click(guardianTabs[guardianTabs.length - 1]);
    expect(await screen.findByText(/Imran Malik/i)).toBeInTheDocument();
    expect(screen.getAllByText('Legal Custody').length).toBeGreaterThanOrEqual(1);

    // Check Enrollments & Transfer tab requires two-step confirmation before calling transferStudent
    fireEvent.click(screen.getByRole('tab', { name: 'Enrollments & Transfer' }));
    expect(await screen.findByText(/Section A \(5A\)/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Transfer Student' }));
    expect(transferSpy).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Confirm Transfer' }));
    await waitFor(() => expect(transferSpy).toHaveBeenCalledTimes(1));
  });

  it('switches to Urdu and Arabic with RTL document direction and translated headings', async () => {
    seedAuthenticatedUser('TenantAdmin', ['students:view']);
    vi.spyOn(schoolOsApi, 'listCanonicalStudents').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listSchools').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listAcademicYears').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listGradeLevels').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listSections').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listGuardians').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listHouseholds').mockResolvedValue([]);

    function LocaleHarness() {
      const { setLocale } = useLocale();
      return (
        <div>
          <button type="button" onClick={() => setLocale('ur')}>UR</button>
          <button type="button" onClick={() => setLocale('ar')}>AR</button>
          <StudentsPage />
        </div>
      );
    }

    renderWithProviders(<LocaleHarness />);
    expect(await screen.findByRole('heading', { name: 'Student Lifecycle' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'UR' }));
    expect(await screen.findByRole('heading', { name: 'طلبہ کا تعلیمی ریکارڈ' })).toBeInTheDocument();
    expect(document.documentElement.dir).toBe('rtl');

    fireEvent.click(screen.getByRole('button', { name: 'AR' }));
    expect(await screen.findByRole('heading', { name: 'دورة حياة الطالب' })).toBeInTheDocument();
    expect(document.documentElement.dir).toBe('rtl');
  });
});
