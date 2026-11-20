import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, FormInput, SelectInput, SegmentedTabs, StatusBadge } from '@/components/ui';
import {
  schoolOsApi,
  type AcademicYearDto,
  type GradeLevelDto,
  type GuardianDto,
  type HouseholdDto,
  type SectionDto,
  type StudentDetailDto,
  type StudentSummaryDto,
} from '@/api/client';
import { useAuth } from '@/context/AuthContext';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

type MainTab = 'directory' | 'register' | 'guardians' | 'households';
type DetailTab = 'profile' | 'guardians' | 'enrollments' | 'status' | 'households';

interface CampusOption {
  id: string;
  schoolId: string;
  name: string;
}

const RELATIONSHIP_OPTIONS = [
  'Father',
  'Mother',
  'LegalGuardian',
  'EmergencyContact',
  'AuthorizedPickup',
  'Payer',
  'Other',
];

const LIFECYCLE_STATUS_OPTIONS = ['Active', 'Inactive', 'Withdrawn', 'Graduated'];

export function StudentsPage() {
  const { t, formatNumber } = useLocale();
  const { showToast } = useToast();
  const { hasPermission } = useAuth();

  const canCreate = hasPermission('students:create');
  const canEdit = hasPermission('students:edit');

  const [mainTab, setMainTab] = useState<MainTab>('directory');
  const [detailTab, setDetailTab] = useState<DetailTab>('profile');

  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [students, setStudents] = useState<StudentSummaryDto[]>([]);
  const [campuses, setCampuses] = useState<CampusOption[]>([]);
  const [academicYears, setAcademicYears] = useState<AcademicYearDto[]>([]);
  const [gradeLevels, setGradeLevels] = useState<GradeLevelDto[]>([]);
  const [sections, setSections] = useState<SectionDto[]>([]);
  const [guardians, setGuardians] = useState<GuardianDto[]>([]);
  const [households, setHouseholds] = useState<HouseholdDto[]>([]);

  // Directory filters
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [yearFilter, setYearFilter] = useState('');

  // Selected student detail
  const [selectedStudentId, setSelectedStudentId] = useState<string | null>(null);
  const [selectedDetail, setSelectedDetail] = useState<StudentDetailDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  // Registration form
  const [regCampusId, setRegCampusId] = useState('');
  const [regAdmissionNo, setRegAdmissionNo] = useState('');
  const [regFirstName, setRegFirstName] = useState('');
  const [regMiddleName, setRegMiddleName] = useState('');
  const [regLastName, setRegLastName] = useState('');
  const [regDob, setRegDob] = useState('2015-05-01');
  const [regGender, setRegGender] = useState('');
  const [regAdmissionDate, setRegAdmissionDate] = useState('2026-04-01');
  const [regNotes, setRegNotes] = useState('');
  const [regError, setRegError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // Edit profile form
  const [editFirstName, setEditFirstName] = useState('');
  const [editMiddleName, setEditMiddleName] = useState('');
  const [editLastName, setEditLastName] = useState('');
  const [editDob, setEditDob] = useState('');
  const [editGender, setEditGender] = useState('');
  const [editNotes, setEditNotes] = useState('');

  // Create guardian form
  const [gFirstName, setGFirstName] = useState('');
  const [gLastName, setGLastName] = useState('');
  const [gPhone, setGPhone] = useState('');
  const [gEmail, setGEmail] = useState('');
  const [gOccupation, setGOccupation] = useState('');

  // Link guardian form
  const [linkGuardianId, setLinkGuardianId] = useState('');
  const [linkRelation, setLinkRelation] = useState('Father');
  const [linkPrimary, setLinkPrimary] = useState(true);
  const [linkEmergency, setLinkEmergency] = useState(true);
  const [linkCustody, setLinkCustody] = useState(true);
  const [linkCollect, setLinkCollect] = useState(true);
  const [linkAcademic, setLinkAcademic] = useState(true);
  const [linkFinancial, setLinkFinancial] = useState(false);

  // Household form
  const [hhCode, setHhCode] = useState('');
  const [hhName, setHhName] = useState('');
  const [hhPrimaryGuardianId, setHhPrimaryGuardianId] = useState('');
  const [addMemberHouseholdId, setAddMemberHouseholdId] = useState('');
  const [addMemberStudentId, setAddMemberStudentId] = useState('');
  const [addMemberGuardianId, setAddMemberGuardianId] = useState('');
  const [addMemberRole, setAddMemberRole] = useState('Student');
  const [addMemberBilling, setAddMemberBilling] = useState(false);

  // Enrollment & Transfer forms
  const [enrollYearId, setEnrollYearId] = useState('');
  const [enrollGradeId, setEnrollGradeId] = useState('');
  const [enrollSectionId, setEnrollSectionId] = useState('');
  const [enrollRoll, setEnrollRoll] = useState('');
  const [enrollStartDate, setEnrollStartDate] = useState('2026-04-05');

  const [transferCampusId, setTransferCampusId] = useState('');
  const [transferYearId, setTransferYearId] = useState('');
  const [transferGradeId, setTransferGradeId] = useState('');
  const [transferSectionId, setTransferSectionId] = useState('');
  const [transferDate, setTransferDate] = useState('2026-08-15');
  const [transferRoll, setTransferRoll] = useState('');
  const [transferReason, setTransferReason] = useState('');
  const [confirmTransfer, setConfirmTransfer] = useState(false);

  // Status change form
  const [newStatus, setNewStatus] = useState('Inactive');
  const [statusReason, setStatusReason] = useState('');
  const [statusDate, setStatusDate] = useState('2026-05-01');
  const [confirmStatusChange, setConfirmStatusChange] = useState(false);

  const [reloadTick, setReloadTick] = useState(0);

  const loadWorkspace = useCallback(() => {
    setLoading(true);
    setLoadError(null);
    setReloadTick((n) => n + 1);
  }, []);

  useEffect(() => {
    let active = true;
    const run = async () => {
      try {
        const [studentsData, schoolsData, yearsData, gradesData, sectionsData, guardiansData, householdsData] =
          await Promise.all([
            schoolOsApi.listCanonicalStudents({
              search: searchTerm.trim() || undefined,
              status: statusFilter || undefined,
              academicYearId: yearFilter || undefined,
            }),
            schoolOsApi.listSchools().catch(() => []),
            schoolOsApi.listAcademicYears().catch(() => []),
            schoolOsApi.listGradeLevels().catch(() => []),
            schoolOsApi.listSections().catch(() => []),
            schoolOsApi.listGuardians().catch(() => []),
            schoolOsApi.listHouseholds().catch(() => []),
          ]);

        if (!active) return;
        setStudents(studentsData);
        const flatCampuses = schoolsData.flatMap((s) =>
          s.campuses.map((c) => ({ id: c.id, schoolId: s.id, name: `${s.name} — ${c.name}` }))
        );
        setCampuses(flatCampuses);
        setRegCampusId((prev) => prev || flatCampuses[0]?.id || '');
        setTransferCampusId((prev) => prev || flatCampuses[0]?.id || '');

        setAcademicYears(yearsData);
        const defaultYear = yearsData.find((y) => y.isCurrent)?.id || yearsData[0]?.id || '';
        setEnrollYearId((prev) => prev || defaultYear);
        setTransferYearId((prev) => prev || defaultYear);

        setGradeLevels(gradesData);
        setEnrollGradeId((prev) => prev || gradesData[0]?.id || '');
        setTransferGradeId((prev) => prev || gradesData[0]?.id || '');

        setSections(sectionsData);
        setGuardians(guardiansData);
        setLinkGuardianId((prev) => prev || guardiansData[0]?.id || '');
        setHouseholds(householdsData);
        setAddMemberHouseholdId((prev) => prev || householdsData[0]?.id || '');
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
  }, [reloadTick, searchTerm, statusFilter, yearFilter, t]);

  const loadStudentDetail = useCallback(
    async (id: string) => {
      setDetailLoading(true);
      setDetailError(null);
      try {
        const detail = await schoolOsApi.getStudentDetail(id);
        setSelectedDetail(detail);
        setEditFirstName(detail.firstName);
        setEditMiddleName(detail.middleName ?? '');
        setEditLastName(detail.lastName);
        setEditDob(detail.dateOfBirth);
        setEditGender(detail.gender ?? '');
        setEditNotes(detail.notes ?? '');
        setTransferCampusId(detail.campusId);
      } catch (err) {
        setDetailError(err instanceof Error ? err.message : t('students.errorLoad'));
      } finally {
        setDetailLoading(false);
      }
    },
    [t]
  );

  const handleSelectStudent = (id: string) => {
    setSelectedStudentId(id);
    setDetailTab('profile');
    setConfirmTransfer(false);
    setConfirmStatusChange(false);
    void loadStudentDetail(id);
  };

  const handleRegisterStudent = async (e: React.FormEvent) => {
    e.preventDefault();
    setRegError('');
    if (!regCampusId || !regAdmissionNo.trim() || !regFirstName.trim() || !regLastName.trim() || !regDob || !regAdmissionDate) {
      setRegError(t('students.validationRequired'));
      return;
    }

    setSubmitting(true);
    try {
      const created = await schoolOsApi.registerStudent({
        campusId: regCampusId,
        admissionNumber: regAdmissionNo.trim(),
        firstName: regFirstName.trim(),
        middleName: regMiddleName.trim() || undefined,
        lastName: regLastName.trim(),
        dateOfBirth: regDob,
        gender: regGender.trim() || undefined,
        admissionDate: regAdmissionDate,
        notes: regNotes.trim() || undefined,
      });
      setRegAdmissionNo('');
      setRegFirstName('');
      setRegMiddleName('');
      setRegLastName('');
      setRegNotes('');
      showToast(t('students.registeredSuccess'), 'success');
      setMainTab('directory');
      await loadWorkspace();
      handleSelectStudent(created.id);
    } catch (err) {
      const msg = err instanceof Error ? err.message : t('students.errorAction');
      setRegError(msg);
      showToast(msg, 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDetail) return;
    try {
      await schoolOsApi.updateStudent(selectedDetail.id, {
        firstName: editFirstName.trim(),
        middleName: editMiddleName.trim() || undefined,
        lastName: editLastName.trim(),
        dateOfBirth: editDob,
        gender: editGender.trim() || undefined,
        notes: editNotes.trim() || undefined,
      });
      showToast(t('students.updatedSuccess'), 'success');
      await Promise.all([loadWorkspace(), loadStudentDetail(selectedDetail.id)]);
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const handleCreateGuardian = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!gFirstName.trim() || !gLastName.trim() || !gPhone.trim()) return;
    try {
      await schoolOsApi.createGuardian({
        firstName: gFirstName.trim(),
        lastName: gLastName.trim(),
        primaryPhone: gPhone.trim(),
        email: gEmail.trim() || undefined,
        occupation: gOccupation.trim() || undefined,
        preferredChannel: 'SMS',
      });
      setGFirstName('');
      setGLastName('');
      setGPhone('');
      setGEmail('');
      setGOccupation('');
      showToast(t('students.guardianCreated'), 'success');
      await loadWorkspace();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const handleLinkGuardian = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDetail || !linkGuardianId) return;
    try {
      await schoolOsApi.linkStudentGuardian(selectedDetail.id, {
        guardianId: linkGuardianId,
        relationshipType: linkRelation,
        isPrimaryContact: linkPrimary,
        isEmergencyContact: linkEmergency,
        hasLegalCustody: linkCustody,
        canCollectStudent: linkCollect,
        receivesAcademicNotices: linkAcademic,
        receivesFinancialNotices: linkFinancial,
      });
      showToast(t('students.guardianLinked'), 'success');
      await loadStudentDetail(selectedDetail.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const handleUnlinkGuardian = async (guardianId: string) => {
    if (!selectedDetail) return;
    try {
      await schoolOsApi.unlinkStudentGuardian(selectedDetail.id, guardianId);
      showToast(t('students.guardianUnlinked'), 'success');
      await loadStudentDetail(selectedDetail.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const handleCreateHousehold = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!hhCode.trim() || !hhName.trim()) return;
    try {
      await schoolOsApi.createHousehold({
        householdCode: hhCode.trim(),
        displayName: hhName.trim(),
        primaryGuardianId: hhPrimaryGuardianId || null,
      });
      setHhCode('');
      setHhName('');
      showToast(t('students.householdCreated'), 'success');
      await loadWorkspace();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const handleAddHouseholdMember = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!addMemberHouseholdId || (!addMemberStudentId && !addMemberGuardianId)) return;
    try {
      await schoolOsApi.addHouseholdMember(addMemberHouseholdId, {
        studentId: addMemberStudentId || null,
        guardianId: addMemberStudentId ? null : addMemberGuardianId || null,
        roleLabel: addMemberRole.trim() || 'Member',
        isBillingMember: addMemberBilling,
      });
      showToast(t('students.householdMemberAdded'), 'success');
      await loadWorkspace();
      if (selectedDetail) await loadStudentDetail(selectedDetail.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const handleEnroll = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDetail || !enrollYearId || !enrollGradeId) return;
    try {
      await schoolOsApi.enrollStudent(selectedDetail.id, {
        campusId: selectedDetail.campusId,
        academicYearId: enrollYearId,
        gradeLevelId: enrollGradeId,
        sectionId: enrollSectionId || null,
        rollNumber: enrollRoll.trim() || undefined,
        startDate: enrollStartDate,
      });
      setEnrollRoll('');
      showToast(t('students.enrolledSuccess'), 'success');
      await loadStudentDetail(selectedDetail.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const handleTransfer = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDetail || !transferCampusId || !transferYearId || !transferGradeId) return;
    if (!confirmTransfer) {
      setConfirmTransfer(true);
      return;
    }
    try {
      await schoolOsApi.transferStudent(selectedDetail.id, {
        targetCampusId: transferCampusId,
        academicYearId: transferYearId,
        targetGradeLevelId: transferGradeId,
        targetSectionId: transferSectionId || null,
        effectiveDate: transferDate,
        rollNumber: transferRoll.trim() || undefined,
        reason: transferReason.trim() || 'Transfer',
      });
      setConfirmTransfer(false);
      setTransferReason('');
      showToast(t('students.transferredSuccess'), 'success');
      await Promise.all([loadWorkspace(), loadStudentDetail(selectedDetail.id)]);
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const handleStatusChange = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDetail) return;
    if (!confirmStatusChange) {
      setConfirmStatusChange(true);
      return;
    }
    try {
      await schoolOsApi.changeStudentStatus(selectedDetail.id, {
        targetStatus: newStatus,
        reason: statusReason.trim() || 'Status update',
        effectiveDate: statusDate,
      });
      setConfirmStatusChange(false);
      setStatusReason('');
      showToast(t('students.statusUpdated'), 'success');
      await Promise.all([loadWorkspace(), loadStudentDetail(selectedDetail.id)]);
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('students.errorAction'), 'error');
    }
  };

  const yearLabelMap = useMemo(
    () => new Map(academicYears.map((y) => [y.id, `${y.name} (${y.code})`])),
    [academicYears]
  );
  const gradeLabelMap = useMemo(
    () => new Map(gradeLevels.map((g) => [g.id, g.name])),
    [gradeLevels]
  );
  const sectionLabelMap = useMemo(
    () => new Map(sections.map((s) => [s.id, `${s.name} (${s.code})`])),
    [sections]
  );

  const enrollSections = useMemo(
    () =>
      sections.filter(
        (s) =>
          (!selectedDetail || s.campusId === selectedDetail.campusId) &&
          (!enrollYearId || s.academicYearId === enrollYearId) &&
          (!enrollGradeId || s.gradeLevelId === enrollGradeId)
      ),
    [sections, selectedDetail, enrollYearId, enrollGradeId]
  );

  const transferSections = useMemo(
    () =>
      sections.filter(
        (s) =>
          (!transferCampusId || s.campusId === transferCampusId) &&
          (!transferYearId || s.academicYearId === transferYearId) &&
          (!transferGradeId || s.gradeLevelId === transferGradeId)
      ),
    [sections, transferCampusId, transferYearId, transferGradeId]
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('students.title')}
        subtitle={`${t('students.subtitle')} (${formatNumber(students.length)})`}
        breadcrumbs={[{ label: 'SchoolOS', path: '/students' }, { label: t('students.title') }]}
      />

      <SegmentedTabs<MainTab>
        value={mainTab}
        onChange={setMainTab}
        options={[
          { value: 'directory', label: t('students.tabs.directory') },
          { value: 'register', label: t('students.tabs.register') },
          { value: 'guardians', label: t('students.tabs.guardians') },
          { value: 'households', label: t('students.tabs.households') },
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
          <Button type="button" variant="outline" size="sm" onClick={() => void loadWorkspace()}>
            {t('students.retry')}
          </Button>
        </Card>
      )}

      {!loading && !loadError && mainTab === 'directory' && (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
          <div className="space-y-4 lg:col-span-5">
            <Card className="space-y-3">
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <FormInput
                  label={t('students.search')}
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder={t('students.searchPlaceholder')}
                />
                <SelectInput
                  label={t('students.status')}
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value)}
                  options={[
                    { value: '', label: t('students.allStatuses') },
                    ...LIFECYCLE_STATUS_OPTIONS.map((s) => ({ value: s, label: s })),
                  ]}
                />
                <SelectInput
                  label={t('students.academicYear')}
                  value={yearFilter}
                  onChange={(e) => setYearFilter(e.target.value)}
                  options={[
                    { value: '', label: t('students.allYears') },
                    ...academicYears.map((y) => ({ value: y.id, label: y.name })),
                  ]}
                />
              </div>
            </Card>

            <Card className="space-y-2">
              {students.length === 0 ? (
                <p className="text-sm text-muted">{t('students.emptyList')}</p>
              ) : (
                students.map((s) => (
                  <button
                    key={s.id}
                    type="button"
                    onClick={() => handleSelectStudent(s.id)}
                    className={`flex w-full items-center justify-between gap-3 rounded-2xl border p-4 text-start transition-colors ${
                      selectedStudentId === s.id
                        ? 'border-primary bg-light-primary/30'
                        : 'border-border hover:bg-subtle'
                    }`}
                  >
                    <div>
                      <p className="text-sm font-bold text-ink">
                        {s.firstName} {s.lastName}
                      </p>
                      <p className="text-xs text-muted">
                        {s.admissionNumber} · {s.dateOfBirth}
                      </p>
                    </div>
                    <StatusBadge
                      label={s.status}
                      tone={s.status === 'Active' ? 'success' : s.status === 'Graduated' ? 'primary' : 'warning'}
                    />
                  </button>
                ))
              )}
            </Card>
          </div>

          <div className="lg:col-span-7">
            {!selectedStudentId && (
              <Card>
                <p className="text-sm text-muted">{t('students.selectPrompt')}</p>
              </Card>
            )}

            {selectedStudentId && detailLoading && (
              <Card>
                <p className="text-sm text-muted">{t('loading.label')}</p>
              </Card>
            )}

            {selectedStudentId && !detailLoading && detailError && (
              <Card className="flex items-center justify-between gap-3 border-error/40">
                <p role="alert" className="text-sm text-error">{detailError}</p>
                <Button type="button" size="sm" variant="outline" onClick={() => void loadStudentDetail(selectedStudentId)}>
                  {t('students.retry')}
                </Button>
              </Card>
            )}

            {selectedDetail && !detailLoading && !detailError && (
              <Card className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3">
                  <div>
                    <h3 className="text-lg font-bold text-ink">
                      {selectedDetail.firstName} {selectedDetail.middleName ? `${selectedDetail.middleName} ` : ''}
                      {selectedDetail.lastName}
                    </h3>
                    <p className="text-xs text-muted">
                      {selectedDetail.admissionNumber} · {t('students.admissionDate')}: {selectedDetail.admissionDate}
                    </p>
                  </div>
                  <StatusBadge
                    label={selectedDetail.status}
                    tone={selectedDetail.status === 'Active' ? 'success' : 'warning'}
                  />
                </div>

                <SegmentedTabs<DetailTab>
                  value={detailTab}
                  onChange={setDetailTab}
                  options={[
                    { value: 'profile', label: t('students.detail.profile') },
                    { value: 'guardians', label: t('students.detail.guardians') },
                    { value: 'enrollments', label: t('students.detail.enrollments') },
                    { value: 'status', label: t('students.detail.status') },
                    { value: 'households', label: t('students.detail.households') },
                  ]}
                />

                {detailTab === 'profile' && (
                  <form onSubmit={handleUpdateProfile} className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                    <FormInput label={t('students.firstName')} value={editFirstName} onChange={(e) => setEditFirstName(e.target.value)} required disabled={!canEdit} />
                    <FormInput label={t('students.middleName')} value={editMiddleName} onChange={(e) => setEditMiddleName(e.target.value)} disabled={!canEdit} />
                    <FormInput label={t('students.lastName')} value={editLastName} onChange={(e) => setEditLastName(e.target.value)} required disabled={!canEdit} />
                    <FormInput label={t('students.dateOfBirth')} type="date" value={editDob} onChange={(e) => setEditDob(e.target.value)} required disabled={!canEdit} />
                    <FormInput label={t('students.gender')} value={editGender} onChange={(e) => setEditGender(e.target.value)} disabled={!canEdit} />
                    <FormInput label={t('students.notes')} value={editNotes} onChange={(e) => setEditNotes(e.target.value)} disabled={!canEdit} />
                    {canEdit && (
                      <div className="sm:col-span-2">
                        <Button type="submit">{t('students.saveProfile')}</Button>
                      </div>
                    )}
                  </form>
                )}

                {detailTab === 'guardians' && (
                  <div className="space-y-4">
                    <div className="space-y-2">
                      {selectedDetail.guardians.length === 0 ? (
                        <p className="text-sm text-muted">{t('students.noGuardians')}</p>
                      ) : (
                        selectedDetail.guardians.map((g) => (
                          <div key={g.id} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-border p-3">
                            <div>
                              <p className="text-sm font-semibold text-ink">
                                {g.firstName} {g.lastName} ({g.relationshipType})
                              </p>
                              <p className="text-xs text-muted">{g.primaryPhone} {g.email ? `· ${g.email}` : ''}</p>
                              <div className="mt-1 flex flex-wrap gap-1 text-[11px]">
                                {g.isPrimaryContact && <StatusBadge label={t('students.flagPrimary')} tone="primary" />}
                                {g.isEmergencyContact && <StatusBadge label={t('students.flagEmergency')} tone="warning" />}
                                {g.hasLegalCustody && <StatusBadge label={t('students.flagCustody')} tone="success" />}
                                {g.canCollectStudent && <StatusBadge label={t('students.flagPickup')} tone="primary" />}
                                {g.receivesFinancialNotices && <StatusBadge label={t('students.flagFinancial')} tone="info" />}
                              </div>
                            </div>
                            {canEdit && (
                              <Button type="button" size="sm" variant="outline" onClick={() => void handleUnlinkGuardian(g.guardianId)}>
                                {t('students.unlink')}
                              </Button>
                            )}
                          </div>
                        ))
                      )}
                    </div>

                    {canEdit && (
                      <form onSubmit={handleLinkGuardian} className="space-y-3 rounded-xl border border-border p-3">
                        <p className="text-xs font-bold uppercase text-muted">{t('students.linkGuardianTitle')}</p>
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                          <SelectInput
                            label={t('students.guardian')}
                            value={linkGuardianId}
                            onChange={(e) => setLinkGuardianId(e.target.value)}
                            options={[
                              { value: '', label: t('students.selectGuardian') },
                              ...guardians.map((g) => ({ value: g.id, label: `${g.firstName} ${g.lastName} (${g.primaryPhone})` })),
                            ]}
                            required
                          />
                          <SelectInput
                            label={t('students.relationship')}
                            value={linkRelation}
                            onChange={(e) => setLinkRelation(e.target.value)}
                            options={RELATIONSHIP_OPTIONS.map((r) => ({ value: r, label: r }))}
                          />
                        </div>
                        <div className="grid grid-cols-2 gap-2 text-xs sm:grid-cols-3">
                          <label className="flex items-center gap-2"><input type="checkbox" checked={linkPrimary} onChange={(e) => setLinkPrimary(e.target.checked)} />{t('students.flagPrimary')}</label>
                          <label className="flex items-center gap-2"><input type="checkbox" checked={linkEmergency} onChange={(e) => setLinkEmergency(e.target.checked)} />{t('students.flagEmergency')}</label>
                          <label className="flex items-center gap-2"><input type="checkbox" checked={linkCustody} onChange={(e) => setLinkCustody(e.target.checked)} />{t('students.flagCustody')}</label>
                          <label className="flex items-center gap-2"><input type="checkbox" checked={linkCollect} onChange={(e) => setLinkCollect(e.target.checked)} />{t('students.flagPickup')}</label>
                          <label className="flex items-center gap-2"><input type="checkbox" checked={linkAcademic} onChange={(e) => setLinkAcademic(e.target.checked)} />{t('students.flagAcademic')}</label>
                          <label className="flex items-center gap-2"><input type="checkbox" checked={linkFinancial} onChange={(e) => setLinkFinancial(e.target.checked)} />{t('students.flagFinancial')}</label>
                        </div>
                        <Button type="submit" size="sm" disabled={!linkGuardianId}>{t('students.linkGuardianSubmit')}</Button>
                      </form>
                    )}
                  </div>
                )}

                {detailTab === 'enrollments' && (
                  <div className="space-y-4">
                    <div className="space-y-2">
                      <p className="text-xs font-bold uppercase text-muted">{t('students.enrollmentHistory')}</p>
                      {selectedDetail.enrollments.length === 0 ? (
                        <p className="text-sm text-muted">{t('students.noEnrollments')}</p>
                      ) : (
                        selectedDetail.enrollments.map((en) => (
                          <div key={en.id} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-border p-3">
                            <div>
                              <p className="text-sm font-semibold text-ink">
                                {yearLabelMap.get(en.academicYearId) ?? en.academicYearId} · {gradeLabelMap.get(en.gradeLevelId) ?? en.gradeLevelId}
                                {en.sectionId ? ` · ${sectionLabelMap.get(en.sectionId) ?? en.sectionId}` : ''}
                              </p>
                              <p className="text-xs text-muted">
                                {en.startDate}{en.endDate ? ` → ${en.endDate}` : ''}
                                {en.rollNumber ? ` · #${en.rollNumber}` : ''}
                                {en.reason ? ` · ${en.reason}` : ''}
                              </p>
                            </div>
                            <StatusBadge label={en.status} tone={en.status === 'Active' ? 'success' : 'warning'} />
                          </div>
                        ))
                      )}
                    </div>

                    {canCreate && (
                      <form onSubmit={handleEnroll} className="space-y-3 rounded-xl border border-border p-3">
                        <p className="text-xs font-bold uppercase text-muted">{t('students.enrollTitle')}</p>
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                          <SelectInput
                            label={t('students.academicYear')}
                            value={enrollYearId}
                            onChange={(e) => setEnrollYearId(e.target.value)}
                            options={[{ value: '', label: t('students.selectYear') }, ...academicYears.map((y) => ({ value: y.id, label: y.name }))]}
                            required
                          />
                          <SelectInput
                            label={t('students.gradeLevel')}
                            value={enrollGradeId}
                            onChange={(e) => setEnrollGradeId(e.target.value)}
                            options={[{ value: '', label: t('students.selectGrade') }, ...gradeLevels.map((g) => ({ value: g.id, label: g.name }))]}
                            required
                          />
                          <SelectInput
                            label={t('students.section')}
                            value={enrollSectionId}
                            onChange={(e) => setEnrollSectionId(e.target.value)}
                            options={[{ value: '', label: t('students.selectSection') }, ...enrollSections.map((s) => ({ value: s.id, label: s.name }))]}
                          />
                          <FormInput label={t('students.rollNumber')} value={enrollRoll} onChange={(e) => setEnrollRoll(e.target.value)} />
                          <FormInput label={t('students.startDate')} type="date" value={enrollStartDate} onChange={(e) => setEnrollStartDate(e.target.value)} required />
                        </div>
                        <Button type="submit" size="sm" disabled={!enrollYearId || !enrollGradeId}>{t('students.enrollSubmit')}</Button>
                      </form>
                    )}

                    {canEdit && (
                      <form onSubmit={handleTransfer} className="space-y-3 rounded-xl border border-border p-3">
                        <p className="text-xs font-bold uppercase text-muted">{t('students.transferTitle')}</p>
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                          <SelectInput
                            label={t('students.targetCampus')}
                            value={transferCampusId}
                            onChange={(e) => setTransferCampusId(e.target.value)}
                            options={campuses.map((c) => ({ value: c.id, label: c.name }))}
                            required
                          />
                          <SelectInput
                            label={t('students.academicYear')}
                            value={transferYearId}
                            onChange={(e) => setTransferYearId(e.target.value)}
                            options={academicYears.map((y) => ({ value: y.id, label: y.name }))}
                            required
                          />
                          <SelectInput
                            label={t('students.gradeLevel')}
                            value={transferGradeId}
                            onChange={(e) => setTransferGradeId(e.target.value)}
                            options={gradeLevels.map((g) => ({ value: g.id, label: g.name }))}
                            required
                          />
                          <SelectInput
                            label={t('students.section')}
                            value={transferSectionId}
                            onChange={(e) => setTransferSectionId(e.target.value)}
                            options={[{ value: '', label: t('students.selectSection') }, ...transferSections.map((s) => ({ value: s.id, label: s.name }))]}
                          />
                          <FormInput label={t('students.effectiveDate')} type="date" value={transferDate} onChange={(e) => setTransferDate(e.target.value)} required />
                          <FormInput label={t('students.rollNumber')} value={transferRoll} onChange={(e) => setTransferRoll(e.target.value)} />
                          <FormInput label={t('students.reason')} value={transferReason} onChange={(e) => setTransferReason(e.target.value)} />
                        </div>
                        {confirmTransfer ? (
                          <div className="flex flex-wrap items-center gap-2">
                            <Button type="submit" size="sm">{t('students.confirmTransfer')}</Button>
                            <Button type="button" size="sm" variant="outline" onClick={() => setConfirmTransfer(false)}>{t('students.cancel')}</Button>
                          </div>
                        ) : (
                          <Button type="submit" size="sm" variant="outline">{t('students.transferSubmit')}</Button>
                        )}
                      </form>
                    )}
                  </div>
                )}

                {detailTab === 'status' && (
                  <div className="space-y-4">
                    <div className="space-y-2">
                      {selectedDetail.statusHistory.map((h) => (
                        <div key={h.id} className="rounded-xl border border-border p-3 text-xs">
                          <span className="font-bold text-ink">{h.fromStatus ? `${h.fromStatus} → ${h.toStatus}` : h.toStatus}</span>
                          <span className="ms-2 text-muted">{h.effectiveDate}</span>
                          {h.reason && <p className="mt-1 text-muted">{h.reason}</p>}
                        </div>
                      ))}
                    </div>

                    {canEdit && (
                      <form onSubmit={handleStatusChange} className="space-y-3 rounded-xl border border-border p-3">
                        <p className="text-xs font-bold uppercase text-muted">{t('students.changeStatusTitle')}</p>
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                          <SelectInput
                            label={t('students.status')}
                            value={newStatus}
                            onChange={(e) => {
                              setNewStatus(e.target.value);
                              setConfirmStatusChange(false);
                            }}
                            options={LIFECYCLE_STATUS_OPTIONS.map((s) => ({ value: s, label: s }))}
                          />
                          <FormInput label={t('students.effectiveDate')} type="date" value={statusDate} onChange={(e) => setStatusDate(e.target.value)} required />
                          <FormInput label={t('students.reason')} value={statusReason} onChange={(e) => setStatusReason(e.target.value)} />
                        </div>
                        {confirmStatusChange ? (
                          <div className="flex flex-wrap items-center gap-2">
                            <Button type="submit" size="sm">{t('students.confirmStatus')}</Button>
                            <Button type="button" size="sm" variant="outline" onClick={() => setConfirmStatusChange(false)}>{t('students.cancel')}</Button>
                          </div>
                        ) : (
                          <Button type="submit" size="sm" variant="outline">{t('students.changeStatusSubmit')}</Button>
                        )}
                      </form>
                    )}
                  </div>
                )}

                {detailTab === 'households' && (
                  <div className="space-y-2">
                    {selectedDetail.households.length === 0 ? (
                      <p className="text-sm text-muted">{t('students.noHouseholds')}</p>
                    ) : (
                      selectedDetail.households.map((h) => (
                        <div key={h.householdId} className="flex items-center justify-between rounded-xl border border-border p-3">
                          <div>
                            <p className="text-sm font-semibold text-ink">{h.displayName}</p>
                            <p className="text-xs text-muted">{h.householdCode} · {h.roleLabel}</p>
                          </div>
                          {h.isBillingMember && <StatusBadge label={t('students.billingMember')} tone="primary" />}
                        </div>
                      ))
                    )}
                  </div>
                )}
              </Card>
            )}
          </div>
        </div>
      )}

      {!loading && !loadError && mainTab === 'register' && (
        <Card>
          {!canCreate ? (
            <p className="text-sm text-muted">{t('students.noPermissionCreate')}</p>
          ) : (
            <form onSubmit={handleRegisterStudent} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <SelectInput
                label={t('students.campus')}
                value={regCampusId}
                onChange={(e) => setRegCampusId(e.target.value)}
                options={[{ value: '', label: t('students.selectCampus') }, ...campuses.map((c) => ({ value: c.id, label: c.name }))]}
                required
              />
              <FormInput label={t('students.admissionNumber')} value={regAdmissionNo} onChange={(e) => setRegAdmissionNo(e.target.value)} required />
              <FormInput label={t('students.firstName')} value={regFirstName} onChange={(e) => setRegFirstName(e.target.value)} required />
              <FormInput label={t('students.middleName')} value={regMiddleName} onChange={(e) => setRegMiddleName(e.target.value)} />
              <FormInput label={t('students.lastName')} value={regLastName} onChange={(e) => setRegLastName(e.target.value)} required />
              <FormInput label={t('students.dateOfBirth')} type="date" value={regDob} onChange={(e) => setRegDob(e.target.value)} required />
              <FormInput label={t('students.gender')} value={regGender} onChange={(e) => setRegGender(e.target.value)} />
              <FormInput label={t('students.admissionDate')} type="date" value={regAdmissionDate} onChange={(e) => setRegAdmissionDate(e.target.value)} required />
              <div className="sm:col-span-2">
                <FormInput label={t('students.notes')} value={regNotes} onChange={(e) => setRegNotes(e.target.value)} />
              </div>
              {regError && (
                <p role="alert" className="sm:col-span-2 rounded-xl bg-light-error px-3 py-2 text-xs font-semibold text-error">
                  {regError}
                </p>
              )}
              <div className="sm:col-span-2">
                <Button type="submit" disabled={submitting}>
                  {submitting ? t('loading.label') : t('students.registerSubmit')}
                </Button>
              </div>
            </form>
          )}
        </Card>
      )}

      {!loading && !loadError && mainTab === 'guardians' && (
        <div className="space-y-4">
          {canCreate && (
            <Card>
              <form onSubmit={handleCreateGuardian} className="grid grid-cols-1 gap-3 sm:grid-cols-5 sm:items-end">
                <FormInput label={t('students.firstName')} value={gFirstName} onChange={(e) => setGFirstName(e.target.value)} required />
                <FormInput label={t('students.lastName')} value={gLastName} onChange={(e) => setGLastName(e.target.value)} required />
                <FormInput label={t('people.phone')} value={gPhone} onChange={(e) => setGPhone(e.target.value)} required />
                <FormInput label={t('login.email')} type="email" value={gEmail} onChange={(e) => setGEmail(e.target.value)} />
                <Button type="submit">{t('students.createGuardian')}</Button>
              </form>
            </Card>
          )}
          <Card className="space-y-2">
            {guardians.length === 0 ? (
              <p className="text-sm text-muted">{t('students.noGuardians')}</p>
            ) : (
              guardians.map((g) => (
                <div key={g.id} className="flex flex-wrap items-center justify-between rounded-xl border border-border p-3">
                  <div>
                    <p className="text-sm font-bold text-ink">{g.firstName} {g.lastName}</p>
                    <p className="text-xs text-muted">{g.primaryPhone} {g.email ? `· ${g.email}` : ''}</p>
                  </div>
                  <StatusBadge label={g.preferredChannel} tone="primary" />
                </div>
              ))
            )}
          </Card>
        </div>
      )}

      {!loading && !loadError && mainTab === 'households' && (
        <div className="space-y-4">
          {canCreate && (
            <Card>
              <form onSubmit={handleCreateHousehold} className="grid grid-cols-1 gap-3 sm:grid-cols-4 sm:items-end">
                <FormInput label={t('students.householdCode')} value={hhCode} onChange={(e) => setHhCode(e.target.value)} required />
                <FormInput label={t('students.householdName')} value={hhName} onChange={(e) => setHhName(e.target.value)} required />
                <SelectInput
                  label={t('students.primaryGuardian')}
                  value={hhPrimaryGuardianId}
                  onChange={(e) => setHhPrimaryGuardianId(e.target.value)}
                  options={[{ value: '', label: t('students.selectGuardian') }, ...guardians.map((g) => ({ value: g.id, label: `${g.firstName} ${g.lastName}` }))]}
                />
                <Button type="submit">{t('students.createHousehold')}</Button>
              </form>
            </Card>
          )}

          {canEdit && households.length > 0 && (
            <Card>
              <form onSubmit={handleAddHouseholdMember} className="grid grid-cols-1 gap-3 sm:grid-cols-5 sm:items-end">
                <SelectInput
                  label={t('students.household')}
                  value={addMemberHouseholdId}
                  onChange={(e) => setAddMemberHouseholdId(e.target.value)}
                  options={households.map((h) => ({ value: h.id, label: `${h.displayName} (${h.householdCode})` }))}
                />
                <SelectInput
                  label={t('students.studentMember')}
                  value={addMemberStudentId}
                  onChange={(e) => {
                    setAddMemberStudentId(e.target.value);
                    if (e.target.value) setAddMemberGuardianId('');
                  }}
                  options={[{ value: '', label: t('students.none') }, ...students.map((s) => ({ value: s.id, label: `${s.firstName} ${s.lastName} (${s.admissionNumber})` }))]}
                />
                <SelectInput
                  label={t('students.guardianMember')}
                  value={addMemberGuardianId}
                  onChange={(e) => {
                    setAddMemberGuardianId(e.target.value);
                    if (e.target.value) setAddMemberStudentId('');
                  }}
                  options={[{ value: '', label: t('students.none') }, ...guardians.map((g) => ({ value: g.id, label: `${g.firstName} ${g.lastName}` }))]}
                />
                <FormInput label={t('students.roleLabel')} value={addMemberRole} onChange={(e) => setAddMemberRole(e.target.value)} />
                <div className="flex items-center gap-2">
                  <label className="flex items-center gap-1 text-xs">
                    <input type="checkbox" checked={addMemberBilling} onChange={(e) => setAddMemberBilling(e.target.checked)} />
                    {t('students.billingMember')}
                  </label>
                  <Button type="submit" size="sm">{t('students.addMember')}</Button>
                </div>
              </form>
            </Card>
          )}

          <Card className="space-y-2">
            {households.length === 0 ? (
              <p className="text-sm text-muted">{t('students.noHouseholds')}</p>
            ) : (
              households.map((h) => (
                <div key={h.id} className="rounded-xl border border-border p-3">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-bold text-ink">{h.displayName}</p>
                    <StatusBadge label={`${formatNumber(h.members.length)} ${t('students.members')}`} tone="primary" />
                  </div>
                  <p className="text-xs text-muted">{h.householdCode}</p>
                </div>
              ))
            )}
          </Card>
        </div>
      )}
    </div>
  );
}
