import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@/context/ThemeContext';
import { LocaleProvider, useLocale } from '@/context/LocaleContext';
import { AuthProvider } from '@/context/AuthContext';
import { ToastProvider } from '@/context/ToastContext';
import { AppDataProvider } from '@/context/AppDataContext';
import { FeesPage } from '@/pages/schoolos/FeesPage';
import { tokenStore } from '@/auth/tokenStore';
import { schoolOsApi } from '@/api/client';

function renderWithProviders(ui: React.ReactElement) {
  return render(
    <MemoryRouter initialEntries={['/fees']}>
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
    permissions: ['fees:view', 'fees:create', 'fees:edit'],
    preferredLanguage: 'en',
  });
}

describe('Phase 2A Fees, Challans & Receipts UI', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    tokenStore.clearSession();
    window.localStorage.clear();
  });

  it('shows real error and retry state when fees API fails without fabricating records', async () => {
    seedAdmin();
    const listHeadsSpy = vi
      .spyOn(schoolOsApi, 'listFeeHeads')
      .mockRejectedValueOnce(new Error('Fees module not licensed'))
      .mockResolvedValueOnce([]);
    vi.spyOn(schoolOsApi, 'listSchools').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listAcademicYears').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listGradeLevels').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listFeeSchedules').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listFeeChallans').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listFeeReceipts').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'getDefaulterAging').mockResolvedValue({
      asOfDate: '2026-11-21',
      outstandingChallanCount: 0,
      totalOutstanding: 0,
      currentAmount: 0,
      days1To30Amount: 0,
      days31To60Amount: 0,
      days61PlusAmount: 0,
      items: [],
    });

    renderWithProviders(<FeesPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Fees module not licensed');

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await waitFor(() => expect(listHeadsSpy).toHaveBeenCalledTimes(2));
  });

  it('collects payment on a challan, views defaulter aging, and supports Urdu/Arabic RTL', async () => {
    seedAdmin();
    vi.spyOn(schoolOsApi, 'listSchools').mockResolvedValue([
      { id: 'sch-1', tenantId: 't1', name: 'Beacon', code: 'BFS', isActive: true, campuses: [{ id: 'cmp-1', name: 'Main', code: 'M' }] },
    ]);
    vi.spyOn(schoolOsApi, 'listAcademicYears').mockResolvedValue([
      { id: 'yr-1', schoolId: 'sch-1', code: '2026-27', name: 'AY 2026-27', startDate: '2026-04-01', endDate: '2027-03-31', isCurrent: true, isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listGradeLevels').mockResolvedValue([
      { id: 'gr-5', schoolId: 'sch-1', code: 'G5', name: 'Grade 5', sequenceOrder: 5, isActive: true },
    ]);
    vi.spyOn(schoolOsApi, 'listFeeHeads').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listFeeSchedules').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'listFeeChallans').mockResolvedValue([
      {
        id: 'ch-1',
        campusId: 'cmp-1',
        studentId: 's1',
        academicYearId: 'yr-1',
        gradeLevelId: 'gr-5',
        billingPeriod: '2026-11',
        challanNumber: 'CH-202611-BFS-001',
        currencyCode: 'PKR',
        issueDate: '2026-11-01',
        dueDate: '2026-11-10',
        grossAmount: 10000,
        discountAmount: 1000,
        lateFeeAmount: 0,
        netAmount: 9000,
        paidAmount: 0,
        balanceAmount: 9000,
        status: 'Issued',
        lines: [],
      },
    ]);
    vi.spyOn(schoolOsApi, 'listFeeReceipts').mockResolvedValue([]);
    vi.spyOn(schoolOsApi, 'getDefaulterAging').mockResolvedValue({
      asOfDate: '2026-12-20',
      outstandingChallanCount: 1,
      totalOutstanding: 9000,
      currentAmount: 0,
      days1To30Amount: 0,
      days31To60Amount: 9000,
      days61PlusAmount: 0,
      items: [
        {
          challanId: 'ch-1',
          studentId: 's1',
          challanNumber: 'CH-202611-BFS-001',
          billingPeriod: '2026-11',
          dueDate: '2026-11-10',
          daysOverdue: 40,
          agingBucket: '31-60 Days',
          balanceAmount: 9000,
          currencyCode: 'PKR',
        },
      ],
    });
    const postReceiptSpy = vi.spyOn(schoolOsApi, 'postFeeReceipt').mockResolvedValue({
      id: 'rcp-1',
      campusId: 'cmp-1',
      challanId: 'ch-1',
      studentId: 's1',
      receiptNumber: 'RCP-20261121-001',
      paymentDate: '2026-11-21',
      amount: 9000,
      currencyCode: 'PKR',
      method: 'Cash',
      collectedBy: 'admin',
      collectedAtUtc: '2026-11-21T00:00:00Z',
      status: 'Posted',
    });

    function Harness() {
      const { setLocale } = useLocale();
      return (
        <div>
          <button type="button" onClick={() => setLocale('ur')}>UR</button>
          <button type="button" onClick={() => setLocale('ar')}>AR</button>
          <FeesPage />
        </div>
      );
    }

    renderWithProviders(<Harness />);
    expect(await screen.findByText(/CH-202611-BFS-001/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Collect Payment' }));
    fireEvent.click(screen.getByRole('button', { name: 'Post Receipt' }));
    await waitFor(() => expect(postReceiptSpy).toHaveBeenCalledTimes(1));

    fireEvent.click(screen.getByRole('button', { name: 'UR' }));
    expect(await screen.findByRole('heading', { name: 'فیس اور چالان' })).toBeInTheDocument();
    expect(document.documentElement.dir).toBe('rtl');

    fireEvent.click(screen.getByRole('button', { name: 'AR' }));
    expect(await screen.findByRole('heading', { name: 'الرسوم والإيصالات' })).toBeInTheDocument();
    expect(document.documentElement.dir).toBe('rtl');
  });
});
