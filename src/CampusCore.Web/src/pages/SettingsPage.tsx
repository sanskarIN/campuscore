import { useState, type FormEvent } from 'react';
import { apiJson, apiRequest } from '../api/client';
import { ErrorState, LoadingState } from '../components/AsyncState';
import { useApiResource } from '../hooks/useApiResource';
import type { AdminUser, AuditLog, InstitutionSettings, PagedResult } from '../types';

const availableRoles = ['Administrator', 'Registrar', 'Teacher'] as const;

export function SettingsPage() {
  const settings = useApiResource(() => apiRequest<InstitutionSettings>('/api/admin/settings'));
  const audit = useApiResource(() => apiRequest<PagedResult<AuditLog>>('/api/admin/audit?page=1&pageSize=50'));
  const users = useApiResource(() => apiRequest<AdminUser[]>('/api/admin/users/'));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [showUserForm, setShowUserForm] = useState(false);

  const perform = async (action: () => Promise<unknown>, success: string, reload?: () => void) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await action();
      setNotice(success);
      reload?.();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Administration operation failed.');
    } finally {
      setBusy(false);
    }
  };

  const saveSettings = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    void perform(
      () => apiJson('/api/admin/settings', 'PUT', {
        institutionName: String(data.get('institutionName')),
        address: String(data.get('address') || '') || null,
        timeZoneId: String(data.get('timeZoneId') || '') || null,
        locale: String(data.get('locale')),
        dateFormat: String(data.get('dateFormat')),
        defaultPageSize: Number(data.get('defaultPageSize')),
        allowGuardianPortal: data.get('allowGuardianPortal') === 'on',
      }),
      'Institution settings saved.',
      settings.reload,
    );
  };

  const createUser = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const roles = availableRoles.filter((role) => data.get(`role-${role}`) === 'on');
    void perform(
      async () => {
        await apiJson('/api/admin/users/', 'POST', {
          email: String(data.get('email')),
          displayName: String(data.get('displayName')),
          password: String(data.get('password')),
          roles,
        });
        form.reset();
        setShowUserForm(false);
      },
      'User account created.',
      users.reload,
    );
  };

  const changeRoles = (user: AdminUser, role: string) => {
    const roles = user.roles.includes(role) ? user.roles.filter((item) => item !== role) : [...user.roles, role];
    void perform(() => apiJson(`/api/admin/users/${user.id}/roles`, 'PUT', { roles }), 'Roles updated.', users.reload);
  };

  const toggleUser = (user: AdminUser) => {
    void perform(() => apiJson(`/api/admin/users/${user.id}/status`, 'PATCH', { isActive: !user.isActive }), user.isActive ? 'User deactivated.' : 'User activated.', users.reload);
  };

  return (
    <section className="page-stack" aria-labelledby="settings-title">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Administrator controls</p>
          <h1 id="settings-title">Settings & audit</h1>
          <p className="muted">Institution defaults, access administration and privacy-safe change history.</p>
        </div>
      </div>

      {notice ? <div className="form-success" role="status">{notice}</div> : null}
      {error ? <div className="form-error" role="alert">{error}</div> : null}
      {settings.loading ? <LoadingState label="Loading institution settings…" /> : null}
      {settings.error ? <ErrorState message={settings.error} onRetry={settings.reload} /> : null}

      {settings.data ? (
        <form className="panel form-grid" key={settings.data.id} onSubmit={saveSettings}>
          <h2 className="full-span">Institution</h2>
          <label><span>Name</span><input name="institutionName" required defaultValue={settings.data.institutionName} /></label>
          <label><span>Time zone ID</span><input name="timeZoneId" defaultValue={settings.data.timeZoneId ?? ''} placeholder="Asia/Kolkata" /></label>
          <label><span>Locale</span><input name="locale" required defaultValue={settings.data.locale} /></label>
          <label><span>Date format</span><input name="dateFormat" required defaultValue={settings.data.dateFormat} /></label>
          <label><span>Default page size</span><input name="defaultPageSize" type="number" min="10" max="100" required defaultValue={settings.data.defaultPageSize} /></label>
          <label className="checkbox-field"><input name="allowGuardianPortal" type="checkbox" defaultChecked={settings.data.allowGuardianPortal} /><span>Allow guardian portal when enabled by deployment</span></label>
          <label className="full-span"><span>Address</span><textarea name="address" rows={3} defaultValue={settings.data.address ?? ''} /></label>
          <div className="full-span form-actions"><button className="button button-primary" type="submit" disabled={busy}>Save settings</button></div>
        </form>
      ) : null}

      <section className="panel page-stack" aria-labelledby="users-title">
        <div className="page-heading compact-heading">
          <div><h2 id="users-title">User accounts</h2><p className="muted">Assign least-privilege roles and deactivate access without deleting audit history.</p></div>
          <button className="button button-secondary" type="button" onClick={() => setShowUserForm((value) => !value)}>{showUserForm ? 'Close form' : 'Create user'}</button>
        </div>

        {showUserForm ? (
          <form className="form-grid nested-form" onSubmit={createUser}>
            <label><span>Display name</span><input name="displayName" required /></label>
            <label><span>Email</span><input name="email" type="email" required /></label>
            <label><span>Initial password</span><input name="password" type="password" minLength={8} autoComplete="new-password" required /></label>
            <fieldset><legend>Roles</legend><div className="role-checks">{availableRoles.map((role) => <label className="checkbox-field" key={role}><input name={`role-${role}`} type="checkbox" /><span>{role}</span></label>)}</div></fieldset>
            <div className="full-span form-actions"><button className="button button-primary" disabled={busy} type="submit">Create account</button></div>
          </form>
        ) : null}

        {users.loading ? <LoadingState label="Loading users…" /> : null}
        {users.error ? <ErrorState message={users.error} onRetry={users.reload} /> : null}
        {users.data ? (
          <div className="table-wrap">
            <table>
              <thead><tr><th>User</th><th>Roles</th><th>Status</th><th>Actions</th></tr></thead>
              <tbody>{users.data.map((user) => (
                <tr key={user.id}>
                  <td><strong>{user.displayName}</strong><small className="muted table-subtitle">{user.email ?? 'No email'}</small></td>
                  <td><div className="role-actions">{availableRoles.map((role) => <button className={user.roles.includes(role) ? 'status-pill success interactive-pill' : 'status-pill interactive-pill'} type="button" key={role} onClick={() => changeRoles(user, role)} aria-pressed={user.roles.includes(role)}>{role}</button>)}</div></td>
                  <td><span className={user.isActive ? 'status-pill success' : 'status-pill warning'}>{user.isActive ? 'Active' : 'Inactive'}</span></td>
                  <td><button className="button button-ghost" disabled={busy} type="button" onClick={() => toggleUser(user)}>{user.isActive ? 'Deactivate' : 'Activate'}</button></td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        ) : null}
      </section>

      <section className="panel page-stack" aria-labelledby="audit-title">
        <div><h2 id="audit-title">Recent audit events</h2><p className="muted">Latest 50 sensitive changes. Metadata is written through the server’s safe audit abstraction.</p></div>
        {audit.loading ? <LoadingState label="Loading audit log…" /> : null}
        {audit.error ? <ErrorState message={audit.error} onRetry={audit.reload} /> : null}
        {audit.data ? (
          <div className="table-wrap">
            <table>
              <thead><tr><th>When</th><th>Action</th><th>Entity</th><th>Actor</th><th>Correlation</th></tr></thead>
              <tbody>{audit.data.items.map((entry) => (
                <tr key={entry.id}>
                  <td><time dateTime={entry.occurredAtUtc}>{new Date(entry.occurredAtUtc).toLocaleString()}</time></td>
                  <td><code>{entry.action}</code></td>
                  <td>{entry.entityType}<small className="muted table-subtitle">{entry.entityId}</small></td>
                  <td>{entry.actorUserId || 'system'}</td>
                  <td>{entry.correlationId ?? '—'}</td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        ) : null}
      </section>
    </section>
  );
}
