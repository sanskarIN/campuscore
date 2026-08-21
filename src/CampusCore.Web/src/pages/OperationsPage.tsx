import { useMemo, useState, type FormEvent } from 'react';
import { apiJson, apiRequest } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorState, LoadingState } from '../components/AsyncState';
import { StudentPicker } from '../components/StudentPicker';
import { useApiResource } from '../hooks/useApiResource';
import type { StaffMember } from '../types';

interface Section { id: string; schoolClassId: string; name: string; capacity: number }
interface SchoolClass { id: string; name: string; sortOrder: number; sections: Section[] }
interface AcademicYear { id: string; name: string; startsOn: string; endsOn: string; isActive: boolean }
interface Subject { id: string; code: string; name: string; maximumMarks: number }
interface TimetableEntry {
  id: string;
  sectionId: string;
  subjectId: string;
  staffMemberId: string | null;
  dayOfWeek: number;
  startsAt: string;
  endsAt: string;
  room: string | null;
  subject: Subject | null;
  staffMember: StaffMember | null;
}
interface ReportAssessment { name: string; score: number; maximumScore: number; percentage: number }
interface ReportSubject { subjectId: string; subjectCode: string; subjectName: string; earned: number; maximum: number; percentage: number; grade: string | null; assessments: ReportAssessment[] }
interface ReportCard { studentId: string; admissionNumber: string; studentName: string; academicYearId: string; academicYear: string; className: string | null; sectionName: string | null; rollNumber: string | null; overallPercentage: number; overallGrade: string | null; subjects: ReportSubject[]; generatedAtUtc: string }

const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

