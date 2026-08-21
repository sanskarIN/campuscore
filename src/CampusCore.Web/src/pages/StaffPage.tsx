import { useState, type FormEvent } from 'react';
import { apiJson, apiRequest } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncState';
import { useApiResource } from '../hooks/useApiResource';
import type { StaffMember } from '../types';

interface StaffForm {
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  jobTitle: string;
}

const initialForm: StaffForm = { employeeNumber: '', firstName: '', lastName: '', email: '', phone: '', jobTitle: '' };

export function StaffPage() {
  const { hasAnyRole } = useAuth();
  const resource = useApiResource(() => apiRequest<StaffMember[]>('/api/operations/staff'));
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<StaffForm>(initialForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await apiJson<unknown>('/api/operations/staff', 'POST', { ...form, phone: form.phone || null });
      setForm(initialForm);
      setShowForm(false);
      resource.reload();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Staff member could not be created.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="page-stack" aria-labelledby="staff-title">
      <div className="page-heading">
        <div>
          <p className="eyebrow">People and responsibilities</p>
          <h1 id="staff-title">Staff directory</h1>
          <p className="muted">Review staff contact details and job responsibilities.</p>
        </div>
        {hasAnyRole('Administrator') ? <button className="button button-primary" type="button" onClick={() => setShowForm((value) => !value)}>{showForm ? 'Close form' : 'Add staff'}</button> : null}
      </div>

      {showForm ? (
        <form className="panel form-grid" onSubmit={submit}>
          <h2 className="full-span">New staff member</h2>
          <label><span>Employee number</span><input required value={form.employeeNumber} onChange={(event) => setForm({ ...form, employeeNumber: event.target.value })} /></label>
          <label><span>Job title</span><input required value={form.jobTitle} onChange={(event) => setForm({ ...form, jobTitle: event.target.value })} /></label>
          <label><span>First name</span><input required value={form.firstName} onChange={(event) => setForm({ ...form, firstName: event.target.value })} /></label>
          <label><span>Last name</span><input value={form.lastName} onChange={(event) => setForm({ ...form, lastName: event.target.value })} /></label>
          <label><span>Email</span><input required type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label>
          <label><span>Phone</span><input value={form.phone} onChange={(event) => setForm({ ...form, phone: event.target.value })} /></label>
          {error ? <div className="form-error full-span" role="alert">{error}</div> : null}
          <div className="full-span form-actions"><button className="button button-primary" type="submit" disabled={saving}>{saving ? 'Saving…' : 'Create staff member'}</button></div>
        </form>
      ) : null}

      {resource.loading ? <LoadingState label="Loading staff…" /> : null}
      {resource.error ? <ErrorState message={resource.error} onRetry={resource.reload} /> : null}
      {resource.data?.length === 0 ? <EmptyState title="No staff records" message="Administrators can add the first staff member." /> : null}
      {resource.data && resource.data.length > 0 ? (
        <div className="card-grid">
          {resource.data.map((staff) => (
            <article className="panel person-card" key={staff.id}>
              <div className="avatar" aria-hidden="true">{staff.firstName.slice(0, 1)}{staff.lastName.slice(0, 1)}</div>
              <div>
                <h2>{staff.firstName} {staff.lastName}</h2>
                <p className="muted">{staff.jobTitle} · {staff.employeeNumber}</p>
                <a href={`mailto:${staff.email}`}>{staff.email}</a>
                {staff.phone ? <p>{staff.phone}</p> : null}
              </div>
            </article>
          ))}
        </div>
      ) : null}
    </section>
  );
}
