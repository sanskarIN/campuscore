import type { ReactNode } from 'react';

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="state-panel" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <p>{label}</p>
    </div>
  );
}

export function EmptyState({ title, message, action }: { title: string; message: string; action?: ReactNode }) {
  return (
    <div className="state-panel">
      <strong>{title}</strong>
      <p>{message}</p>
      {action}
    </div>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="state-panel state-panel-error" role="alert">
      <strong>Something went wrong</strong>
      <p>{message}</p>
      {onRetry ? (
        <button className="button button-secondary" type="button" onClick={onRetry}>
          Try again
        </button>
      ) : null}
    </div>
  );
}
