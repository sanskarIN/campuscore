import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { apiRequest } from '../api/client';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncState';
import type { SearchResult } from '../types';

export function SearchPage() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const search = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const normalized = query.trim();
    if (normalized.length < 2) {
      setResults([]);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setResults(await apiRequest<SearchResult[]>(`/api/search?q=${encodeURIComponent(normalized)}`));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Search failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="page-stack" aria-labelledby="search-title">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Find records quickly</p>
          <h1 id="search-title">Global search</h1>
          <p className="muted">Search students by admission/name and staff by employee number/name.</p>
        </div>
      </div>

      <form className="search-bar" role="search" onSubmit={search}>
        <label className="grow-field">
          <span className="sr-only">Search CampusCore</span>
          <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Type at least 2 characters" autoFocus />
        </label>
        <button className="button button-primary" type="submit" disabled={loading}>Search</button>
      </form>

      {loading ? <LoadingState label="Searching…" /> : null}
      {error ? <ErrorState message={error} /> : null}
      {results?.length === 0 ? <EmptyState title="No matching records" message="Try a name, admission number, or employee number." /> : null}
      {results && results.length > 0 ? (
        <div className="result-list" aria-live="polite">
          {results.map((result) => (
            <Link className="result-row" key={`${result.type}-${result.id}`} to={result.type === 'student' ? `/students?q=${encodeURIComponent(result.title)}` : '/staff'}>
              <div>
                <strong>{result.title}</strong>
                <span className="muted">{result.subtitle}</span>
              </div>
              <span className="status-pill">{result.type}</span>
            </Link>
          ))}
        </div>
      ) : null}
    </section>
  );
}
