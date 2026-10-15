import type { AuthResponse } from '../types';

const SESSION_KEY = 'campuscore.auth';

const storage = (): Storage | null => (typeof window === 'undefined' ? null : window.sessionStorage);

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isAuthResponse(value: unknown): value is AuthResponse {
  if (!isRecord(value)) return false;
  if (typeof value.accessToken !== 'string' || value.accessToken.trim().length === 0) return false;
  if (typeof value.expiresAtUtc !== 'string' || value.expiresAtUtc.trim().length === 0) return false;
  if (typeof value.displayName !== 'string') return false;
  if (!Array.isArray(value.roles) || value.roles.some((role) => typeof role !== 'string')) return false;

  const expiresAt = Date.parse(value.expiresAtUtc);
  return Number.isFinite(expiresAt) && expiresAt > Date.now();
}

export function loadSession(): AuthResponse | null {
  const raw = storage()?.getItem(SESSION_KEY);
  if (!raw) return null;

  try {
    const parsed: unknown = JSON.parse(raw);
    if (!isAuthResponse(parsed)) {
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
