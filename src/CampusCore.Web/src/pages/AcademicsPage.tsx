import { useState, type FormEvent } from 'react';
import { apiJson, apiRequest } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncState';
import { useApiResource } from '../hooks/useApiResource';

interface AcademicYear { id: string; name: string; startsOn: string; endsOn: string; isActive: boolean }
interface Subject { id: string; code: string; name: string; maximumMarks: number }
interface GradeScale { id: string; name: string; minimumPercentage: number; maximumPercentage: number; grade: string; description: string | null }
interface GradeResult { grade: string; description: string | null; percentage: number }

const attendanceStatuses = [
  { value: 1, label: 'Present' },
  { value: 2, label: 'Absent' },
  { value: 3, label: 'Late' },
  { value: 4, label: 'Excused' },
];

export function AcademicsPage() {
  const { hasAnyRole } = useAuth();
  const years = useApiResource(() => apiRequest<AcademicYear[]>('/api/catalog/academic-years'));
  const subjects = useApiResource(() => apiRequest<Subject[]>('/api/catalog/subjects'));
  const scales = useApiResource(() => apiRequest<GradeScale[]>('/api/catalog/grade-scales'));
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [gradePercentage, setGradePercentage] = useState('');
  const [gradeResult, setGradeResult] = useState<GradeResult | null>(null);

  const runMutation = async (work: () => Promise<unknown>, success: string) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await work();
      setNotice(success);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'The academic operation failed.');
    } finally {
      setBusy(false);
    }
  };

  const submitAttendance = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    void runMutation(
      () => apiJson('/api/academics/attendance', 'PUT', {
        studentId: String(data.get('studentId')),
        date: String(data.get('date')),
        status: Number(data.get('status')),
        note: String(data.get('note') || '') || null,
      }),
      'Attendance saved.',
    );
  };

  const submitMark = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    void runMutation(
      () => apiJson('/api/academics/marks', 'POST', {
        studentId: String(data.get('studentId')),
        subjectId: String(data.get('subjectId')),
        academicYearId: String(data.get('academicYearId')),
        assessmentName: String(data.get('assessmentName')),
        score: Number(data.get('score')),
        maximumScore: Number(data.get('maximumScore')),
      }),
      'Mark recorded.',
    );
  };

  const resolveGrade = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setGradeResult(null);
    try {
      setGradeResult(await apiRequest<GradeResult>(`/api/academics/grades/resolve?percentage=${encodeURIComponent(gradePercentage)}`));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Grade could not be resolved.');
    } finally {
      setBusy(false);
    }
  };

  const loading = years.loading || subjects.loading || scales.loading;
  const catalogError = years.error || subjects.error || scales.error;

  return (
    <section className="page-stack" aria-labelledby="academics-title">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Attendance, assessment and grading</p>
          <h1 id="academics-title">Academics</h1>
          <p className="muted">Record classroom activity against the configured academic catalog.</p>
        </div>
      </div>

      {loading ? <LoadingState label="Loading academic catalog…" /> : null}
      {catalogError ? <ErrorState message={catalogError} onRetry={() => { years.reload(); subjects.reload(); scales.reload(); }} /> : null}
      {notice ? <div className="form-success" role="status">{notice}</div> : null}
      {error ? <div className="form-error" role="alert">{error}</div> : null}

      <div className="content-grid two-column">
        {hasAnyRole('Administrator', 'Registrar', 'Teacher') ? (
          <form className="panel form-stack" onSubmit={submitAttendance}>
            <h2>Record attendance</h2>
            <label><span>Student ID</span><input name="studentId" required placeholder="Student UUID" /></label>
            <label><span>Date</span><input name="date" type="date" required max={new Date(Date.now() + 86_400_000).toISOString().slice(0, 10)} /></label>
            <label><span>Status</span><select name="status" defaultValue="1">{attendanceStatuses.map((status) => <option key={status.value} value={status.value}>{status.label}</option>)}</select></label>
            <label><span>Note</span><textarea name="note" rows={3} placeholder="Optional context" /></label>
            <button className="button button-primary" disabled={busy} type="submit">Save attendance</button>
          </form>
        ) : null}

        {hasAnyRole('Administrator', 'Teacher') ? (
          <form className="panel form-stack" onSubmit={submitMark}>
            <h2>Record mark</h2>
            <label><span>Student ID</span><input name="studentId" required placeholder="Student UUID" /></label>
            <label><span>Academic year</span><select name="academicYearId" required defaultValue=""><option value="" disabled>Select year</option>{years.data?.map((year) => <option key={year.id} value={year.id}>{year.name}</option>)}</select></label>
            <label><span>Subject</span><select name="subjectId" required defaultValue=""><option value="" disabled>Select subject</option>{subjects.data?.map((subject) => <option key={subject.id} value={subject.id}>{subject.code} · {subject.name}</option>)}</select></label>
            <label><span>Assessment</span><input name="assessmentName" required placeholder="Midterm, Quiz 1…" /></label>
            <div className="form-grid compact-grid"><label><span>Score</span><input name="score" type="number" min="0" step="0.01" required /></label><label><span>Maximum</span><input name="maximumScore" type="number" min="0.01" step="0.01" required /></label></div>
            <button className="button button-primary" disabled={busy} type="submit">Record mark</button>
          </form>
        ) : null}
      </div>

      <form className="panel grade-resolver" onSubmit={(event) => void resolveGrade(event)}>
        <div><h2>Grade resolver</h2><p className="muted">Check the currently configured grade scale without changing data.</p></div>
        <label><span>Percentage</span><input value={gradePercentage} onChange={(event) => setGradePercentage(event.target.value)} type="number" min="0" max="100" step="0.01" required /></label>
        <button className="button button-secondary" disabled={busy} type="submit">Resolve</button>
        {gradeResult ? <div className="grade-result" role="status"><strong>{gradeResult.grade}</strong><span>{gradeResult.percentage}%</span><small>{gradeResult.description ?? 'Configured grade band'}</small></div> : null}
      </form>

      <div className="content-grid two-column">
        <article className="panel">
          <h2>Subjects</h2>
          {subjects.data?.length ? <ul className="plain-list">{subjects.data.map((subject) => <li key={subject.id}><strong>{subject.code}</strong> · {subject.name}<span className="muted">Maximum {subject.maximumMarks}</span></li>)}</ul> : <EmptyState title="No subjects" message="An administrator needs to configure subjects." />}
        </article>
        <article className="panel">
          <h2>Grade scales</h2>
          {scales.data?.length ? <ul className="plain-list">{scales.data.map((scale) => <li key={scale.id}><strong>{scale.grade}</strong> · {scale.minimumPercentage}%–{scale.maximumPercentage}%<span className="muted">{scale.name}</span></li>)}</ul> : <EmptyState title="No grade scales" message="An administrator needs to configure grading rules." />}
        </article>
      </div>
    </section>
  );
}
