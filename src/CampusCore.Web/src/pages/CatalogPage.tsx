import { useMemo, useState, type FormEvent } from 'react';
import { apiJson, apiRequest } from '../api/client';
import { ErrorState, LoadingState } from '../components/AsyncState';
import { useApiResource } from '../hooks/useApiResource';

interface Section { id: string; schoolClassId: string; name: string; capacity: number }
interface SchoolClass { id: string; name: string; sortOrder: number; sections: Section[] }
interface AcademicYear { id: string; name: string; startsOn: string; endsOn: string; isActive: boolean }
interface Subject { id: string; code: string; name: string; maximumMarks: number }
interface GradeScale { id: string; name: string; minimumPercentage: number; maximumPercentage: number; grade: string; description: string | null }

type CatalogTab = 'years' | 'classes' | 'subjects' | 'grades';

export function CatalogPage() {
  const years = useApiResource(() => apiRequest<AcademicYear[]>('/api/catalog/academic-years'));
  const classes = useApiResource(() => apiRequest<SchoolClass[]>('/api/catalog/classes'));
  const subjects = useApiResource(() => apiRequest<Subject[]>('/api/catalog/subjects'));
  const grades = useApiResource(() => apiRequest<GradeScale[]>('/api/catalog/grade-scales'));
  const [tab, setTab] = useState<CatalogTab>('years');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const totalSections = useMemo(() => classes.data?.reduce((total, schoolClass) => total + schoolClass.sections.length, 0) ?? 0, [classes.data]);

  const perform = async (action: () => Promise<unknown>, success: string, reload: () => void, form?: HTMLFormElement) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await action();
      form?.reset();
      setNotice(success);
      reload();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Catalog operation failed.');
    } finally {
      setBusy(false);
    }
  };

  const createYear = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    void perform(() => apiJson('/api/catalog/academic-years', 'POST', {
      name: String(data.get('name')),
      startsOn: String(data.get('startsOn')),
      endsOn: String(data.get('endsOn')),
      isActive: data.get('isActive') === 'on',
    }), 'Academic year created.', years.reload, form);
  };

  const createClass = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    void perform(() => apiJson('/api/catalog/classes', 'POST', { name: String(data.get('name')), sortOrder: Number(data.get('sortOrder')) }), 'Class created.', classes.reload, form);
  };

  const createSection = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    void perform(() => apiJson('/api/catalog/sections', 'POST', { schoolClassId: String(data.get('schoolClassId')), name: String(data.get('name')), capacity: Number(data.get('capacity')) }), 'Section created.', classes.reload, form);
  };

  const createSubject = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    void perform(() => apiJson('/api/catalog/subjects', 'POST', { code: String(data.get('code')), name: String(data.get('name')), maximumMarks: Number(data.get('maximumMarks')) }), 'Subject created.', subjects.reload, form);
  };

  const createGrade = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    void perform(() => apiJson('/api/catalog/grade-scales', 'POST', {
      name: String(data.get('name')),
      minimumPercentage: Number(data.get('minimumPercentage')),
      maximumPercentage: Number(data.get('maximumPercentage')),
      grade: String(data.get('grade')),
      description: String(data.get('description') || '') || null,
    }), 'Grade scale created.', grades.reload, form);
  };

  const loading = years.loading || classes.loading || subjects.loading || grades.loading;
  const loadError = years.error || classes.error || subjects.error || grades.error;

  return (
    <section className="page-stack" aria-labelledby="catalog-title">
      <div className="page-heading">
        <div><p className="eyebrow">Administrator academic setup</p><h1 id="catalog-title">Academic catalog</h1><p className="muted">Configure the reference data used by enrollment, assessment, timetable and reporting workflows.</p></div>
      </div>

      <div className="metric-grid compact-metrics">
        <article className="metric-card"><span className="metric-value">{years.data?.length ?? 0}</span><strong>Academic years</strong></article>
        <article className="metric-card"><span className="metric-value">{classes.data?.length ?? 0}</span><strong>Classes</strong></article>
        <article className="metric-card"><span className="metric-value">{totalSections}</span><strong>Sections</strong></article>
        <article className="metric-card"><span className="metric-value">{subjects.data?.length ?? 0}</span><strong>Subjects</strong></article>
      </div>

      <div className="segmented catalog-tabs" role="tablist" aria-label="Catalog areas">
        {([['years', 'Academic years'], ['classes', 'Classes & sections'], ['subjects', 'Subjects'], ['grades', 'Grade scales']] as const).map(([value, label]) => (
          <button key={value} role="tab" aria-selected={tab === value} className={tab === value ? 'active' : ''} type="button" onClick={() => setTab(value)}>{label}</button>
        ))}
      </div>

      {loading ? <LoadingState label="Loading catalog…" /> : null}
      {loadError ? <ErrorState message={loadError} onRetry={() => { years.reload(); classes.reload(); subjects.reload(); grades.reload(); }} /> : null}
      {notice ? <div className="form-success" role="status">{notice}</div> : null}
      {error ? <div className="form-error" role="alert">{error}</div> : null}

      {tab === 'years' ? (
        <div className="content-grid two-column">
          <form className="panel form-stack" onSubmit={createYear}><h2>Add academic year</h2><label><span>Name</span><input name="name" required placeholder="2026–27" /></label><div className="form-grid compact-grid"><label><span>Starts</span><input name="startsOn" type="date" required /></label><label><span>Ends</span><input name="endsOn" type="date" required /></label></div><label className="checkbox-field"><input name="isActive" type="checkbox" /><span>Make active</span></label><button className="button button-primary" type="submit" disabled={busy}>Create year</button></form>
          <article className="panel"><h2>Configured years</h2><ul className="plain-list">{years.data?.map((year) => <li key={year.id}><strong>{year.name}</strong><span>{year.startsOn} → {year.endsOn}</span><span className={year.isActive ? 'status-pill success' : 'status-pill'}>{year.isActive ? 'Active' : 'Inactive'}</span></li>)}</ul></article>
        </div>
      ) : null}

      {tab === 'classes' ? (
        <div className="content-grid two-column">
          <div className="page-stack"><form className="panel form-stack" onSubmit={createClass}><h2>Add class</h2><label><span>Name</span><input name="name" required /></label><label><span>Sort order</span><input name="sortOrder" type="number" min="0" defaultValue="0" required /></label><button className="button button-primary" type="submit" disabled={busy}>Create class</button></form><form className="panel form-stack" onSubmit={createSection}><h2>Add section</h2><label><span>Class</span><select name="schoolClassId" required defaultValue=""><option value="" disabled>Select class</option>{classes.data?.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label><span>Section name</span><input name="name" required placeholder="A" /></label><label><span>Capacity</span><input name="capacity" type="number" min="1" max="500" defaultValue="40" required /></label><button className="button button-primary" type="submit" disabled={busy}>Create section</button></form></div>
          <article className="panel"><h2>Classes & sections</h2><ul className="plain-list">{classes.data?.map((item) => <li key={item.id}><strong>{item.name}</strong>{item.sections.length ? <span>{item.sections.map((section) => `${section.name} (${section.capacity})`).join(' · ')}</span> : <span className="muted">No sections yet</span>}</li>)}</ul></article>
        </div>
      ) : null}

      {tab === 'subjects' ? (
        <div className="content-grid two-column"><form className="panel form-stack" onSubmit={createSubject}><h2>Add subject</h2><label><span>Code</span><input name="code" required placeholder="MATH" /></label><label><span>Name</span><input name="name" required /></label><label><span>Default maximum marks</span><input name="maximumMarks" type="number" min="0.01" step="0.01" defaultValue="100" required /></label><button className="button button-primary" type="submit" disabled={busy}>Create subject</button></form><article className="panel"><h2>Configured subjects</h2><ul className="plain-list">{subjects.data?.map((subject) => <li key={subject.id}><strong>{subject.code}</strong><span>{subject.name}</span><span className="muted">Maximum {subject.maximumMarks}</span></li>)}</ul></article></div>
      ) : null}

      {tab === 'grades' ? (
        <div className="content-grid two-column"><form className="panel form-stack" onSubmit={createGrade}><h2>Add grade band</h2><label><span>Scale name</span><input name="name" required placeholder="Standard" /></label><div className="form-grid compact-grid"><label><span>Minimum %</span><input name="minimumPercentage" type="number" min="0" max="100" step="0.01" required /></label><label><span>Maximum %</span><input name="maximumPercentage" type="number" min="0" max="100" step="0.01" required /></label></div><label><span>Grade</span><input name="grade" required placeholder="A" /></label><label><span>Description</span><input name="description" /></label><button className="button button-primary" type="submit" disabled={busy}>Create grade band</button></form><article className="panel"><h2>Configured grade bands</h2><ul className="plain-list">{grades.data?.map((grade) => <li key={grade.id}><strong>{grade.grade}</strong><span>{grade.minimumPercentage}%–{grade.maximumPercentage}%</span><span className="muted">{grade.name}{grade.description ? ` · ${grade.description}` : ''}</span></li>)}</ul></article></div>
      ) : null}
    </section>
  );
}
