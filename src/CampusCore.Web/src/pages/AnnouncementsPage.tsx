import { useState, type FormEvent } from 'react';
import { apiDownload, apiRequest } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncState';
import { useApiResource } from '../hooks/useApiResource';
import type { Announcement } from '../types';

const audiences = [
  { value: 1, label: 'Everyone' },
  { value: 2, label: 'Students' },
  { value: 3, label: 'Guardians' },
  { value: 4, label: 'Staff' },
];

const audienceLabel = (value: string | number): string => audiences.find((item) => item.value === Number(value))?.label ?? String(value);
const toLocalInput = (date: Date): string => {
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
};

export function AnnouncementsPage() {
  const { hasAnyRole } = useAuth();
  const resource = useApiResource(() => apiRequest<Announcement[]>('/api/announcements/'));
  const [showForm, setShowForm] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const publish = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const file = data.get('attachment');
    setBusy(true);
    setError(null);
    setNotice(null);

    try {
      const created = await apiRequest<{ id: string }>('/api/announcements/', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: String(data.get('title')),
          body: String(data.get('body')),
          audience: Number(data.get('audience')),
          publishAtUtc: new Date(String(data.get('publishAt'))).toISOString(),
          expiresAtUtc: data.get('expiresAt') ? new Date(String(data.get('expiresAt'))).toISOString() : null,
        }),
      });

      if (file instanceof File && file.size > 0) {
        const upload = new FormData();
        upload.set('file', file);
        await apiRequest(`/api/announcements/${created.id}/attachments/`, { method: 'POST', body: upload });
      }

      form.reset();
      setShowForm(false);
      setNotice('Announcement published.');
      resource.reload();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Announcement could not be published.');
    } finally {
      setBusy(false);
    }
  };

  const download = async (announcementId: string, attachmentId: string, fileName: string) => {
    try {
      const blob = await apiDownload(`/api/announcements/${announcementId}/attachments/${attachmentId}`);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = fileName;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Attachment download failed.');
    }
  };

  return (
    <section className="page-stack" aria-labelledby="announcements-title">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Campus communication</p>
          <h1 id="announcements-title">Announcements</h1>
          <p className="muted">Current notices, audience targeting and validated document attachments.</p>
        </div>
        {hasAnyRole('Administrator', 'Registrar') ? (
          <button className="button button-primary" type="button" onClick={() => setShowForm((value) => !value)}>
            {showForm ? 'Close form' : 'New announcement'}
          </button>
        ) : null}
      </div>

      {showForm ? (
        <form className="panel form-stack" onSubmit={(event) => void publish(event)}>
          <h2>Publish announcement</h2>
          <label><span>Title</span><input name="title" maxLength={200} required /></label>
          <label><span>Message</span><textarea name="body" rows={6} required /></label>
          <div className="form-grid compact-grid">
            <label><span>Audience</span><select name="audience" defaultValue="1">{audiences.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
            <label><span>Publish at</span><input name="publishAt" type="datetime-local" required defaultValue={toLocalInput(new Date())} /></label>
            <label><span>Expires at</span><input name="expiresAt" type="datetime-local" /></label>
            <label><span>Attachment</span><input name="attachment" type="file" accept=".pdf,.png,.jpg,.jpeg,.txt,.csv,.docx,.xlsx" /><small className="muted">Optional · maximum 10 MB · server validates file signature and type.</small></label>
          </div>
          <button className="button button-primary" disabled={busy} type="submit">{busy ? 'Publishing…' : 'Publish'}</button>
        </form>
      ) : null}

      {notice ? <div className="form-success" role="status">{notice}</div> : null}
      {error ? <div className="form-error" role="alert">{error}</div> : null}
      {resource.loading ? <LoadingState label="Loading announcements…" /> : null}
      {resource.error ? <ErrorState message={resource.error} onRetry={resource.reload} /> : null}
      {resource.data?.length === 0 ? <EmptyState title="No current announcements" message="Published notices will appear here while they are active." /> : null}
      {resource.data && resource.data.length > 0 ? (
        <div className="announcement-list">
          {resource.data.map((announcement) => (
            <article className="panel announcement-card" key={announcement.id}>
              <div className="page-heading compact-heading">
                <div><span className="status-pill">{audienceLabel(announcement.audience)}</span><h2>{announcement.title}</h2></div>
                <time className="muted" dateTime={announcement.publishAtUtc}>{new Date(announcement.publishAtUtc).toLocaleString()}</time>
              </div>
              <p className="preserve-lines">{announcement.body}</p>
              {announcement.attachments.length ? (
                <div className="attachment-list" aria-label="Attachments">
                  {announcement.attachments.map((attachment) => (
                    <button className="attachment-button" type="button" key={attachment.id} onClick={() => void download(announcement.id, attachment.id, attachment.fileName)}>
                      <span>{attachment.fileName}</span>
                      <small>{Math.max(1, Math.round(attachment.sizeBytes / 1024)).toLocaleString()} KB</small>
                    </button>
                  ))}
                </div>
              ) : null}
              {announcement.expiresAtUtc ? <small className="muted">Expires {new Date(announcement.expiresAtUtc).toLocaleString()}</small> : null}
            </article>
          ))}
        </div>
      ) : null}
    </section>
  );
}
