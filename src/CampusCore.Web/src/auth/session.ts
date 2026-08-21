import type { AuthResponse } from '../types';

const SESSION_KEY = 'campuscore.auth';

const storage = (): Storage | null => (typeof window === 'undefined' ? null : window.sessionStorage);

export function loadSession(): AuthResponse | null {
  const raw = storage()?.getItem(SESSION_KEY);
  if (!raw) return null;

  try {
    const parsed = JSON.parse(raw) as AuthResponse;
    if (!parsed.accessToken || !parsed.expiresAtUtc || new Date(parsed.expiresAtUtc).getTime() <= Date.now()) {
      clearSession();
      return null;
    }
    return parsed;
  } catch {
    clearSession();
    return null;
  }
}

export function saveSession(session: AuthResponse): void {
  storage()?.setItem(SESSION_KEY, JSON.stringify(session));
}

export function clearSession(): void {
  storage()?.removeItem(SESSION_KEY);
}

export function getAccessToken(): string | null {
  return loadSession()?.accessToken ?? null;
}
