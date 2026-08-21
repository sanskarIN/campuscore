import { useState } from 'react';
import { apiRequest } from '../api/client';
import type { SearchResult } from '../types';

interface StudentPickerProps {
  name: string;
  label?: string;
  required?: boolean;
}

export function StudentPicker({ name, label = 'Student', required = true }: StudentPickerProps) {
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<SearchResult | null>(null);
  const [results, setResults] = useState<SearchResult[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const search = async () => {
    const value = query.trim();
    if (value.length < 2) {
      setResults([]);
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const items = await apiRequest<SearchResult[]>(`/api/search?q=${encodeURIComponent(value)}`);
      setResults(items.filter((item) => item.type === 'student'));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Student search failed.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="picker-field">
      <span className="field-label">{label}</span>
      <input type="hidden" name={name} value={selected?.id ?? ''} required={required} />
      {selected ? (
        <div className="picker-selection">
          <span><strong>{selected.title}</strong><small>{selected.subtitle}</small></span>
          <button className="button button-ghost" type="button" onClick={() => { setSelected(null); setQuery(''); }}>Change</button>
        </div>
      ) : (
        <>
          <div className="search-bar compact-search">
            <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Name or admission number" aria-label={`${label} search`} />
            <button className="button button-secondary" type="button" disabled={busy || query.trim().length < 2} onClick={() => void search()}>{busy ? 'Searching…' : 'Find'}</button>
          </div>
          {error ? <small className="field-error" role="alert">{error}</small> : null}
          {results.length > 0 ? (
            <div className="picker-results" role="listbox" aria-label="Student matches">
              {results.map((result) => (
                <button key={result.id} type="button" role="option" aria-selected="false" onClick={() => { setSelected(result); setResults([]); }}>
                  <strong>{result.title}</strong><span>{result.subtitle}</span>
                </button>
              ))}
            </div>
          ) : null}
        </>
      )}
    </div>
  );
}
