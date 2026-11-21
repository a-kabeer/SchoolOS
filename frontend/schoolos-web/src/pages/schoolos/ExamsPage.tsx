import React, { useCallback, useEffect, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, FormInput, SegmentedTabs, SelectInput, StatusBadge } from '@/components/ui';
import {
  schoolOsApi,
  type AcademicYearDto,
  type ExamPaperDto,
  type ExamTermDto,
  type GradeLevelDto,
  type MarkSheetSummaryDto,
  type SectionDto,
  type SectionMarkSheetViewDto,
  type StudentReportCardDto,
  type StudentSummaryDto,
  type SubjectDto,
} from '@/api/client';
import { useAuth } from '@/context/AuthContext';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

type ExamsTab = 'marks' | 'approvals' | 'setup' | 'reportcard';

interface SchoolOption {
  id: string;
  name: string;
}

interface CampusOption {
  id: string;
  name: string;
}

interface DraftScore {
  marks: string;
  isAbsent: boolean;
  remarks: string;
}

export function ExamsPage() {
  const { t, formatNumber } = useLocale();
  const { showToast } = useToast();
  const { hasPermission } = useAuth();

  const canCreate = hasPermission('exams:create');
  const canEdit = hasPermission('exams:edit');

  const [tab, setTab] = useState<ExamsTab>('marks');
  const [reloadTick, setReloadTick] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [schools, setSchools] = useState<SchoolOption[]>([]);
  const [campuses, setCampuses] = useState<CampusOption[]>([]);
  const [academicYears, setAcademicYears] = useState<AcademicYearDto[]>([]);
  const [gradeLevels, setGradeLevels] = useState<GradeLevelDto[]>([]);
  const [sections, setSections] = useState<SectionDto[]>([]);
  const [students, setStudents] = useState<StudentSummaryDto[]>([]);
  const [subjects, setSubjects] = useState<SubjectDto[]>([]);
  const [terms, setTerms] = useState<ExamTermDto[]>([]);
  const [papers, setPapers] = useState<ExamPaperDto[]>([]);
  const [markSheets, setMarkSheets] = useState<MarkSheetSummaryDto[]>([]);

  // Mark entry state
  const [selectedPaperId, setSelectedPaperId] = useState('');
  const [selectedSectionId, setSelectedSectionId] = useState('');
  const [sheetTick, setSheetTick] = useState(0);
  const [sheetLoading, setSheetLoading] = useState(false);
  const [sheetError, setSheetError] = useState<string | null>(null);
  const [activeSheet, setActiveSheet] = useState<SectionMarkSheetViewDto | null>(null);
  const [draftScores, setDraftScores] = useState<Record<string, DraftScore>>({});
  const [savingMarks, setSavingMarks] = useState(false);

  // Setup forms
  const [subSchoolId, setSubSchoolId] = useState('');
  const [subGradeId, setSubGradeId] = useState('');
  const [subCode, setSubCode] = useState('');
  const [subName, setSubName] = useState('');

  const [termCampusId, setTermCampusId] = useState('');
  const [termYearId, setTermYearId] = useState('');
  const [termCode, setTermCode] = useState('');
  const [termName, setTermName] = useState('');
  const [termStart, setTermStart] = useState('2026-11-10');
  const [termEnd, setTermEnd] = useState('2026-11-25');

  const [paperTermId, setPaperTermId] = useState('');
  const [paperSubjectId, setPaperSubjectId] = useState('');
  const [paperMax, setPaperMax] = useState('100');
  const [paperPass, setPaperPass] = useState('40');

  // Review notes
  const [reviewNotes, setReviewNotes] = useState('');

  // Report Card state
  const [rcTermId, setRcTermId] = useState('');
  const [rcStudentId, setRcStudentId] = useState('');
  const [reportCard, setReportCard] = useState<StudentReportCardDto | null>(null);
  const [rcLoading, setRcLoading] = useState(false);

  const refresh = useCallback(() => {
    setLoading(true);
    setLoadError(null);
    setReloadTick((n) => n + 1);
  }, []);

  const refreshSheet = useCallback(() => {
    setSheetLoading(true);
    setSheetError(null);
    setSheetTick((n) => n + 1);
  }, []);

  useEffect(() => {
    let active = true;
    const run = async () => {
      try {
        const [schoolsData, yearsData, gradesData, sectionsData, studentsData, subjectsData, termsData, papersData, sheetsData] =
          await Promise.all([
            schoolOsApi.listSchools(),
            schoolOsApi.listAcademicYears(),
            schoolOsApi.listGradeLevels(),
            schoolOsApi.listSections(),
            schoolOsApi.listCanonicalStudents().catch(() => []),
            schoolOsApi.listSubjects(),
            schoolOsApi.listExamTerms(),
            schoolOsApi.listExamPapers(),
            schoolOsApi.listMarkSheets(),
          ]);

        if (!active) return;
        setSchools(schoolsData.map((s) => ({ id: s.id, name: s.name })));
        setSubSchoolId((prev) => prev || schoolsData[0]?.id || '');

        const flatCampuses = schoolsData.flatMap((s) =>
          s.campuses.map((c) => ({ id: c.id, name: `${s.name} — ${c.name}` }))
        );
        setCampuses(flatCampuses);
        setTermCampusId((prev) => prev || flatCampuses[0]?.id || '');

        setAcademicYears(yearsData);
        const defaultYear = yearsData.find((y) => y.isCurrent)?.id || yearsData[0]?.id || '';
        setTermYearId((prev) => prev || defaultYear);

        setGradeLevels(gradesData);
        setSubGradeId((prev) => prev || gradesData[0]?.id || '');

        setSections(sectionsData);
        setSelectedSectionId((prev) => prev || sectionsData[0]?.id || '');

        setStudents(studentsData);
        setRcStudentId((prev) => prev || studentsData[0]?.id || '');

        setSubjects(subjectsData);
        setPaperSubjectId((prev) => prev || subjectsData[0]?.id || '');

        setTerms(termsData);
        setPaperTermId((prev) => prev || termsData[0]?.id || '');
        setRcTermId((prev) => prev || termsData[0]?.id || '');

        setPapers(papersData);
        setSelectedPaperId((prev) => prev || papersData[0]?.id || '');
        setMarkSheets(sheetsData);
      } catch (err) {
        if (!active) return;
        setLoadError(err instanceof Error ? err.message : t('exams.errorLoad'));
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
    if (!selectedPaperId || !selectedSectionId) return;
    let active = true;
    const run = async () => {
      try {
        const view = await schoolOsApi.getSectionMarkSheet(selectedPaperId, selectedSectionId);
        if (!active) return;
        setActiveSheet(view);
        const nextDrafts: Record<string, DraftScore> = {};
        for (const s of view.students) {
          nextDrafts[s.studentId] = {
            marks: s.marksObtained != null ? String(s.marksObtained) : '',
            isAbsent: s.isAbsent,
            remarks: s.remarks ?? '',
          };
        }
        setDraftScores(nextDrafts);
      } catch (err) {
        if (!active) return;
        setSheetError(err instanceof Error ? err.message : t('exams.errorLoad'));
      } finally {
        if (active) setSheetLoading(false);
      }
    };
    void run();
    return () => {
      active = false;
    };
  }, [selectedPaperId, selectedSectionId, sheetTick, t]);

  const handleSaveMarks = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activeSheet || !selectedPaperId || !selectedSectionId) return;
    setSavingMarks(true);
    try {
      const entries = activeSheet.students
        .filter((s) => {
          const d = draftScores[s.studentId];
          return d && (d.isAbsent || d.marks.trim() !== '');
        })
        .map((s) => {
          const d = draftScores[s.studentId];
          return {
            studentId: s.studentId,
            marksObtained: d.isAbsent ? 0 : Number(d.marks),
            isAbsent: d.isAbsent,
            remarks: d.remarks.trim() || undefined,
          };
        });

      await schoolOsApi.saveSectionMarks(selectedPaperId, selectedSectionId, entries);
      showToast(t('exams.marksSaved'), 'success');
      refreshSheet();
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('exams.errorAction'), 'error');
    } finally {
      setSavingMarks(false);
    }
  };

  const handleSubmitSheet = async () => {
    if (!activeSheet?.markSheetId) return;
    try {
      await schoolOsApi.submitMarkSheet(activeSheet.markSheetId);
      showToast(t('exams.submittedSuccess'), 'success');
      refreshSheet();
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('exams.errorAction'), 'error');
    }
  };

  const handleReviewSheet = async (markSheetId: string, approve: boolean) => {
    try {
      await schoolOsApi.reviewMarkSheet(markSheetId, approve, reviewNotes.trim() || (approve ? 'Approved' : 'Correction required'));
      setReviewNotes('');
      showToast(approve ? t('exams.approvedSuccess') : t('exams.returnedSuccess'), 'success');
      refresh();
      if (activeSheet?.markSheetId === markSheetId) refreshSheet();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('exams.errorAction'), 'error');
    }
  };

  const handleCreateSubject = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!subSchoolId || !subGradeId || !subCode.trim() || !subName.trim()) return;
    try {
      await schoolOsApi.createSubject({ schoolId: subSchoolId, gradeLevelId: subGradeId, code: subCode.trim(), name: subName.trim() });
      setSubCode('');
      setSubName('');
      showToast(t('exams.subjectCreated'), 'success');
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('exams.errorAction'), 'error');
    }
  };

  const handleCreateTerm = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!termCampusId || !termYearId || !termCode.trim() || !termName.trim()) return;
    try {
      await schoolOsApi.createExamTerm({
        campusId: termCampusId,
        academicYearId: termYearId,
        code: termCode.trim(),
        name: termName.trim(),
        startDate: termStart,
        endDate: termEnd,
      });
      setTermCode('');
      setTermName('');
      showToast(t('exams.termCreated'), 'success');
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('exams.errorAction'), 'error');
    }
  };

  const handleCreatePaper = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!paperTermId || !subGradeId || !paperSubjectId) return;
    try {
      await schoolOsApi.createExamPaper({
        examTermId: paperTermId,
        gradeLevelId: subGradeId,
        subjectId: paperSubjectId,
        maxMarks: Number(paperMax),
        passingMarks: Number(paperPass),
      });
      showToast(t('exams.paperCreated'), 'success');
      refresh();
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('exams.errorAction'), 'error');
    }
  };

  const handleLoadReportCard = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!rcTermId || !rcStudentId) return;
    setRcLoading(true);
    try {
      const card = await schoolOsApi.getStudentReportCard(rcTermId, rcStudentId);
      setReportCard(card);
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('exams.errorAction'), 'error');
    } finally {
      setRcLoading(false);
    }
  };

  const isSheetEditable = activeSheet?.status === 'Draft' || activeSheet?.status === 'ReturnedForCorrection';

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('exams.title')}
        subtitle={t('exams.subtitle')}
        breadcrumbs={[{ label: 'SchoolOS', path: '/exams' }, { label: t('exams.title') }]}
      />

      <SegmentedTabs<ExamsTab>
        value={tab}
        onChange={setTab}
        options={[
          { value: 'marks', label: t('exams.tabs.marks') },
          { value: 'approvals', label: t('exams.tabs.approvals') },
          { value: 'setup', label: t('exams.tabs.setup') },
          { value: 'reportcard', label: t('exams.tabs.reportcard') },
        ]}
      />

      {loading && (
        <Card>
          <p className="text-sm text-muted">{t('loading.label')}</p>
        </Card>
      )}

      {!loading && loadError && (
        <Card className="flex flex-wrap items-center justify-between gap-3 border-error/40 bg-light-error/30">
          <p role="alert" className="text-sm font-medium text-error">{loadError}</p>
          <Button type="button" variant="outline" size="sm" onClick={refresh}>{t('students.retry')}</Button>
        </Card>
      )}

      {!loading && !loadError && tab === 'marks' && (
        <div className="space-y-4">
          <Card className="grid grid-cols-1 gap-4 sm:grid-cols-3 sm:items-end">
            <SelectInput
              label={t('exams.paper')}
              value={selectedPaperId}
              onChange={(e) => {
                setSelectedPaperId(e.target.value);
                setSheetLoading(true);
                setSheetError(null);
              }}
              options={[
                { value: '', label: t('exams.selectPaper') },
                ...papers.map((p) => ({ value: p.id, label: `${p.examTermName} — ${p.subjectName} (${p.maxMarks})` })),
              ]}
            />
            <SelectInput
              label={t('students.section')}
              value={selectedSectionId}
              onChange={(e) => {
                setSelectedSectionId(e.target.value);
                setSheetLoading(true);
                setSheetError(null);
              }}
              options={[
                { value: '', label: t('attendance.selectSection') },
                ...sections.map((s) => ({ value: s.id, label: `${s.name} (${s.code})` })),
              ]}
            />
            <Button type="button" variant="outline" size="sm" onClick={refreshSheet}>{t('students.retry')}</Button>
          </Card>

          {sheetLoading && <Card><p className="text-sm text-muted">{t('loading.label')}</p></Card>}
          {!sheetLoading && sheetError && (
            <Card className="border-error/40 bg-light-error/30">
              <p role="alert" className="text-sm font-medium text-error">{sheetError}</p>
            </Card>
          )}

          {!sheetLoading && !sheetError && activeSheet && (
            <Card>
              <form onSubmit={handleSaveMarks} className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3">
                  <div>
                    <h3 className="text-base font-bold text-ink">{activeSheet.subjectName} — {activeSheet.sectionName}</h3>
                    <p className="text-xs text-muted">
                      {t('exams.maxMarks')}: {formatNumber(activeSheet.maxMarks)} · {t('exams.passingMarks')}: {formatNumber(activeSheet.passingMarks)}
                    </p>
                  </div>
                  <StatusBadge label={activeSheet.status} tone={activeSheet.status === 'Approved' ? 'success' : 'warning'} />
                </div>

                {activeSheet.reviewNotes && (
                  <p className="rounded-xl bg-subtle px-3 py-2 text-xs text-ink">
                    {t('exams.reviewNotes')}: {activeSheet.reviewNotes}
                  </p>
                )}

                {activeSheet.students.length === 0 ? (
                  <p className="text-sm text-muted">{t('attendance.emptyRoster')}</p>
                ) : (
                  <div className="space-y-2">
                    {activeSheet.students.map((st) => {
                      const d = draftScores[st.studentId] ?? { marks: '', isAbsent: false, remarks: '' };
                      return (
                        <div key={st.studentId} className="grid grid-cols-1 gap-3 rounded-2xl border border-border p-3 md:grid-cols-12 md:items-center">
                          <div className="md:col-span-4">
                            <p className="text-sm font-bold text-ink">{st.firstName} {st.lastName}</p>
                            <p className="text-xs text-muted">{st.admissionNumber}{st.letterGrade ? ` · ${st.letterGrade} (${st.percentage}%)` : ''}</p>
                          </div>
                          <div className="md:col-span-3">
                            <FormInput
                              label={t('exams.marksObtained')}
                              type="number"
                              step="0.5"
                              value={d.marks}
                              disabled={!canCreate || !isSheetEditable || d.isAbsent}
                              onChange={(e) => setDraftScores((prev) => ({ ...prev, [st.studentId]: { ...d, marks: e.target.value } }))}
                            />
                          </div>
                          <div className="md:col-span-2">
                            <label className="flex items-center gap-2 text-xs">
                              <input
                                type="checkbox"
                                checked={d.isAbsent}
                                disabled={!canCreate || !isSheetEditable}
                                onChange={(e) => setDraftScores((prev) => ({ ...prev, [st.studentId]: { ...d, isAbsent: e.target.checked, marks: e.target.checked ? '0' : d.marks } }))}
                              />
                              {t('attendance.status.Absent')}
                            </label>
                          </div>
                          <div className="md:col-span-3">
                            <FormInput
                              label={t('attendance.remarks')}
                              value={d.remarks}
                              disabled={!canCreate || !isSheetEditable}
                              onChange={(e) => setDraftScores((prev) => ({ ...prev, [st.studentId]: { ...d, remarks: e.target.value } }))}
                            />
                          </div>
                        </div>
                      );
                    })}

                    <div className="flex flex-wrap gap-2 pt-2">
                      {canCreate && isSheetEditable && (
                        <Button type="submit" disabled={savingMarks}>
                          {savingMarks ? t('loading.label') : t('exams.saveMarks')}
                        </Button>
                      )}
                      {canEdit && isSheetEditable && activeSheet.markSheetId && (
                        <Button type="button" variant="outline" onClick={() => void handleSubmitSheet()}>
                          {t('exams.submitForApproval')}
                        </Button>
                      )}
                    </div>
                  </div>
                )}
              </form>
            </Card>
          )}
        </div>
      )}

      {!loading && !loadError && tab === 'approvals' && (
        <div className="space-y-4">
          {canEdit && (
            <Card>
              <FormInput label={t('exams.reviewNotes')} value={reviewNotes} onChange={(e) => setReviewNotes(e.target.value)} />
            </Card>
          )}
          <Card className="space-y-2">
            {markSheets.length === 0 ? (
              <p className="text-sm text-muted">{t('exams.noMarkSheets')}</p>
            ) : (
              markSheets.map((m) => (
                <div key={m.id} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border p-3">
                  <div>
                    <p className="text-sm font-bold text-ink">{m.examTermName} — {m.subjectName}</p>
                    <p className="text-xs text-muted">{t('exams.maxMarks')}: {formatNumber(m.maxMarks)}{m.reviewNotes ? ` · ${m.reviewNotes}` : ''}</p>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <StatusBadge label={m.status} tone={m.status === 'Approved' ? 'success' : 'warning'} />
                    {canEdit && m.status === 'Submitted' && (
                      <>
                        <Button type="button" size="sm" onClick={() => void handleReviewSheet(m.id, true)}>{t('exams.approve')}</Button>
                        <Button type="button" size="sm" variant="outline" onClick={() => void handleReviewSheet(m.id, false)}>{t('exams.returnForCorrection')}</Button>
                      </>
                    )}
                  </div>
                </div>
              ))
            )}
          </Card>
        </div>
      )}

      {!loading && !loadError && tab === 'setup' && (
        <div className="space-y-4">
          {canCreate && (
            <>
              <Card>
                <form onSubmit={handleCreateSubject} className="grid grid-cols-1 gap-3 sm:grid-cols-5 sm:items-end">
                  <SelectInput label={t('exams.school')} value={subSchoolId} onChange={(e) => setSubSchoolId(e.target.value)} options={schools.map((s) => ({ value: s.id, label: s.name }))} required />
                  <SelectInput label={t('students.gradeLevel')} value={subGradeId} onChange={(e) => setSubGradeId(e.target.value)} options={gradeLevels.map((g) => ({ value: g.id, label: g.name }))} required />
                  <FormInput label={t('exams.subjectCode')} value={subCode} onChange={(e) => setSubCode(e.target.value)} placeholder="MATH" required />
                  <FormInput label={t('exams.subjectName')} value={subName} onChange={(e) => setSubName(e.target.value)} required />
                  <Button type="submit">{t('exams.addSubject')}</Button>
                </form>
              </Card>

              <Card>
                <form onSubmit={handleCreateTerm} className="grid grid-cols-1 gap-3 sm:grid-cols-3 lg:grid-cols-7 sm:items-end">
                  <SelectInput label={t('students.campus')} value={termCampusId} onChange={(e) => setTermCampusId(e.target.value)} options={campuses.map((c) => ({ value: c.id, label: c.name }))} required />
                  <SelectInput label={t('students.academicYear')} value={termYearId} onChange={(e) => setTermYearId(e.target.value)} options={academicYears.map((y) => ({ value: y.id, label: y.name }))} required />
                  <FormInput label={t('exams.termCode')} value={termCode} onChange={(e) => setTermCode(e.target.value)} placeholder="MID-2026" required />
                  <FormInput label={t('exams.termName')} value={termName} onChange={(e) => setTermName(e.target.value)} required />
                  <FormInput label={t('students.startDate')} type="date" value={termStart} onChange={(e) => setTermStart(e.target.value)} required />
                  <FormInput label={t('exams.endDate')} type="date" value={termEnd} onChange={(e) => setTermEnd(e.target.value)} required />
                  <Button type="submit">{t('exams.addTerm')}</Button>
                </form>
              </Card>

              {terms.length > 0 && subjects.length > 0 && (
                <Card>
                  <form onSubmit={handleCreatePaper} className="grid grid-cols-1 gap-3 sm:grid-cols-6 sm:items-end">
                    <SelectInput label={t('exams.term')} value={paperTermId} onChange={(e) => setPaperTermId(e.target.value)} options={terms.map((tm) => ({ value: tm.id, label: tm.name }))} required />
                    <SelectInput label={t('students.gradeLevel')} value={subGradeId} onChange={(e) => setSubGradeId(e.target.value)} options={gradeLevels.map((g) => ({ value: g.id, label: g.name }))} required />
                    <SelectInput label={t('exams.subject')} value={paperSubjectId} onChange={(e) => setPaperSubjectId(e.target.value)} options={subjects.map((s) => ({ value: s.id, label: `${s.name} (${s.code})` }))} required />
                    <FormInput label={t('exams.maxMarks')} type="number" value={paperMax} onChange={(e) => setPaperMax(e.target.value)} required />
                    <FormInput label={t('exams.passingMarks')} type="number" value={paperPass} onChange={(e) => setPaperPass(e.target.value)} required />
                    <Button type="submit">{t('exams.addPaper')}</Button>
                  </form>
                </Card>
              )}
            </>
          )}
        </div>
      )}

      {!loading && !loadError && tab === 'reportcard' && (
        <div className="space-y-4">
          <Card>
            <form onSubmit={handleLoadReportCard} className="grid grid-cols-1 gap-3 sm:grid-cols-3 sm:items-end">
              <SelectInput label={t('exams.term')} value={rcTermId} onChange={(e) => setRcTermId(e.target.value)} options={terms.map((tm) => ({ value: tm.id, label: tm.name }))} required />
              <SelectInput label={t('students.studentMember')} value={rcStudentId} onChange={(e) => setRcStudentId(e.target.value)} options={students.map((s) => ({ value: s.id, label: `${s.firstName} ${s.lastName} (${s.admissionNumber})` }))} required />
              <Button type="submit" disabled={rcLoading}>{rcLoading ? t('loading.label') : t('exams.viewReportCard')}</Button>
            </form>
          </Card>

          {reportCard && (
            <Card className="space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3">
                <div>
                  <h3 className="text-base font-bold text-ink">{reportCard.examTermName}</h3>
                  <p className="text-xs text-muted">
                    {formatNumber(reportCard.totalObtainedMarks)} / {formatNumber(reportCard.totalMaxMarks)} ({formatNumber(reportCard.overallPercentage)}%) · {t('exams.grade')}: {reportCard.overallGrade}
                  </p>
                </div>
                <StatusBadge label={reportCard.isOverallPassed ? t('exams.passed') : t('exams.failed')} tone={reportCard.isOverallPassed ? 'success' : 'error'} />
              </div>

              {reportCard.subjects.length === 0 ? (
                <p className="text-sm text-muted">{t('exams.noApprovedMarks')}</p>
              ) : (
                <div className="space-y-2">
                  {reportCard.subjects.map((sub) => (
                    <div key={sub.subjectId} className="flex flex-wrap items-center justify-between rounded-xl border border-border p-3 text-xs">
                      <span className="font-bold text-ink">{sub.subjectName} ({sub.subjectCode})</span>
                      <span>
                        {formatNumber(sub.marksObtained)} / {formatNumber(sub.maxMarks)} ({formatNumber(sub.percentage)}%) · <strong>{sub.letterGrade}</strong>
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </Card>
          )}
        </div>
      )}
    </div>
  );
}
