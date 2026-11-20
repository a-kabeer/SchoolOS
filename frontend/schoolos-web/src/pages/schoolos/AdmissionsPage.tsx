import React, { useCallback, useEffect, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, FormInput, SelectInput, SegmentedTabs, StatusBadge } from '@/components/ui';
import { schoolOsApi } from '@/api/client';
import { useAuth } from '@/context/AuthContext';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

type AdmissionsTab = 'enquiries' | 'students';

interface CampusOption {
  id: string;
  name: string;
}

export function AdmissionsPage() {
  const { t, formatNumber } = useLocale();
  const { showToast } = useToast();
  const { hasPermission } = useAuth();

  const [tab, setTab] = useState<AdmissionsTab>('enquiries');
  const [enquiries, setEnquiries] = useState<Array<{ id: string; campusId: string; applicantName: string; guardianName: string; guardianPhone: string; targetGrade: string; academicYear: string; status: string }>>([]);
  const [students, setStudents] = useState<Array<{ id: string; admissionNumber: string; firstName: string; lastName: string; targetGrade: string; status: string }>>([]);
  const [campuses, setCampuses] = useState<CampusOption[]>([]);
  const [campusId, setCampusId] = useState('');
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [applicantName, setApplicantName] = useState('');
  const [guardianName, setGuardianName] = useState('');
  const [guardianPhone, setGuardianPhone] = useState('');
  const [targetGrade, setTargetGrade] = useState('Grade 5');

  const [reloadTick, setReloadTick] = useState(0);

  const load = useCallback(() => {
    setLoading(true);
    setLoadError(null);
    setReloadTick((n) => n + 1);
  }, []);

  useEffect(() => {
    let active = true;
    const run = async () => {
      try {
        const [enqData, stdData, schoolsData] = await Promise.all([
          schoolOsApi.listEnquiries(),
          schoolOsApi.listStudents(),
          schoolOsApi.listSchools().catch(() => []),
        ]);
        if (!active) return;
        setEnquiries(enqData);
        setStudents(stdData);
        const flatCampuses = schoolsData.flatMap((s) =>
          s.campuses.map((c) => ({ id: c.id, name: `${s.name} — ${c.name}` }))
        );
        setCampuses(flatCampuses);
        setCampusId((prev) => prev || flatCampuses[0]?.id || '');
      } catch (err) {
        if (!active) return;
        setLoadError(err instanceof Error ? err.message : t('students.errorLoad'));
      } finally {
        if (active) setLoading(false);
      }
    };
    void run();
    return () => {
      active = false;
    };
  }, [reloadTick, t]);

  const handleCreateEnquiry = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!applicantName.trim() || !guardianName.trim() || !guardianPhone.trim() || !campusId) return;
    try {
      const created = await schoolOsApi.createEnquiry({
        campusId,
        applicantName: applicantName.trim(),
        guardianName: guardianName.trim(),
        guardianPhone: guardianPhone.trim(),
        targetGrade: targetGrade.trim(),
        academicYear: '2026-2027',
      });
      setEnquiries((prev) => [
        {
          id: created.id,
          campusId,
          applicantName: created.applicantName,
          guardianName: guardianName.trim(),
          guardianPhone: guardianPhone.trim(),
          targetGrade: targetGrade.trim(),
          academicYear: '2026-2027',
          status: created.status,
        },
        ...prev,
      ]);
      setApplicantName('');
      setGuardianName('');
      setGuardianPhone('');
      showToast(t('schoolos.enquiryCreated'), 'success');
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('login.failed'), 'error');
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('module.admissions.name')}
        subtitle={`${t('module.admissions.description')} (${formatNumber(enquiries.length)})`}
        breadcrumbs={[{ label: 'SchoolOS', path: '/admissions' }, { label: t('module.admissions.name') }]}
      />

      <SegmentedTabs<AdmissionsTab>
        value={tab}
        onChange={setTab}
        options={[
          { value: 'enquiries', label: t('schoolos.enquiriesTab') },
          { value: 'students', label: t('module.students.name') },
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
          <Button type="button" variant="outline" size="sm" onClick={() => void load()}>
            {t('students.retry')}
          </Button>
        </Card>
      )}

      {!loading && !loadError && tab === 'enquiries' && (
        <>
          {hasPermission('admissions:create') && (
            <Card>
              <form onSubmit={handleCreateEnquiry} className="grid grid-cols-1 gap-4 md:grid-cols-6 md:items-end">
                <SelectInput
                  label={t('students.campus')}
                  value={campusId}
                  onChange={(e) => setCampusId(e.target.value)}
                  options={[
                    { value: '', label: t('students.selectCampus') },
                    ...campuses.map((c) => ({ value: c.id, label: c.name })),
                  ]}
                  required
                />
                <FormInput label={t('schoolos.applicantName')} value={applicantName} onChange={(e) => setApplicantName(e.target.value)} required />
                <FormInput label={t('schoolos.guardianName')} value={guardianName} onChange={(e) => setGuardianName(e.target.value)} required />
                <FormInput label={t('people.phone')} value={guardianPhone} onChange={(e) => setGuardianPhone(e.target.value)} required />
                <FormInput label={t('schoolos.targetGrade')} value={targetGrade} onChange={(e) => setTargetGrade(e.target.value)} required />
                <Button type="submit" disabled={!campusId}>{t('schoolos.addEnquiry')}</Button>
              </form>
            </Card>
          )}

          <Card className="space-y-3">
            {enquiries.length === 0 ? (
              <p className="text-sm text-muted">{t('students.emptyList')}</p>
            ) : (
              enquiries.map((e) => (
                <div key={e.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border p-4">
                  <div>
                    <p className="text-sm font-bold text-ink">
                      {e.applicantName} ({e.targetGrade})
                    </p>
                    <p className="text-xs text-muted">
                      {e.guardianName} · {e.guardianPhone} · {e.academicYear}
                    </p>
                  </div>
                  <StatusBadge label={e.status} tone={e.status === 'Admitted' ? 'success' : 'primary'} />
                </div>
              ))
            )}
          </Card>
        </>
      )}

      {!loading && !loadError && tab === 'students' && (
        <Card className="space-y-3">
          {students.length === 0 ? (
            <p className="text-sm text-muted">{t('students.emptyList')}</p>
          ) : (
            students.map((s) => (
              <div key={s.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border p-4">
                <div>
                  <p className="text-sm font-bold text-ink">
                    {s.firstName} {s.lastName}
                  </p>
                  <p className="text-xs text-muted">
                    {s.admissionNumber} · {s.targetGrade}
                  </p>
                </div>
                <StatusBadge label={s.status} tone="success" />
              </div>
            ))
          )}
        </Card>
      )}
    </div>
  );
}
