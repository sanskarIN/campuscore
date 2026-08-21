import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AuthResponse } from '../types';
import { clearSession, getAccessToken, loadSession, saveSession } from './session';

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

const validSession = (): AuthResponse => ({
  accessToken: 'token-value',
  expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
  displayName: 'Test Administrator',
  roles: ['Administrator'],
});

describe('auth session storage', () => {
  beforeEach(() => {
    vi.stubGlobal('window', { sessionStorage: memoryStorage() });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('stores and reloads a valid session in sessionStorage', () => {
    const session = validSession();
    saveSession(session);
    expect(loadSession()).toEqual(session);
    expect(getAccessToken()).toBe('token-value');
  });

  it('rejects and clears expired sessions', () => {
    saveSession({ ...validSession(), expiresAtUtc: new Date(Date.now() - 1_000).toISOString() });
    expect(loadSession()).toBeNull();
    expect(window.sessionStorage.length).toBe(0);
  });

  it('clears malformed session data safely', () => {
    window.sessionStorage.setItem('campuscore.auth', '{not-json');
    expect(loadSession()).toBeNull();
    expect(window.sessionStorage.length).toBe(0);
  });

  it('removes a session on explicit logout cleanup', () => {
    saveSession(validSession());
    clearSession();
    expect(getAccessToken()).toBeNull();
  });
});
