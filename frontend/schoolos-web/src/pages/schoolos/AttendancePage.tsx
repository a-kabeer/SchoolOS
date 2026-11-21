import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, FormInput, SegmentedTabs, SelectInput, StatusBadge } from '@/components/ui';
import {
  schoolOsApi,
  type AcademicYearDto,
  type AttendanceCorrectionDto,
  type AttendanceStatusValue,
  type AttendanceSummaryDto,
  type GradeLevelDto,
  type SectionDto,
  type SectionRegisterViewDto,
  type StaffMemberDto,
  type TeacherAssignmentDto,
} from '@/api/client';
import { useAuth } from '@/context/AuthContext';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

type AttendanceTab = 'rollcall' | 'staff' | 'summary';

interface CampusOption {
  id: string;
  name: string;
}

interface DraftMark {
  status: AttendanceStatusValue;
  remarks: string;
  correctionReason: string;
}

const ATTENDANCE_STATUSES: AttendanceStatusValue[] = ['Present', 'Absent', 'Late', 'Excused', 'HalfDay'];
const STAFF_ROLES = ['Teacher', 'HomeroomTeacher', 'Coordinator', 'CampusStaff', 'Principal'];

export function AttendancePage() {
  const { t, formatNumber } = useLocale();
  const { showToast } = useToast();
  const { hasPermission } = useAuth();

  const canMark = hasPermission('attendance:create');
  const canCreateStaff = hasPermission('staff:create');
  const canAssignStaff = hasPermission('staff:edit');

  const [tab, setTab] = useState<AttendanceTab>('rollcall');
  const [reloadTick, setReloadTick] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [campuses, setCampuses] = useState<CampusOption[]>([]);
  const [academicYears, setAcademicYears] = useState<AcademicYearDto[]>([]);
  const [gradeLevels, setGradeLevels] = useState<GradeLevelDto[]>([]);
  const [sections, setSections] = useState<SectionDto[]>([]);
  const [staffMembers, setStaffMembers] = useState<StaffMemberDto[]>([]);
  const [assignments, setAssignments] = useState<TeacherAssignmentDto[]>([]);
  const [corrections, setCorrections] = useState<AttendanceCorrectionDto[]>([]);

  // Roll call controls
  const [selectedSectionId, setSelectedSectionId] = useState('');
  const [selectedDate, setSelectedDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [registerTick, setRegisterTick] = useState(0);
  const [registerLoading, setRegisterLoading] = useState(false);
  const [registerError, setRegisterError] = useState<string | null>(null);
  const [register, setRegister] = useState<SectionRegisterViewDto | null>(null);
  const [registerNotes, setRegisterNotes] = useState('');
  const [draftMarks, setDraftMarks] = useState<Record<string, DraftMark>>({});
  const [savingRegister, setSavingRegister] = useState(false);

  // Summary state
  const [summary, setSummary] = useState<AttendanceSummaryDto | null>(null);

  // Staff creation form
  const [staffCampusId, setStaffCampusId] = useState('');
  const [empCode, setEmpCode] = useState('');
  const [staffFirst, setStaffFirst] = useState('');
  const [staffLast, setStaffLast] = useState('');
  const [staffRole, setStaffRole] = useState('Teacher');
  const [staffEmail, setStaffEmail] = useState('');

  // Teacher section assignment form
  const [assignStaffId, setAssignStaffId] = useState('');
  const [assignYearId, setAssignYearId] = useState('');
  const [assignGradeId, setAssignGradeId] = useState('');
  const [assignSectionId, setAssignSectionId] = useState('');
  const [assignHomeroom, setAssignHomeroom] = useState(true);

  const refreshWorkspace = useCallback(() => {
    setLoading(true);
    setLoadError(null);
    setReloadTick((n) => n + 1);
  }, []);

  const refreshRegister = useCallback(() => {
    setRegisterLoading(true);
    setRegisterError(null);
    setRegisterTick((n) => n + 1);
  }, []);

  useEffect(() => {
    let active = true;
    const run = async () => {
      try {
        const [schoolsData, yearsData, gradesData, sectionsData, staffData, assignmentsData, correctionsData] =
          await Promise.all([
            schoolOsApi.listSchools(),
            schoolOsApi.listAcademicYears(),
            schoolOsApi.listGradeLevels(),
            schoolOsApi.listSections(),
            schoolOsApi.listStaffMembers().catch(() => []),
            schoolOsApi.listTeacherAssignments().catch(() => []),
            schoolOsApi.listAttendanceCorrections().catch(() => []),
          ]);

        if (!active) return;
        const flatCampuses = schoolsData.flatMap((s) =>
          s.campuses.map((c) => ({ id: c.id, name: `${s.name} — ${c.name}` }))
        );
        setCampuses(flatCampuses);
        setStaffCampusId((prev) => prev || flatCampuses[0]?.id || '');

        setAcademicYears(yearsData);
        const defaultYear = yearsData.find((y) => y.isCurrent)?.id || yearsData[0]?.id || '';
        setAssignYearId((prev) => prev || defaultYear);

        setGradeLevels(gradesData);
        setAssignGradeId((prev) => prev || gradesData[0]?.id || '');

        setSections(sectionsData);
        setSelectedSectionId((prev) => prev || sectionsData[0]?.id || '');
        setAssignSectionId((prev) => prev || sectionsData[0]?.id || '');

        setStaffMembers(staffData);
        setAssignStaffId((prev) => prev || staffData[0]?.id || '');
        setAssignments(assignmentsData);
        setCorrections(correctionsData);
      } catch (err) {
        if (!active) return;
        setLoadError(err instanceof Error ? err.message : t('attendance.errorLoad'));
      } finally {
        if (active) setLoading(false);
      }
    };
    void run();
    return () => {
      active = false;
    };
  }, [reloadTick, t]);

  useEffect(() => {
    if (!selectedSectionId || !selectedDate) return;
    let active = true;
    const run = async () => {
      try {
        const [reg, sum] = await Promise.all([
          schoolOsApi.getSectionAttendanceRegister(selectedSectionId, selectedDate),
          schoolOsApi.getSectionAttendanceSummary(selectedSectionId).catch(() => null),
        ]);
        if (!active) return;
        setRegister(reg);
        setSummary(sum);
        setRegisterNotes(reg.notes ?? '');
        const nextDrafts: Record<string, DraftMark> = {};
        for (const s of reg.students) {
          nextDrafts[s.studentId] = {
            status: s.status ?? 'Present',
            remarks: s.remarks ?? '',
            correctionReason: '',
          };
        }
        setDraftMarks(nextDrafts);
      } catch (err) {
        if (!active) return;
        setRegisterError(err instanceof Error ? err.message : t('attendance.errorLoad'));
      } finally {
        if (active) setRegisterLoading(false);
      }
    };
    void run();
    return () => {
      active = false;
    };
  }, [selectedSectionId, selectedDate, registerTick, t]);

  const handleMarkAllPresent = () => {
    if (!register) return;
    setDraftMarks((prev) => {
      const updated = { ...prev };
      for (const s of register.students) {
        updated[s.studentId] = {
          status: 'Present',
          remarks: updated[s.studentId]?.remarks ?? '',
          correctionReason: updated[s.studentId]?.correctionReason ?? '',
        };
      }
      return updated;
    });
  };

  const handleSaveAttendance = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!register || !selectedSectionId || register.students.length === 0) return;
    setSavingRegister(true);
    try {
      const entries = register.students.map((s) => {
        const draft = draftMarks[s.studentId] ?? { status: 'Present' as AttendanceStatusValue, remarks: '', correctionReason: '' };
        return {
          studentId: s.studentId,
          status: draft.status,
          remarks: draft.remarks.trim() || undefined,
          correctionReason: draft.correctionReason.trim() || undefined,
        };
      });

      const result = await schoolOsApi.markSectionAttendance(selectedSectionId, {
        attendanceDate: selectedDate,
        notes: registerNotes.trim() || undefined,
        entries,
      });

      showToast(
        `${t('attendance.savedSuccess')} (${formatNumber(result.createdCount + result.updatedCount)})`,
        'success'
      );
      refreshRegister();
      refreshWorkspace();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('attendance.errorSave'), 'error');
    } finally {
      setSavingRegister(false);
    }
  };

  const handleCreateStaff = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!staffCampusId || !empCode.trim() || !staffFirst.trim() || !staffLast.trim()) return;
    try {
      await schoolOsApi.createStaffMember({
        campusId: staffCampusId,
        employeeCode: empCode.trim(),
        firstName: staffFirst.trim(),
        lastName: staffLast.trim(),
        roleType: staffRole,
        email: staffEmail.trim() || undefined,
      });
      setEmpCode('');
      setStaffFirst('');
      setStaffLast('');
      setStaffEmail('');
      showToast(t('attendance.staffCreated'), 'success');
      refreshWorkspace();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('attendance.errorSave'), 'error');
    }
  };

  const handleAssignTeacher = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!assignStaffId || !assignYearId || !assignGradeId || !assignSectionId) return;
    try {
      await schoolOsApi.assignTeacherSection(assignStaffId, {
        academicYearId: assignYearId,
        gradeLevelId: assignGradeId,
        sectionId: assignSectionId,
        isHomeroomTeacher: assignHomeroom,
      });
      showToast(t('attendance.assignmentSaved'), 'success');
      refreshWorkspace();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('attendance.errorSave'), 'error');
    }
  };

  const sectionLabelMap = useMemo(
    () => new Map(sections.map((s) => [s.id, `${s.name} (${s.code})`])),
    [sections]
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('attendance.title')}
        subtitle={t('attendance.subtitle')}
        breadcrumbs={[{ label: 'SchoolOS', path: '/attendance' }, { label: t('attendance.title') }]}
      />

      <SegmentedTabs<AttendanceTab>
        value={tab}
        onChange={setTab}
        options={[
          { value: 'rollcall', label: t('attendance.tabs.rollcall') },
          { value: 'staff', label: t('attendance.tabs.staff') },
          { value: 'summary', label: t('attendance.tabs.summary') },
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
          <Button type="button" variant="outline" size="sm" onClick={refreshWorkspace}>
            {t('students.retry')}
          </Button>
        </Card>
      )}

      {!loading && !loadError && tab === 'rollcall' && (
        <div className="space-y-4">
          <Card className="grid grid-cols-1 gap-4 sm:grid-cols-3 sm:items-end">
            <SelectInput
              label={t('students.section')}
              value={selectedSectionId}
              onChange={(e) => {
                setSelectedSectionId(e.target.value);
                setRegisterLoading(true);
                setRegisterError(null);
              }}
              options={[
                { value: '', label: t('attendance.selectSection') },
                ...sections.map((s) => ({ value: s.id, label: `${s.name} (${s.code})` })),
              ]}
            />
            <FormInput
              label={t('attendance.date')}
              type="date"
              value={selectedDate}
              onChange={(e) => {
                setSelectedDate(e.target.value);
                setRegisterLoading(true);
                setRegisterError(null);
              }}
            />
            <div className="flex flex-wrap gap-2">
              {canMark && register && register.students.length > 0 && (
                <Button type="button" variant="outline" size="sm" onClick={handleMarkAllPresent}>
                  {t('attendance.markAllPresent')}
                </Button>
              )}
              <Button type="button" variant="outline" size="sm" onClick={refreshRegister}>
                {t('students.retry')}
              </Button>
            </div>
          </Card>

          {registerLoading && (
            <Card>
              <p className="text-sm text-muted">{t('loading.label')}</p>
            </Card>
          )}

          {!registerLoading && registerError && (
            <Card className="flex flex-wrap items-center justify-between gap-3 border-error/40 bg-light-error/30">
              <p role="alert" className="text-sm font-medium text-error">
                {registerError}
              </p>
              <Button type="button" variant="outline" size="sm" onClick={refreshRegister}>
                {t('students.retry')}
              </Button>
            </Card>
          )}

          {!registerLoading && !registerError && register && (
            <Card>
              <form onSubmit={handleSaveAttendance} className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3">
                  <div>
                    <h3 className="text-base font-bold text-ink">{register.sectionName}</h3>
                    <p className="text-xs text-muted">
                      {register.attendanceDate} · {formatNumber(register.students.length)} {t('attendance.studentsCount')}
                    </p>
                  </div>
                  <StatusBadge
                    label={register.isMarked ? t('attendance.marked') : t('attendance.unmarked')}
                    tone={register.isMarked ? 'success' : 'warning'}
                  />
                </div>

                {register.students.length === 0 ? (
                  <p className="text-sm text-muted">{t('attendance.emptyRoster')}</p>
                ) : (
                  <div className="space-y-3">
                    {register.students.map((student) => {
                      const draft = draftMarks[student.studentId] ?? {
                        status: 'Present' as AttendanceStatusValue,
                        remarks: '',
                        correctionReason: '',
                      };
                      const statusChanged = student.status != null && student.status !== draft.status;
                      return (
                        <div
                          key={student.studentId}
                          className="flex flex-col gap-3 rounded-2xl border border-border p-4 md:flex-row md:items-center md:justify-between"
                        >
                          <div>
                            <p className="text-sm font-bold text-ink">
                              {student.firstName} {student.lastName}
                            </p>
                            <p className="text-xs text-muted">
                              {student.admissionNumber}
                              {student.rollNumber ? ` · #${student.rollNumber}` : ''}
                            </p>
                          </div>

                          <div className="flex flex-wrap items-center gap-2">
                            {ATTENDANCE_STATUSES.map((st) => (
                              <button
                                key={st}
                                type="button"
                                disabled={!canMark || register.isLocked}
                                onClick={() =>
                                  setDraftMarks((prev) => ({
                                    ...prev,
                                    [student.studentId]: { ...draft, status: st },
                                  }))
                                }
                                className={`rounded-full px-3 py-1 text-xs font-semibold transition-colors ${
                                  draft.status === st
                                    ? 'bg-primary text-white'
                                    : 'border border-border bg-surface text-muted hover:text-ink'
                                }`}
                              >
                                {t(`attendance.status.${st}`)}
                              </button>
                            ))}
                          </div>

                          <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 md:w-72">
                            <FormInput
                              label={t('attendance.remarks')}
                              value={draft.remarks}
                              onChange={(e) =>
                                setDraftMarks((prev) => ({
                                  ...prev,
                                  [student.studentId]: { ...draft, remarks: e.target.value },
                                }))
                              }
                              disabled={!canMark || register.isLocked}
                            />
                            {statusChanged && (
                              <FormInput
                                label={t('attendance.correctionReason')}
                                value={draft.correctionReason}
                                onChange={(e) =>
                                  setDraftMarks((prev) => ({
                                    ...prev,
                                    [student.studentId]: { ...draft, correctionReason: e.target.value },
                                  }))
                                }
                                required
                              />
                            )}
                          </div>
                        </div>
                      );
                    })}

                    <FormInput
                      label={t('students.notes')}
                      value={registerNotes}
                      onChange={(e) => setRegisterNotes(e.target.value)}
                      disabled={!canMark || register.isLocked}
                    />

                    {canMark && !register.isLocked && (
                      <Button type="submit" disabled={savingRegister}>
                        {savingRegister ? t('loading.label') : t('attendance.saveRegister')}
                      </Button>
                    )}
                  </div>
                )}
              </form>
            </Card>
          )}
        </div>
      )}

      {!loading && !loadError && tab === 'staff' && (
        <div className="space-y-4">
          {canCreateStaff && (
            <Card>
              <form onSubmit={handleCreateStaff} className="grid grid-cols-1 gap-3 sm:grid-cols-3 lg:grid-cols-6 sm:items-end">
                <SelectInput
                  label={t('students.campus')}
                  value={staffCampusId}
                  onChange={(e) => setStaffCampusId(e.target.value)}
                  options={[{ value: '', label: t('students.selectCampus') }, ...campuses.map((c) => ({ value: c.id, label: c.name }))]}
                  required
                />
                <FormInput label={t('attendance.employeeCode')} value={empCode} onChange={(e) => setEmpCode(e.target.value)} required />
                <FormInput label={t('students.firstName')} value={staffFirst} onChange={(e) => setStaffFirst(e.target.value)} required />
                <FormInput label={t('students.lastName')} value={staffLast} onChange={(e) => setStaffLast(e.target.value)} required />
                <SelectInput
                  label={t('attendance.roleType')}
                  value={staffRole}
                  onChange={(e) => setStaffRole(e.target.value)}
                  options={STAFF_ROLES.map((r) => ({ value: r, label: r }))}
                />
                <Button type="submit">{t('attendance.addStaff')}</Button>
              </form>
            </Card>
          )}

          {canAssignStaff && staffMembers.length > 0 && (
            <Card>
              <form onSubmit={handleAssignTeacher} className="grid grid-cols-1 gap-3 sm:grid-cols-5 sm:items-end">
                <SelectInput
                  label={t('attendance.staffMember')}
                  value={assignStaffId}
                  onChange={(e) => setAssignStaffId(e.target.value)}
                  options={staffMembers.map((s) => ({ value: s.id, label: `${s.firstName} ${s.lastName} (${s.employeeCode})` }))}
                />
                <SelectInput
                  label={t('students.academicYear')}
                  value={assignYearId}
                  onChange={(e) => setAssignYearId(e.target.value)}
                  options={academicYears.map((y) => ({ value: y.id, label: y.name }))}
                />
                <SelectInput
                  label={t('students.gradeLevel')}
                  value={assignGradeId}
                  onChange={(e) => setAssignGradeId(e.target.value)}
                  options={gradeLevels.map((g) => ({ value: g.id, label: g.name }))}
                />
                <SelectInput
                  label={t('students.section')}
                  value={assignSectionId}
                  onChange={(e) => setAssignSectionId(e.target.value)}
                  options={sections.map((s) => ({ value: s.id, label: `${s.name} (${s.code})` }))}
                />
                <div className="flex items-center gap-2">
                  <label className="flex items-center gap-1 text-xs">
                    <input type="checkbox" checked={assignHomeroom} onChange={(e) => setAssignHomeroom(e.target.checked)} />
                    {t('attendance.homeroom')}
                  </label>
                  <Button type="submit" size="sm">{t('attendance.assignSection')}</Button>
                </div>
              </form>
            </Card>
          )}

          <Card className="space-y-2">
            {staffMembers.length === 0 ? (
              <p className="text-sm text-muted">{t('attendance.noStaff')}</p>
            ) : (
              staffMembers.map((s) => (
                <div key={s.id} className="flex flex-wrap items-center justify-between rounded-xl border border-border p-3">
                  <div>
                    <p className="text-sm font-bold text-ink">{s.firstName} {s.lastName}</p>
                    <p className="text-xs text-muted">{s.employeeCode} {s.email ? `· ${s.email}` : ''}</p>
                  </div>
                  <StatusBadge label={s.roleType} tone="primary" />
                </div>
              ))
            )}
          </Card>

          {assignments.length > 0 && (
            <Card className="space-y-2">
              <p className="text-xs font-bold uppercase text-muted">{t('attendance.assignmentsTitle')}</p>
              {assignments.map((a) => (
                <div key={a.id} className="flex flex-wrap items-center justify-between rounded-xl border border-border p-3 text-xs">
                  <span className="font-semibold text-ink">
                    {a.staffName} ({a.employeeCode}) → {sectionLabelMap.get(a.sectionId) ?? a.sectionId}
                  </span>
                  {a.isHomeroomTeacher && <StatusBadge label={t('attendance.homeroom')} tone="success" />}
                </div>
              ))}
            </Card>
          )}
        </div>
      )}

      {!loading && !loadError && tab === 'summary' && (
        <div className="space-y-4">
          {summary && (
            <Card className="grid grid-cols-2 gap-4 sm:grid-cols-4">
              <div>
                <p className="text-xs text-muted">{t('attendance.totalMarks')}</p>
                <p className="text-xl font-bold text-ink">{formatNumber(summary.totalMarks)}</p>
              </div>
              <div>
                <p className="text-xs text-muted">{t('attendance.status.Present')}</p>
                <p className="text-xl font-bold text-success">{formatNumber(summary.presentCount)}</p>
              </div>
              <div>
                <p className="text-xs text-muted">{t('attendance.status.Absent')}</p>
                <p className="text-xl font-bold text-error">{formatNumber(summary.absentCount)}</p>
              </div>
              <div>
                <p className="text-xs text-muted">{t('attendance.percentage')}</p>
                <p className="text-xl font-bold text-primary">{formatNumber(summary.attendancePercentage)}%</p>
              </div>
            </Card>
          )}

          <Card className="space-y-2">
            <p className="text-xs font-bold uppercase text-muted">{t('attendance.correctionsTitle')}</p>
            {corrections.length === 0 ? (
              <p className="text-sm text-muted">{t('attendance.noCorrections')}</p>
            ) : (
              corrections.map((c) => (
                <div key={c.id} className="rounded-xl border border-border p-3 text-xs">
                  <span className="font-bold text-ink">
                    {c.attendanceDate}: {c.previousStatus} → {c.newStatus}
                  </span>
                  <p className="mt-1 text-muted">{c.reason}</p>
                </div>
              ))
            )}
          </Card>
        </div>
      )}
    </div>
  );
}
