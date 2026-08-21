import { useMemo, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { apiJson, apiRequest } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncState';
import { useApiResource } from '../hooks/useApiResource';
import type { CreateStudentInput, PagedResult, StudentDetails, StudentListItem } from '../types';

const emptyStudent: CreateStudentInput = {
  admissionNumber: '',
  firstName: '',
  lastName: '',
  dateOfBirth: '',
  email: null,
  phone: null,
  addressLine: null,
};

export function StudentsPage() {
  const { hasAnyRole } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const query = searchParams.get('q') ?? '';
  const active = searchParams.get('active') ?? 'true';
  const page = Math.max(1, Number(searchParams.get('page') ?? '1') || 1);
  const [draftQuery, setDraftQuery] = useState(query);
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState<CreateStudentInput>(emptyStudent);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [selected, setSelected] = useState<StudentDetails | null>(null);
  const [detailsBusy, setDetailsBusy] = useState(false);
  const [detailsError, setDetailsError] = useState<string | null>(null);

  const resource = useApiResource(
    () => apiRequest<PagedResult<StudentListItem>>(`/api/students/?q=${encodeURIComponent(query)}&active=${active}&page=${page}&pageSize=25`),
    [query, active, page],
  );

  const totalPages = useMemo(() => Math.max(1, Math.ceil((resource.data?.total ?? 0) / 25)), [resource.data?.total]);

  const applySearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const next = new URLSearchParams(searchParams);
    const normalized = draftQuery.trim();
    if (normalized) next.set('q', normalized);
    else next.delete('q');
    next.set('page', '1');
    setSearchParams(next);
  };

  const setActiveFilter = (value: string) => {
    const next = new URLSearchParams(searchParams);
    if (value === 'all') next.delete('active');
    else next.set('active', value);
    next.set('page', '1');
    setSearchParams(next);
  };

  const setPage = (value: number) => {
    const next = new URLSearchParams(searchParams);
    next.set('page', String(Math.min(totalPages, Math.max(1, value))));
    setSearchParams(next);
  };

  const createStudent = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    setSaveError(null);
    try {
      await apiJson<unknown>('/api/students/', 'POST', form);
      setForm(emptyStudent);
      setCreateOpen(false);
      resource.reload();
    } catch (reason) {
      setSaveError(reason instanceof Error ? reason.message : 'Student could not be created.');
    } finally {
      setSaving(false);
    }
  };

  const openDetails = async (id: string) => {
    setDetailsBusy(true);
    setDetailsError(null);
    try {
      setSelected(await apiRequest<StudentDetails>(`/api/students/${id}`));
    } catch (reason) {
      setDetailsError(reason instanceof Error ? reason.message : 'Student details could not be loaded.');
    } finally {
      setDetailsBusy(false);
    }
  };

  return (
    <section className="page-stack" aria-labelledby="students-title">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Profiles and enrollment context</p>
          <h1 id="students-title">Students</h1>
          <p className="muted">Search active or inactive student profiles and review guardians and enrollment history.</p>
        </div>
        {hasAnyRole('Administrator', 'Registrar') ? (
          <button className="button button-primary" type="button" onClick={() => setCreateOpen((value) => !value)}>
            {createOpen ? 'Close form' : 'Add student'}
          </button>
        ) : null}
      </div>

      {createOpen ? (
        <form className="panel form-grid" onSubmit={createStudent}>
          <h2 className="full-span">New student</h2>
          <label><span>Admission number</span><input required value={form.admissionNumber} onChange={(event) => setForm({ ...form, admissionNumber: event.target.value })} /></label>
          <label><span>Date of birth</span><input required type="date" value={form.dateOfBirth} onChange={(event) => setForm({ ...form, dateOfBirth: event.target.value })} /></label>
          <label><span>First name</span><input required value={form.firstName} onChange={(event) => setForm({ ...form, firstName: event.target.value })} /></label>
          <label><span>Last name</span><input required value={form.lastName} onChange={(event) => setForm({ ...form, lastName: event.target.value })} /></label>
          <label><span>Email</span><input type="email" value={form.email ?? ''} onChange={(event) => setForm({ ...form, email: event.target.value || null })} /></label>
          <label><span>Phone</span><input value={form.phone ?? ''} onChange={(event) => setForm({ ...form, phone: event.target.value || null })} /></label>
          <label className="full-span"><span>Address</span><input value={form.addressLine ?? ''} onChange={(event) => setForm({ ...form, addressLine: event.target.value || null })} /></label>
          {saveError ? <div className="form-error full-span" role="alert">{saveError}</div> : null}
          <div className="full-span form-actions"><button className="button button-primary" disabled={saving} type="submit">{saving ? 'Saving…' : 'Create student'}</button></div>
        </form>
      ) : null}

      <div className="toolbar panel">
        <form className="search-bar" role="search" onSubmit={applySearch}>
          <label className="grow-field"><span className="sr-only">Search students</span><input value={draftQuery} onChange={(event) => setDraftQuery(event.target.value)} placeholder="Name or admission number" /></label>
          <button className="button button-secondary" type="submit">Search</button>
        </form>
        <label className="compact-field"><span>Status</span><select value={active === 'true' || active === 'false' ? active : 'all'} onChange={(event) => setActiveFilter(event.target.value)}><option value="true">Active</option><option value="false">Inactive</option><option value="all">All</option></select></label>
      </div>

      {resource.loading ? <LoadingState label="Loading students…" /> : null}
      {resource.error ? <ErrorState message={resource.error} onRetry={resource.reload} /> : null}
      {resource.data?.items.length === 0 ? <EmptyState title="No students found" message="Adjust the search or status filter, or add the first student." /> : null}
      {resource.data && resource.data.items.length > 0 ? (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Student</th><th>Admission</th><th>Class</th><th>Section</th><th>Roll</th><th>Status</th><th><span className="sr-only">Actions</span></th></tr></thead>
            <tbody>
              {resource.data.items.map((student) => (
                <tr key={student.id}>
                  <td><strong>{student.displayName}</strong><small className="muted table-subtitle">DOB {student.dateOfBirth}</small></td>
                  <td>{student.admissionNumber}</td>
                  <td>{student.className ?? '—'}</td>
                  <td>{student.sectionName ?? '—'}</td>
                  <td>{student.rollNumber ?? '—'}</td>
                  <td><span className={student.isActive ? 'status-pill success' : 'status-pill'}>{student.isActive ? 'Active' : 'Inactive'}</span></td>
                  <td><button className="button button-ghost" type="button" onClick={() => void openDetails(student.id)}>View</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {resource.data && resource.data.total > 25 ? (
        <nav className="pagination" aria-label="Student pages">
          <button className="button button-secondary" type="button" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
          <span>Page {page} of {totalPages} · {resource.data.total} students</span>
          <button className="button button-secondary" type="button" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Next</button>
        </nav>
      ) : null}

      {detailsBusy ? <LoadingState label="Loading student details…" /> : null}
      {detailsError ? <ErrorState message={detailsError} /> : null}
      {selected ? (
        <article className="panel detail-panel">
          <div className="page-heading compact-heading"><div><p className="eyebrow">{selected.admissionNumber}</p><h2>{selected.firstName} {selected.lastName}</h2></div><button className="button button-ghost" type="button" onClick={() => setSelected(null)}>Close</button></div>
          <dl className="definition-grid"><div><dt>Email</dt><dd>{selected.email ?? '—'}</dd></div><div><dt>Phone</dt><dd>{selected.phone ?? '—'}</dd></div><div><dt>Address</dt><dd>{selected.addressLine ?? '—'}</dd></div><div><dt>Status</dt><dd>{selected.isActive ? 'Active' : 'Inactive'}</dd></div></dl>
          <h3>Guardians</h3>
          {selected.guardians.length ? <ul className="plain-list">{selected.guardians.map((guardian) => <li key={guardian.id}><strong>{guardian.name}</strong> · {guardian.relationship}{guardian.isPrimary ? ' · Primary' : ''}</li>)}</ul> : <p className="muted">No guardians recorded.</p>}
          <h3>Enrollment history</h3>
          {selected.enrollments.length ? <ul className="plain-list">{selected.enrollments.map((enrollment) => <li key={enrollment.id}><strong>{enrollment.academicYear}</strong> · {enrollment.className} / {enrollment.sectionName} · {enrollment.status}</li>)}</ul> : <p className="muted">No enrollments recorded.</p>}
        </article>
      ) : null}
    </section>
  );
}
