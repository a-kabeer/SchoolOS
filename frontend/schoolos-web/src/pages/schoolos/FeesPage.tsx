import React, { useCallback, useEffect, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, FormInput, SegmentedTabs, SelectInput, StatusBadge } from '@/components/ui';
import {
  schoolOsApi,
  type AcademicYearDto,
  type DefaulterAgingSummaryDto,
  type FeeChallanDto,
  type FeeHeadDto,
  type FeeReceiptDto,
  type FeeScheduleDto,
  type GradeLevelDto,
} from '@/api/client';
import { useAuth } from '@/context/AuthContext';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

type FeesTab = 'challans' | 'schedules' | 'receipts' | 'defaulters';

interface SchoolOption {
  id: string;
  name: string;
}

interface CampusOption {
  id: string;
  schoolId: string;
  name: string;
}

const FEE_FREQUENCIES = ['Monthly', 'Term', 'Annual', 'OneTime'];
const PAYMENT_METHODS = ['Cash', 'BankTransfer', 'Cheque', 'OnlineGateway'];

export function FeesPage() {
  const { t, formatNumber } = useLocale();
  const { showToast } = useToast();
  const { hasPermission } = useAuth();

  const canCreate = hasPermission('fees:create');
  const canEdit = hasPermission('fees:edit');

  const [tab, setTab] = useState<FeesTab>('challans');
  const [reloadTick, setReloadTick] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [schools, setSchools] = useState<SchoolOption[]>([]);
  const [campuses, setCampuses] = useState<CampusOption[]>([]);
  const [academicYears, setAcademicYears] = useState<AcademicYearDto[]>([]);
  const [gradeLevels, setGradeLevels] = useState<GradeLevelDto[]>([]);
  const [feeHeads, setFeeHeads] = useState<FeeHeadDto[]>([]);
  const [schedules, setSchedules] = useState<FeeScheduleDto[]>([]);
  const [challans, setChallans] = useState<FeeChallanDto[]>([]);
  const [receipts, setReceipts] = useState<FeeReceiptDto[]>([]);
  const [aging, setAging] = useState<DefaulterAgingSummaryDto | null>(null);

  // Batch challan generation
  const [genCampusId, setGenCampusId] = useState('');
  const [genYearId, setGenYearId] = useState('');
  const [genGradeId, setGenGradeId] = useState('');
  const [genPeriod, setGenPeriod] = useState(() => new Date().toISOString().slice(0, 7));
  const [genIssueDate, setGenIssueDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [genDueDate, setGenDueDate] = useState(() => new Date().toISOString().slice(0, 10));

  // Payment collection
  const [payChallanId, setPayChallanId] = useState('');
  const [payAmount, setPayAmount] = useState('');
  const [payMethod, setPayMethod] = useState('Cash');
  const [payRef, setPayRef] = useState('');
  const [payDate, setPayDate] = useState(() => new Date().toISOString().slice(0, 10));

  // Fee Head & Schedule creation
  const [headSchoolId, setHeadSchoolId] = useState('');
  const [headCode, setHeadCode] = useState('');
  const [headName, setHeadName] = useState('');
  const [headFreq, setHeadFreq] = useState('Monthly');

  const [schedFeeHeadId, setSchedFeeHeadId] = useState('');
  const [schedAmount, setSchedAmount] = useState('');
  const [schedSiblingPct, setSchedSiblingPct] = useState('0');
  const [schedLateFee, setSchedLateFee] = useState('0');

  // Reversal confirmation
  const [reverseTargetId, setReverseTargetId] = useState<string | null>(null);
  const [reverseReason, setReverseReason] = useState('');

  const refresh = useCallback(() => {
    setLoading(true);
    setLoadError(null);
    setReloadTick((n) => n + 1);
  }, []);

  useEffect(() => {
    let active = true;
    const run = async () => {
      try {
        const [schoolsData, yearsData, gradesData, headsData, schedulesData, challansData, receiptsData, agingData] =
          await Promise.all([
            schoolOsApi.listSchools(),
            schoolOsApi.listAcademicYears(),
            schoolOsApi.listGradeLevels(),
            schoolOsApi.listFeeHeads(),
            schoolOsApi.listFeeSchedules(),
            schoolOsApi.listFeeChallans(),
            schoolOsApi.listFeeReceipts(),
            schoolOsApi.getDefaulterAging(),
          ]);

        if (!active) return;
        setSchools(schoolsData.map((s) => ({ id: s.id, name: s.name })));
        setHeadSchoolId((prev) => prev || schoolsData[0]?.id || '');

        const flatCampuses = schoolsData.flatMap((s) =>
          s.campuses.map((c) => ({ id: c.id, schoolId: s.id, name: `${s.name} — ${c.name}` }))
        );
        setCampuses(flatCampuses);
        setGenCampusId((prev) => prev || flatCampuses[0]?.id || '');

        setAcademicYears(yearsData);
        const defaultYear = yearsData.find((y) => y.isCurrent)?.id || yearsData[0]?.id || '';
        setGenYearId((prev) => prev || defaultYear);

        setGradeLevels(gradesData);
        setGenGradeId((prev) => prev || gradesData[0]?.id || '');

        setFeeHeads(headsData);
        setSchedFeeHeadId((prev) => prev || headsData[0]?.id || '');
        setSchedules(schedulesData);
        setChallans(challansData);
        setReceipts(receiptsData);
        setAging(agingData);
      } catch (err) {
        if (!active) return;
        setLoadError(err instanceof Error ? err.message : t('fees.errorLoad'));
      } finally {
        if (active) setLoading(false);
      }
    };
    void run();
    return () => {
      active = false;
    };
  }, [reloadTick, t]);

  const handleGenerateChallans = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!genCampusId || !genYearId || !genGradeId || !genPeriod) return;
    try {
      const res = await schoolOsApi.generateMonthlyChallans({
        campusId: genCampusId,
        academicYearId: genYearId,
        gradeLevelId: genGradeId,
        billingPeriod: genPeriod,
        issueDate: genIssueDate,
        dueDate: genDueDate,
      });
      showToast(
        `${t('fees.generatedSuccess')}: ${formatNumber(res.generatedCount)} (${t('fees.skipped')}: ${formatNumber(res.skippedExistingCount)})`,
        'success'
      );
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('fees.errorAction'), 'error');
    }
  };

  const handlePostReceipt = async (e: React.FormEvent) => {
    e.preventDefault();
    const numericAmount = Number(payAmount);
    if (!payChallanId || !Number.isFinite(numericAmount) || numericAmount <= 0) return;
    try {
      const created = await schoolOsApi.postFeeReceipt({
        challanId: payChallanId,
        amount: numericAmount,
        paymentDate: payDate,
        method: payMethod,
        externalReference: payRef.trim() || undefined,
      });
      setPayChallanId('');
      setPayAmount('');
      setPayRef('');
      showToast(`${t('fees.receiptPosted')}: ${created.receiptNumber}`, 'success');
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('fees.errorAction'), 'error');
    }
  };

  const handleCreateHead = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!headSchoolId || !headCode.trim() || !headName.trim()) return;
    try {
      await schoolOsApi.createFeeHead({
        schoolId: headSchoolId,
        code: headCode.trim(),
        name: headName.trim(),
        frequency: headFreq,
      });
      setHeadCode('');
      setHeadName('');
      showToast(t('fees.headCreated'), 'success');
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('fees.errorAction'), 'error');
    }
  };

  const handleCreateSchedule = async (e: React.FormEvent) => {
    e.preventDefault();
    const amt = Number(schedAmount);
    if (!genCampusId || !genYearId || !genGradeId || !schedFeeHeadId || !Number.isFinite(amt) || amt <= 0) return;
    try {
      await schoolOsApi.createFeeSchedule({
        campusId: genCampusId,
        academicYearId: genYearId,
        gradeLevelId: genGradeId,
        feeHeadId: schedFeeHeadId,
        amount: amt,
        currencyCode: 'PKR',
        siblingDiscountPercent: Number(schedSiblingPct) || 0,
        lateFeePerDay: Number(schedLateFee) || 0,
      });
      setSchedAmount('');
      showToast(t('fees.scheduleCreated'), 'success');
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('fees.errorAction'), 'error');
    }
  };

  const handleReverseReceipt = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reverseTargetId || !reverseReason.trim()) return;
    try {
      await schoolOsApi.reverseFeeReceipt(reverseTargetId, reverseReason.trim());
      setReverseTargetId(null);
      setReverseReason('');
      showToast(t('fees.receiptReversed'), 'success');
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('fees.errorAction'), 'error');
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('fees.title')}
        subtitle={t('fees.subtitle')}
        breadcrumbs={[{ label: 'SchoolOS', path: '/fees' }, { label: t('fees.title') }]}
      />

      <SegmentedTabs<FeesTab>
        value={tab}
        onChange={setTab}
        options={[
          { value: 'challans', label: t('fees.tabs.challans') },
          { value: 'schedules', label: t('fees.tabs.schedules') },
          { value: 'receipts', label: t('fees.tabs.receipts') },
          { value: 'defaulters', label: t('fees.tabs.defaulters') },
        ]}
      />

      {loading && (
        <Card>
          <p className="text-sm text-muted">{t('loading.label')}</p>
        </Card>
      )}

      {!loading && loadError && (
        <Card className="flex flex-wrap items-center justify-between gap-3 border-error/40 bg-light-error/30">
          <p role="alert" className="text-sm font-medium text-error">
            {loadError}
          </p>
          <Button type="button" variant="outline" size="sm" onClick={refresh}>
            {t('students.retry')}
          </Button>
        </Card>
      )}

      {!loading && !loadError && tab === 'challans' && (
        <div className="space-y-4">
          {canCreate && (
            <Card>
              <form onSubmit={handleGenerateChallans} className="grid grid-cols-1 gap-3 sm:grid-cols-3 lg:grid-cols-7 sm:items-end">
                <SelectInput label={t('students.campus')} value={genCampusId} onChange={(e) => setGenCampusId(e.target.value)} options={campuses.map((c) => ({ value: c.id, label: c.name }))} required />
                <SelectInput label={t('students.academicYear')} value={genYearId} onChange={(e) => setGenYearId(e.target.value)} options={academicYears.map((y) => ({ value: y.id, label: y.name }))} required />
                <SelectInput label={t('students.gradeLevel')} value={genGradeId} onChange={(e) => setGenGradeId(e.target.value)} options={gradeLevels.map((g) => ({ value: g.id, label: g.name }))} required />
                <FormInput label={t('fees.billingPeriod')} value={genPeriod} onChange={(e) => setGenPeriod(e.target.value)} placeholder="2026-11" required />
                <FormInput label={t('fees.issueDate')} type="date" value={genIssueDate} onChange={(e) => setGenIssueDate(e.target.value)} required />
                <FormInput label={t('fees.dueDate')} type="date" value={genDueDate} onChange={(e) => setGenDueDate(e.target.value)} required />
                <Button type="submit">{t('fees.generateBatch')}</Button>
              </form>
            </Card>
          )}

          {canCreate && payChallanId && (
            <Card className="border-primary/40">
              <form onSubmit={handlePostReceipt} className="grid grid-cols-1 gap-3 sm:grid-cols-5 sm:items-end">
                <FormInput label={t('fees.amount')} type="number" step="0.01" value={payAmount} onChange={(e) => setPayAmount(e.target.value)} required />
                <SelectInput label={t('fees.method')} value={payMethod} onChange={(e) => setPayMethod(e.target.value)} options={PAYMENT_METHODS.map((m) => ({ value: m, label: m }))} />
                <FormInput label={t('fees.paymentDate')} type="date" value={payDate} onChange={(e) => setPayDate(e.target.value)} required />
                <FormInput label={t('fees.externalRef')} value={payRef} onChange={(e) => setPayRef(e.target.value)} />
                <div className="flex gap-2">
                  <Button type="submit" size="sm">{t('fees.collectSubmit')}</Button>
                  <Button type="button" size="sm" variant="outline" onClick={() => setPayChallanId('')}>{t('students.cancel')}</Button>
                </div>
              </form>
            </Card>
          )}

          <Card className="space-y-2">
            {challans.length === 0 ? (
              <p className="text-sm text-muted">{t('fees.noChallans')}</p>
            ) : (
              challans.map((c) => (
                <div key={c.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border p-4">
                  <div>
                    <p className="text-sm font-bold text-ink">
                      {c.challanNumber} · {c.billingPeriod}
                    </p>
                    <p className="text-xs text-muted">
                      {t('fees.net')}: {c.currencyCode} {formatNumber(c.netAmount)} · {t('fees.paid')}: {formatNumber(c.paidAmount)} · {t('fees.balance')}: {formatNumber(c.balanceAmount)} · {t('fees.dueDate')}: {c.dueDate}
                    </p>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <StatusBadge
                      label={c.status}
                      tone={c.status === 'Paid' ? 'success' : c.status === 'Voided' ? 'error' : 'warning'}
                    />
                    {canCreate && c.balanceAmount > 0 && c.status !== 'Voided' && (
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          setPayChallanId(c.id);
                          setPayAmount(String(c.balanceAmount));
                        }}
                      >
                        {t('fees.collect')}
                      </Button>
                    )}
                  </div>
                </div>
              ))
            )}
          </Card>
        </div>
      )}

      {!loading && !loadError && tab === 'schedules' && (
        <div className="space-y-4">
          {canCreate && (
            <Card>
              <form onSubmit={handleCreateHead} className="grid grid-cols-1 gap-3 sm:grid-cols-5 sm:items-end">
                <SelectInput label={t('fees.school')} value={headSchoolId} onChange={(e) => setHeadSchoolId(e.target.value)} options={schools.map((s) => ({ value: s.id, label: s.name }))} required />
                <FormInput label={t('fees.headCode')} value={headCode} onChange={(e) => setHeadCode(e.target.value)} placeholder="TUITION" required />
                <FormInput label={t('fees.headName')} value={headName} onChange={(e) => setHeadName(e.target.value)} required />
                <SelectInput label={t('fees.frequency')} value={headFreq} onChange={(e) => setHeadFreq(e.target.value)} options={FEE_FREQUENCIES.map((f) => ({ value: f, label: f }))} />
                <Button type="submit">{t('fees.addHead')}</Button>
              </form>
            </Card>
          )}

          {canCreate && feeHeads.length > 0 && (
            <Card>
              <form onSubmit={handleCreateSchedule} className="grid grid-cols-1 gap-3 sm:grid-cols-4 lg:grid-cols-8 sm:items-end">
                <SelectInput label={t('students.campus')} value={genCampusId} onChange={(e) => setGenCampusId(e.target.value)} options={campuses.map((c) => ({ value: c.id, label: c.name }))} required />
                <SelectInput label={t('students.academicYear')} value={genYearId} onChange={(e) => setGenYearId(e.target.value)} options={academicYears.map((y) => ({ value: y.id, label: y.name }))} required />
                <SelectInput label={t('students.gradeLevel')} value={genGradeId} onChange={(e) => setGenGradeId(e.target.value)} options={gradeLevels.map((g) => ({ value: g.id, label: g.name }))} required />
                <SelectInput label={t('fees.feeHead')} value={schedFeeHeadId} onChange={(e) => setSchedFeeHeadId(e.target.value)} options={feeHeads.map((h) => ({ value: h.id, label: `${h.name} (${h.code})` }))} required />
                <FormInput label={t('fees.amount')} type="number" step="0.01" value={schedAmount} onChange={(e) => setSchedAmount(e.target.value)} required />
                <FormInput label={t('fees.siblingDiscount')} type="number" step="0.01" value={schedSiblingPct} onChange={(e) => setSchedSiblingPct(e.target.value)} />
                <FormInput label={t('fees.lateFeePerDay')} type="number" step="0.01" value={schedLateFee} onChange={(e) => setSchedLateFee(e.target.value)} />
                <Button type="submit">{t('fees.addSchedule')}</Button>
              </form>
            </Card>
          )}

          <Card className="space-y-2">
            {schedules.length === 0 ? (
              <p className="text-sm text-muted">{t('fees.noSchedules')}</p>
            ) : (
              schedules.map((s) => (
                <div key={s.id} className="flex flex-wrap items-center justify-between rounded-xl border border-border p-3">
                  <div>
                    <p className="text-sm font-bold text-ink">{s.feeHeadName} ({s.feeHeadCode})</p>
                    <p className="text-xs text-muted">
                      {s.currencyCode} {formatNumber(s.amount)} · {t('fees.siblingDiscount')}: {formatNumber(s.siblingDiscountPercent)}%
                    </p>
                  </div>
                  <StatusBadge label={s.currencyCode} tone="primary" />
                </div>
              ))
            )}
          </Card>
        </div>
      )}

      {!loading && !loadError && tab === 'receipts' && (
        <div className="space-y-4">
          {canEdit && reverseTargetId && (
            <Card className="border-error/40">
              <form onSubmit={handleReverseReceipt} className="flex flex-wrap items-end gap-3">
                <FormInput label={t('fees.reversalReason')} value={reverseReason} onChange={(e) => setReverseReason(e.target.value)} required />
                <Button type="submit" size="sm">{t('fees.confirmReverse')}</Button>
                <Button type="button" size="sm" variant="outline" onClick={() => setReverseTargetId(null)}>{t('students.cancel')}</Button>
              </form>
            </Card>
          )}

          <Card className="space-y-2">
            {receipts.length === 0 ? (
              <p className="text-sm text-muted">{t('fees.noReceipts')}</p>
            ) : (
              receipts.map((r) => (
                <div key={r.id} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-border p-3">
                  <div>
                    <p className="text-sm font-bold text-ink">
                      {r.receiptNumber} · {r.currencyCode} {formatNumber(r.amount)} ({r.method})
                    </p>
                    <p className="text-xs text-muted">
                      {r.paymentDate}{r.externalReference ? ` · ${r.externalReference}` : ''}{r.reversalReason ? ` · ${r.reversalReason}` : ''}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <StatusBadge label={r.status} tone={r.status === 'Posted' ? 'success' : 'error'} />
                    {canEdit && r.status === 'Posted' && (
                      <Button type="button" size="sm" variant="outline" onClick={() => setReverseTargetId(r.id)}>
                        {t('fees.reverse')}
                      </Button>
                    )}
                  </div>
                </div>
              ))
            )}
          </Card>
        </div>
      )}

      {!loading && !loadError && tab === 'defaulters' && aging && (
        <div className="space-y-4">
          <Card className="grid grid-cols-2 gap-4 sm:grid-cols-5">
            <div>
              <p className="text-xs text-muted">{t('fees.totalOutstanding')}</p>
              <p className="text-xl font-bold text-ink">{formatNumber(aging.totalOutstanding)}</p>
            </div>
            <div>
              <p className="text-xs text-muted">{t('fees.bucketCurrent')}</p>
              <p className="text-xl font-bold text-primary">{formatNumber(aging.currentAmount)}</p>
            </div>
            <div>
              <p className="text-xs text-muted">1–30</p>
              <p className="text-xl font-bold text-warning">{formatNumber(aging.days1To30Amount)}</p>
            </div>
            <div>
              <p className="text-xs text-muted">31–60</p>
              <p className="text-xl font-bold text-warning">{formatNumber(aging.days31To60Amount)}</p>
            </div>
            <div>
              <p className="text-xs text-muted">61+</p>
              <p className="text-xl font-bold text-error">{formatNumber(aging.days61PlusAmount)}</p>
            </div>
          </Card>

          <Card className="space-y-2">
            {aging.items.length === 0 ? (
              <p className="text-sm text-muted">{t('fees.noDefaulters')}</p>
            ) : (
              aging.items.map((item) => (
                <div key={item.challanId} className="flex flex-wrap items-center justify-between rounded-xl border border-border p-3 text-xs">
                  <div>
                    <p className="font-bold text-ink">{item.challanNumber} · {item.billingPeriod}</p>
                    <p className="text-muted">{t('fees.dueDate')}: {item.dueDate} · {item.agingBucket}</p>
                  </div>
                  <StatusBadge label={`${item.currencyCode} ${formatNumber(item.balanceAmount)}`} tone="error" />
                </div>
              ))
            )}
          </Card>
        </div>
      )}
    </div>
  );
}
