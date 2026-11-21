import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@/context/ThemeContext';
import { LocaleProvider, useLocale } from '@/context/LocaleContext';
import { AuthProvider } from '@/context/AuthContext';
import { ToastProvider } from '@/context/ToastContext';
import { AppDataProvider } from '@/context/AppDataContext';
import { AttendancePage } from '@/pages/schoolos/AttendancePage';
import { tokenStore } from '@/auth/tokenStore';
import { schoolOsApi } from '@/api/client';

function renderWithProviders(ui: React.ReactElement) {
  return render(
    <MemoryRouter initialEntries={['/attendance']}>
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

function seedAdmin() {
  tokenStore.setSession('test-jwt', 3600, {
    id: '01931a74-0000-7000-8000-000000000099',
    email: 'admin@schoolos.test',
    name: 'Admin',
    role: 'TenantAdmin',
    tenantId: '01931a74-0000-7000-8000-000000000001',
    campusId: null,
    mustChangePassword: false,
    permissions: ['attendance:view', 'attendance:create', 'staff:view', 'staff:create', 'staff:edit'],
    preferredLanguage: 'en',
  });
}

describe('Phase 1C Daily Attendance & Staff UI', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    tokenStore.clearSession();
    window.localStorage.clear();
  });

  it('shows real error and retry state when loading fails without fabricating records', async () => {
    seedAdmin();
    const listSchoolsSpy = vi
      .spyOn(schoolOsApi, 'listSchools')
      .mockRejectedValueOnce(new Error('Attendance API unreachable'))
      .mockResolvedValueOnce([]);
    vi.spyOn(schoolOsApi, 'listAcademicYears').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listGradeLevels').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listSections').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listStaffMembers').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listTeacherAssignments').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listAttendanceCorrections').mockResolvedValue([]);

    renderWithProviders(<AttendancePage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Attendance API unreachable');

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await waitFor(() => expect(listSchoolsSpy).toHaveBeenCalledTimes(2));
  });

  it('loads section roster, marks attendance in bulk, and supports Urdu/Arabic RTL', async () => {
    seedAdmin();
    vi.spyOn(schoolOsApi, 'listSchools').mockResolvedValue([
      { id: 'sch-1', tenantId: 't1', name: 'Al-Noor', code: 'ANA', isActive: true, campuses: [{ id: 'cmp-1', name: 'City', code: 'C' }] },
    ]);
    vi.spyOn(schoolOsApi, 'listAcademicYears').mockResolvedValue([
      { id: 'yr-1', schoolId: 'sch-1', code: '2026-27', name: 'AY 2026-27', startDate: '2026-04-01', endDate: '2027-03-31', isCurrent: true, isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listGradeLevels').mockResolvedValue([
      { id: 'gr-5', schoolId: 'sch-1', code: 'G5', name: 'Grade 5', sequenceOrder: 5, isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listSections').mockResolvedValue([
      { id: 'sec-a', schoolId: 'sch-1', campusId: 'cmp-1', academicYearId: 'yr-1', gradeLevelId: 'gr-5', code: '5A', name: 'Grade 5 - A', isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listStaffMembers').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listTeacherAssignments').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listAttendanceCorrections').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'getSectionAttendanceRegister').mockResolvedValue({
      registerId: null,
      campusId: 'cmp-1',
      academicYearId: 'yr-1',
      gradeLevelId: 'gr-5',
      sectionId: 'sec-a',
      sectionName: 'Grade 5 - A',
      attendanceDate: '2026-11-21',
      isMarked: false,
      isLocked: false,
      students: [
        { studentId: 's1', admissionNumber: 'ANA-001', firstName: 'Ali', lastName: 'Hassan', rollNumber: '01' },
      ],
    });
    vi.spyOn(schoolOsApi, 'getSectionAttendanceSummary').mockResolvedValue({
      totalMarks: 1,
      presentCount: 1,
      absentCount: 0,
      lateCount: 0,
      excusedCount: 0,
      halfDayCount: 0,
      attendancePercentage: 100,
    });
    const markSpy = vi.spyOn(schoolOsApi, 'markSectionAttendance').mockResolvedValue({
      registerId: 'reg-1',
      attendanceDate: '2026-11-21',
      createdCount: 1,
      updatedCount: 0,
      correctionsRecorded: 0,
    });

    function Harness() {
      const { setLocale } = useLocale();
      return (
        <div>
          <button type="button" onClick={() => setLocale('ur')}>UR</button>
          <button type="button" onClick={() => setLocale('ar')}>AR</button>
          <AttendancePage />
        </div>
      );
    }

    renderWithProviders(<Harness />);
    expect(await screen.findByText(/Ali Hassan/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Save Attendance' }));
    await waitFor(() => expect(markSpy).toHaveBeenCalledTimes(1));

    fireEvent.click(screen.getByRole('button', { name: 'UR' }));
    expect(await screen.findByRole('heading', { name: 'روزانہ حاضری اور عملہ' })).toBeInTheDocument();
    expect(document.documentElement.dir).toBe('rtl');

    fireEvent.click(screen.getByRole('button', { name: 'AR' }));
    expect(await screen.findByRole('heading', { name: 'الحضور اليومي والكادر' })).toBeInTheDocument();
    expect(document.documentElement.dir).toBe('rtl');
  });
});
