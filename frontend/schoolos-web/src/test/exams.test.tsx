import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@/context/ThemeContext';
import { LocaleProvider, useLocale } from '@/context/LocaleContext';
import { AuthProvider } from '@/context/AuthContext';
import { ToastProvider } from '@/context/ToastContext';
import { AppDataProvider } from '@/context/AppDataContext';
import { ExamsPage } from '@/pages/schoolos/ExamsPage';
import { tokenStore } from '@/auth/tokenStore';
import { schoolOsApi } from '@/api/client';

function renderWithProviders(ui: React.ReactElement) {
  return render(
    <MemoryRouter initialEntries={['/exams']}>
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
    email: 'principal@schoolos.test',
    name: 'Principal',
    role: 'TenantAdmin',
    tenantId: '01931a74-0000-7000-8000-000000000001',
    campusId: null,
    mustChangePassword: false,
    permissions: ['exams:view', 'exams:create', 'exams:edit'],
    preferredLanguage: 'en',
  });
}

describe('Phase 2B Examinations, Gradebooks & Report Cards UI', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    tokenStore.clearSession();
    window.localStorage.clear();
  });

  it('shows real error and retry state when exams API fails without fabricating records', async () => {
    seedAdmin();
    const listSubjectsSpy = vi
      .spyOn(schoolOsApi, 'listSubjects')
      .mockRejectedValueOnce(new Error('Exams module not licensed'))
      .mockResolvedValueOnce([]);
    vi.spyOn(schoolOsApi, 'listSchools').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listAcademicYears').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listGradeLevels').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listSections').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listCanonicalStudents').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listExamTerms').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listExamPapers').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listMarkSheets').mockResolvedValue([]);

    renderWithProviders(<ExamsPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Exams module not licensed');

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await waitFor(() => expect(listSubjectsSpy).toHaveBeenCalledTimes(2));
  });

  it('loads section mark sheet, saves marks, approves submitted sheet, and supports Urdu/Arabic RTL', async () => {
    seedAdmin();
    vi.spyOn(schoolOsApi, 'listSchools').mockResolvedValue([
      { id: 'sch-1', tenantId: 't1', name: 'City Grammar', code: 'CGS', isActive: true, campuses: [{ id: 'cmp-1', name: 'North', code: 'N' }] },
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
    vi.spyOn(schoolOsApi, 'listCanonicalStudents').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listSubjects').mockResolvedValue([
      { id: 'sub-1', schoolId: 'sch-1', gradeLevelId: 'gr-5', code: 'MATH', name: 'Mathematics', isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listExamTerms').mockResolvedValue([
      { id: 'tm-1', schoolId: 'sch-1', campusId: 'cmp-1', academicYearId: 'yr-1', code: 'MID', name: 'Midterm 2026', startDate: '2026-11-10', endDate: '2026-11-25', status: 'Active' },
    ]);
    vi.spyOn(schoolOsApi, 'listExamPapers').mockResolvedValue([
      { id: 'pp-1', campusId: 'cmp-1', examTermId: 'tm-1', examTermName: 'Midterm 2026', gradeLevelId: 'gr-5', subjectId: 'sub-1', subjectCode: 'MATH', subjectName: 'Mathematics', maxMarks: 100, passingMarks: 40, weightagePercent: 100 },
    ]);
    vi.spyOn(schoolOsApi, 'listMarkSheets').mockResolvedValue([
      { id: 'ms-1', campusId: 'cmp-1', examPaperId: 'pp-1', sectionId: 'sec-a', examTermName: 'Midterm 2026', subjectName: 'Mathematics', maxMarks: 100, status: 'Submitted' },
    ]);
    vi.spyOn(schoolOsApi, 'getSectionMarkSheet').mockResolvedValue({
      markSheetId: 'ms-1',
      examPaperId: 'pp-1',
      sectionId: 'sec-a',
      sectionName: 'Grade 5 - A',
      subjectName: 'Mathematics',
      maxMarks: 100,
      passingMarks: 40,
      status: 'Draft',
      students: [
        { studentId: 's1', admissionNumber: 'CGS-001', firstName: 'Bilal', lastName: 'Aslam', marksObtained: 92, isAbsent: false, percentage: 92, letterGrade: 'A+', isPassed: true },
      ],
    });
    const saveSpy = vi.spyOn(schoolOsApi, 'saveSectionMarks').mockResolvedValue({
      markSheetId: 'ms-1',
      status: 'Draft',
      marksCount: 1,
    });
    const reviewSpy = vi.spyOn(schoolOsApi, 'reviewMarkSheet').mockResolvedValue({
      id: 'ms-1',
      status: 'Approved',
    });

    function Harness() {
      const { setLocale } = useLocale();
      return (
        <div>
          <button type="button" onClick={() => setLocale('ur')}>UR</button>
          <button type="button" onClick={() => setLocale('ar')}>AR</button>
          <ExamsPage />
        </div>
      );
    }

    renderWithProviders(<Harness />);
    expect(await screen.findByText(/Bilal Aslam/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Save Marks' }));
    await waitFor(() => expect(saveSpy).toHaveBeenCalledTimes(1));

    // Switch to Approvals tab and approve
    fireEvent.click(screen.getByRole('tab', { name: 'Approval Queue' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Approve' }));
    await waitFor(() => expect(reviewSpy).toHaveBeenCalledWith('ms-1', true, 'Approved'));

    fireEvent.click(screen.getByRole('button', { name: 'UR' }));
    expect(await screen.findByRole('heading', { name: 'امتحانات اور رزلٹ کارڈ' })).toBeInTheDocument();
    expect(document.documentElement.dir).toBe('rtl');

    fireEvent.click(screen.getByRole('button', { name: 'AR' }));
    expect(await screen.findByRole('heading', { name: 'الاختبارات وبطاقات الدرجات' })).toBeInTheDocument();
    expect(document.documentElement.dir).toBe('rtl');
  });
});
