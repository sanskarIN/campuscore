import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it } from 'vitest';
import { EmptyState, ErrorState, LoadingState } from './AsyncState';

describe('async state components', () => {
  it('renders an accessible loading status', () => {
    const html = renderToStaticMarkup(<LoadingState label="Loading students…" />);
    expect(html).toContain('role="status"');
    expect(html).toContain('Loading students');
  });

  it('renders empty state guidance', () => {
    const html = renderToStaticMarkup(<EmptyState title="No records" message="Add the first record." />);
    expect(html).toContain('No records');
    expect(html).toContain('Add the first record.');
  });

  it('renders errors with alert semantics', () => {
    const html = renderToStaticMarkup(<ErrorState message="Safe error message" />);
    expect(html).toContain('role="alert"');
    expect(html).toContain('Safe error message');
  });
});
