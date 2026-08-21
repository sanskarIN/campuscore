import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AuthResponse } from '../types';
import { saveSession } from '../auth/session';
import { ApiError, apiRequest } from './client';

function memoryStorage(): Storage {
  const values = new Map<string, string>();
  return {
    get length() { return values.size; },
    clear: () => values.clear(),
    getItem: (key) => values.get(key) ?? null,
    key: (index) => [...values.keys()][index] ?? null,
    removeItem: (key) => { values.delete(key); },
    setItem: (key, value) => { values.set(key, value); },
  };
}

const session: AuthResponse = {
  accessToken: 'secure-test-token',
  expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
  displayName: 'Test User',
  roles: ['Teacher'],
};

describe('API client', () => {
  beforeEach(() => {
    vi.stubGlobal('window', { sessionStorage: memoryStorage(), dispatchEvent: vi.fn() });
    saveSession(session);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('adds the bearer token without sending cookies', async () => {
    const fetchMock = vi.fn(async (_url: string, init?: RequestInit) => {
      const headers = new Headers(init?.headers);
      expect(headers.get('Authorization')).toBe('Bearer secure-test-token');
      expect(init?.credentials).toBe('omit');
      return new Response(JSON.stringify({ ok: true }), { status: 200, headers: { 'Content-Type': 'application/json' } });
    });
    vi.stubGlobal('fetch', fetchMock);

    await expect(apiRequest<{ ok: boolean }>('/api/test')).resolves.toEqual({ ok: true });
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it('surfaces safe problem detail messages', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ detail: 'Validation failed safely.' }), {
      status: 400,
      headers: { 'Content-Type': 'application/problem+json' },
    })));

    await expect(apiRequest('/api/test')).rejects.toMatchObject<ApiError>({ status: 400, message: 'Validation failed safely.' });
  });

  it('clears the session after an authenticated 401 response', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(null, { status: 401 })));

    await expect(apiRequest('/api/test')).rejects.toMatchObject({ status: 401 });
    expect(window.sessionStorage.length).toBe(0);
    expect(window.dispatchEvent).toHaveBeenCalledOnce();
  });
});
