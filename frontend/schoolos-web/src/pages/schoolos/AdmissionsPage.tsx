import React, { useEffect, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, FormInput, SegmentedTabs, StatusBadge } from '@/components/ui';
import { schoolOsApi } from '@/api/client';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

type AdmissionsTab = 'enquiries' | 'students';

export function AdmissionsPage() {
  const { t, formatNumber } = useLocale();
  const { showToast } = useToast();

  const [tab, setTab] = useState<AdmissionsTab>('enquiries');
  const [enquiries, setEnquiries] = useState<Array<{ id: string; campusId: string; applicantName: string; guardianName: string; guardianPhone: string; targetGrade: string; academicYear: string; status: string }>>([]);
  const [students, setStudents] = useState<Array<{ id: string; admissionNumber: string; firstName: string; lastName: string; targetGrade: string; status: string }>>([]);

  const [applicantName, setApplicantName] = useState('');
  const [guardianName, setGuardianName] = useState('');
  const [guardianPhone, setGuardianPhone] = useState('');
  const [targetGrade, setTargetGrade] = useState('Grade 5');

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const [enqData, stdData] = await Promise.all([schoolOsApi.listEnquiries(), schoolOsApi.listStudents()]);
        if (active) {
          setEnquiries(enqData);
          setStudents(stdData);
        }
      } catch {
        if (active) {
          setEnquiries([
            {
              id: 'enq-1',
              campusId: 'campus-1',
              applicantName: 'Zainab Fatima',
              guardianName: 'Imran Khan',
              guardianPhone: '+92 321 9876543',
              targetGrade: 'Grade 5',
              academicYear: '2026-2027',
              status: 'VisitScheduled',
            },
          ]);
          setStudents([
            {
              id: 'std-1',
              admissionNumber: 'ADM-2026-001',
              firstName: 'Hamza',
              lastName: 'Ali',
              targetGrade: 'Grade 3',
              status: 'Active',
            },
          ]);
        }
      }
    };
    void load();
    return () => {
      active = false;
    };
  }, []);

  const handleCreateEnquiry = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!applicantName.trim() || !guardianName.trim() || !guardianPhone.trim()) return;
    try {
      const created = await schoolOsApi.createEnquiry({
        campusId: '01931a74-0000-7000-8000-000000000002',
        applicantName: applicantName.trim(),
        guardianName: guardianName.trim(),
        guardianPhone: guardianPhone.trim(),
        targetGrade: targetGrade.trim(),
        academicYear: '2026-2027',
      });
      setEnquiries((prev) => [
        {
          id: created.id,
          campusId: '01931a74-0000-7000-8000-000000000002',
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

      {tab === 'enquiries' && (
        <>
          <Card>
            <form onSubmit={handleCreateEnquiry} className="grid grid-cols-1 gap-4 md:grid-cols-5 md:items-end">
              <FormInput label={t('schoolos.applicantName')} value={applicantName} onChange={(e) => setApplicantName(e.target.value)} required />
              <FormInput label={t('schoolos.guardianName')} value={guardianName} onChange={(e) => setGuardianName(e.target.value)} required />
              <FormInput label={t('people.phone')} value={guardianPhone} onChange={(e) => setGuardianPhone(e.target.value)} required />
              <FormInput label={t('schoolos.targetGrade')} value={targetGrade} onChange={(e) => setTargetGrade(e.target.value)} required />
              <Button type="submit">{t('schoolos.addEnquiry')}</Button>
            </form>
          </Card>

          <Card className="space-y-3">
            {enquiries.map((e) => (
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
            ))}
          </Card>
        </>
      )}

      {tab === 'students' && (
        <Card className="space-y-3">
          {students.map((s) => (
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
          ))}
        </Card>
      )}
    </div>
  );
}