export function OperationsPage() {
  const { hasAnyRole } = useAuth();
  const classes = useApiResource(() => apiRequest<SchoolClass[]>('/api/catalog/classes'));
  const years = useApiResource(() => apiRequest<AcademicYear[]>('/api/catalog/academic-years'));
  const subjects = useApiResource(() => apiRequest<Subject[]>('/api/catalog/subjects'));
  const staff = useApiResource(() => apiRequest<StaffMember[]>('/api/operations/staff'));
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [timetableSection, setTimetableSection] = useState('');
  const [timetable, setTimetable] = useState<TimetableEntry[]>([]);
  const [report, setReport] = useState<ReportCard | null>(null);

  const sections = useMemo(() => classes.data?.flatMap((schoolClass) => schoolClass.sections.map((section) => ({ ...section, className: schoolClass.name }))) ?? [], [classes.data]);

  const perform = async (work: () => Promise<unknown>, success: string) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await work();
      setNotice(success);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Operation failed.');
    } finally {
      setBusy(false);
    }
  };

  const submitEnrollment = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    void perform(async () => {
      await apiJson('/api/operations/enrollments', 'POST', {
        studentId: String(data.get('studentId')),
        academicYearId: String(data.get('academicYearId')),
        sectionId: String(data.get('sectionId')),
        enrolledOn: String(data.get('enrolledOn')),
        rollNumber: String(data.get('rollNumber') || '') || null,
      });
      form.reset();
    }, 'Enrollment created.');
  };

  const submitLeave = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    void perform(async () => {
      await apiJson('/api/operations/leave', 'POST', {
        studentId: String(data.get('studentId')),
        startsOn: String(data.get('startsOn')),
        endsOn: String(data.get('endsOn')),
        reason: String(data.get('reason')),
      });
      form.reset();
    }, 'Leave request created.');
  };

  const submitTimetable = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const sectionId = String(data.get('sectionId'));
    void perform(async () => {
      await apiJson('/api/operations/timetable', 'POST', {
        sectionId,
        subjectId: String(data.get('subjectId')),
        staffMemberId: String(data.get('staffMemberId') || '') || null,
        dayOfWeek: Number(data.get('dayOfWeek')),
        startsAt: `${String(data.get('startsAt'))}:00`,
        endsAt: `${String(data.get('endsAt'))}:00`,
        room: String(data.get('room') || '') || null,
      });
      form.reset();
      setTimetableSection(sectionId);
      setTimetable(await apiRequest<TimetableEntry[]>(`/api/operations/timetable/${sectionId}`));
    }, 'Timetable period created.');
  };

  const loadTimetable = async (sectionId: string) => {
    setTimetableSection(sectionId);
    if (!sectionId) { setTimetable([]); return; }
    setBusy(true);
    setError(null);
    try {
      setTimetable(await apiRequest<TimetableEntry[]>(`/api/operations/timetable/${sectionId}`));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Timetable could not be loaded.');
    } finally {
      setBusy(false);
    }
  };

  const loadReport = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setBusy(true);
    setError(null);
    setReport(null);
    try {
      setReport(await apiRequest<ReportCard>(`/api/reports/report-card/${String(data.get('studentId'))}/${String(data.get('academicYearId'))}`));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Report card could not be generated.');
    } finally {
      setBusy(false);
    }
  };

  const catalogLoading = classes.loading || years.loading || subjects.loading || staff.loading;
  const catalogError = classes.error || years.error || subjects.error || staff.error;

  return (
    <section className="page-stack" aria-labelledby="operations-title">
      <div className="page-heading">
        <div><p className="eyebrow">Academic operations</p><h1 id="operations-title">Enrollment, leave & timetable</h1><p className="muted">Complete the workflows that connect students, sections, staff and academic years.</p></div>
      </div>

      {catalogLoading ? <LoadingState label="Loading operational catalog…" /> : null}
      {catalogError ? <ErrorState message={catalogError} onRetry={() => { classes.reload(); years.reload(); subjects.reload(); staff.reload(); }} /> : null}
      {notice ? <div className="form-success" role="status">{notice}</div> : null}
      {error ? <div className="form-error" role="alert">{error}</div> : null}

      <div className="content-grid two-column">
        {hasAnyRole('Administrator', 'Registrar') ? (
          <form className="panel form-stack" onSubmit={submitEnrollment}>
            <h2>Create enrollment</h2>
            <StudentPicker name="studentId" />
            <label><span>Academic year</span><select name="academicYearId" required defaultValue=""><option value="" disabled>Select year</option>{years.data?.map((year) => <option key={year.id} value={year.id}>{year.name}</option>)}</select></label>
            <label><span>Section</span><select name="sectionId" required defaultValue=""><option value="" disabled>Select section</option>{sections.map((section) => <option key={section.id} value={section.id}>{section.className} · {section.name}</option>)}</select></label>
            <label><span>Enrollment date</span><input name="enrolledOn" type="date" required defaultValue={new Date().toISOString().slice(0, 10)} /></label>
            <label><span>Roll number</span><input name="rollNumber" /></label>
            <button className="button button-primary" disabled={busy} type="submit">Create enrollment</button>
          </form>
        ) : null}

        <form className="panel form-stack" onSubmit={submitLeave}>
          <h2>Submit leave request</h2>
          <StudentPicker name="studentId" />
          <div className="form-grid compact-grid"><label><span>Starts</span><input name="startsOn" type="date" required /></label><label><span>Ends</span><input name="endsOn" type="date" required /></label></div>
          <label><span>Reason</span><textarea name="reason" rows={4} required /></label>
          <button className="button button-primary" disabled={busy} type="submit">Submit leave</button>
        </form>
      </div>

      {hasAnyRole('Administrator', 'Registrar') ? (
        <form className="panel form-grid" onSubmit={submitTimetable}>
          <h2 className="full-span">Add timetable period</h2>
          <label><span>Section</span><select name="sectionId" required defaultValue=""><option value="" disabled>Select section</option>{sections.map((section) => <option key={section.id} value={section.id}>{section.className} · {section.name}</option>)}</select></label>
          <label><span>Subject</span><select name="subjectId" required defaultValue=""><option value="" disabled>Select subject</option>{subjects.data?.map((subject) => <option key={subject.id} value={subject.id}>{subject.code} · {subject.name}</option>)}</select></label>
          <label><span>Staff member</span><select name="staffMemberId" defaultValue=""><option value="">Unassigned</option>{staff.data?.map((member) => <option key={member.id} value={member.id}>{member.firstName} {member.lastName} · {member.jobTitle}</option>)}</select></label>
          <label><span>Day</span><select name="dayOfWeek" defaultValue="1">{dayNames.map((day, index) => <option key={day} value={index}>{day}</option>)}</select></label>
          <label><span>Starts</span><input name="startsAt" type="time" required /></label>
          <label><span>Ends</span><input name="endsAt" type="time" required /></label>
          <label><span>Room</span><input name="room" /></label>
          <div className="full-span form-actions"><button className="button button-primary" disabled={busy} type="submit">Add period</button></div>
        </form>
      ) : null}

      <section className="panel page-stack" aria-labelledby="timetable-title">
        <div className="page-heading compact-heading"><div><h2 id="timetable-title">Timetable viewer</h2><p className="muted">Select a section to review its ordered weekly schedule.</p></div><label className="compact-field"><span>Section</span><select value={timetableSection} onChange={(event) => void loadTimetable(event.target.value)}><option value="">Select section</option>{sections.map((section) => <option key={section.id} value={section.id}>{section.className} · {section.name}</option>)}</select></label></div>
        {timetable.length ? <div className="table-wrap"><table><thead><tr><th>Day</th><th>Time</th><th>Subject</th><th>Teacher</th><th>Room</th></tr></thead><tbody>{timetable.map((entry) => <tr key={entry.id}><td>{dayNames[entry.dayOfWeek] ?? entry.dayOfWeek}</td><td>{entry.startsAt.slice(0, 5)}–{entry.endsAt.slice(0, 5)}</td><td>{entry.subject ? `${entry.subject.code} · ${entry.subject.name}` : entry.subjectId}</td><td>{entry.staffMember ? `${entry.staffMember.firstName} ${entry.staffMember.lastName}` : 'Unassigned'}</td><td>{entry.room ?? '—'}</td></tr>)}</tbody></table></div> : timetableSection ? <p className="muted">No periods configured for this section.</p> : null}
      </section>

      <section className="panel page-stack print-section" aria-labelledby="report-title">
        <div><h2 id="report-title">Report card</h2><p className="muted">Generate the current aggregate report from recorded marks and grading rules.</p></div>
        <form className="form-grid compact-grid report-controls" onSubmit={(event) => void loadReport(event)}>
          <StudentPicker name="studentId" />
          <label><span>Academic year</span><select name="academicYearId" required defaultValue=""><option value="" disabled>Select year</option>{years.data?.map((year) => <option key={year.id} value={year.id}>{year.name}</option>)}</select></label>
          <div className="form-actions"><button className="button button-secondary" disabled={busy} type="submit">Generate report</button></div>
        </form>
        {report ? (
          <article className="report-card">
            <div className="page-heading compact-heading"><div><p className="eyebrow">{report.academicYear}</p><h3>{report.studentName}</h3><p>{report.admissionNumber} · {report.className ?? 'No class'} / {report.sectionName ?? 'No section'}{report.rollNumber ? ` · Roll ${report.rollNumber}` : ''}</p></div><div className="grade-result"><strong>{report.overallGrade ?? '—'}</strong><span>{report.overallPercentage}%</span><small>Overall</small></div></div>
            <div className="table-wrap"><table><thead><tr><th>Subject</th><th>Earned</th><th>Maximum</th><th>Percentage</th><th>Grade</th></tr></thead><tbody>{report.subjects.map((subject) => <tr key={subject.subjectId}><td><strong>{subject.subjectCode}</strong> · {subject.subjectName}</td><td>{subject.earned}</td><td>{subject.maximum}</td><td>{subject.percentage}%</td><td>{subject.grade ?? '—'}</td></tr>)}</tbody></table></div>
            <div className="form-actions no-print"><button className="button button-secondary" type="button" onClick={() => window.print()}>Print report</button></div>
          </article>
        ) : null}
      </section>
    </section>
  );
}
