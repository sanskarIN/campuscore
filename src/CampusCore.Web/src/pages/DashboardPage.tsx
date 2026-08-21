import { apiDownload, apiRequest } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorState, LoadingState } from '../components/AsyncState';
import { useApiResource } from '../hooks/useApiResource';
import type { DashboardSummary } from '../types';

const metrics: Array<{ key: keyof DashboardSummary; label: string; hint: string }> = [
  { key: 'activeStudents', label: 'Active students', hint: 'Currently active profiles' },
  { key: 'activeStaff', label: 'Active staff', hint: 'Active staff members' },
  { key: 'sections', label: 'Sections', hint: 'Configured academic sections' },
  { key: 'presentToday', label: 'Present today', hint: 'Present or late attendance' },
  { key: 'absentToday', label: 'Absent today', hint: 'Marked absent today' },
  { key: 'pendingLeaveRequests', label: 'Pending leave', hint: 'Awaiting a decision' },
  { key: 'publishedAnnouncements', label: 'Announcements', hint: 'Currently published' },
];

export function DashboardPage() {
  const { hasAnyRole } = useAuth();
  const resource = useApiResource(() => apiRequest<DashboardSummary>('/api/dashboard'));

  const downloadStudents = async () => {
    const blob = await apiDownload('/api/reports/students.csv');
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'students.csv';
    anchor.click();
    URL.revokeObjectURL(url);
  };

  return (
    <section className="page-stack" aria-labelledby="dashboard-title">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Overview</p>
          <h1 id="dashboard-title">Dashboard</h1>
          <p className="muted">Privacy-conscious operational counts for today.</p>
        </div>
        {hasAnyRole('Administrator', 'Registrar') ? (
          <button className="button button-secondary" type="button" onClick={() => void downloadStudents()}>
            Export students CSV
          </button>
        ) : null}
      </div>

      {resource.loading ? <LoadingState label="Loading dashboard…" /> : null}
      {resource.error ? <ErrorState message={resource.error} onRetry={resource.reload} /> : null}
      {resource.data ? (
        <div className="metric-grid">
          {metrics.map((metric) => (
            <article className="metric-card" key={metric.key}>
              <span className="metric-value">{resource.data?.[metric.key].toLocaleString()}</span>
              <strong>{metric.label}</strong>
              <small className="muted">{metric.hint}</small>
            </article>
          ))}
        </div>
      ) : null}

      <div className="content-grid two-column">
        <article className="panel">
          <h2>Quick workflow</h2>
          <p className="muted">Use global search to locate a student or staff record, then move into the focused workflow.</p>
          <a className="button button-primary inline-button" href="/search">Open search</a>
        </article>
        <article className="panel">
          <h2>Data handling</h2>
          <p className="muted">The service worker caches only public application assets. Authenticated API responses are never cached for offline use.</p>
        </article>
      </div>
    </section>
  );
}
