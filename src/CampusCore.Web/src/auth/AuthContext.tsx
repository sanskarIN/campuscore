import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { apiJson, apiRequest } from '../api/client';
import type { AuthResponse, CurrentUser, Role } from '../types';
import { clearSession, loadSession, saveSession } from './session';

interface AuthContextValue {
  session: AuthResponse | null;
  currentUser: CurrentUser | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  bootstrap: (email: string, password: string, displayName: string, bootstrapKey: string) => Promise<void>;
  logout: () => void;
  hasAnyRole: (...roles: Role[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthResponse | null>(() => loadSession());
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);

  const acceptSession = useCallback((next: AuthResponse) => {
    saveSession(next);
    setSession(next);
    setCurrentUser({ id: null, name: next.displayName, roles: next.roles });
  }, []);

  const logout = useCallback(() => {
    clearSession();
    setSession(null);
    setCurrentUser(null);
  }, []);

  const login = useCallback(
    async (email: string, password: string) => {
      const response = await apiJson<AuthResponse>('/api/auth/login', 'POST', { email, password });
      acceptSession(response);
    },
    [acceptSession],
  );

  const bootstrap = useCallback(
    async (email: string, password: string, displayName: string, bootstrapKey: string) => {
      const response = await apiRequest<AuthResponse>('/api/auth/bootstrap', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Bootstrap-Key': bootstrapKey },
        body: JSON.stringify({ email, password, displayName }),
      });
      acceptSession(response);
    },
    [acceptSession],
  );

  useEffect(() => {
    const onUnauthorized = () => logout();
    window.addEventListener('campuscore:unauthorized', onUnauthorized);
    return () => window.removeEventListener('campuscore:unauthorized', onUnauthorized);
  }, [logout]);

  useEffect(() => {
    if (!session) return;
    let cancelled = false;
    void apiRequest<CurrentUser>('/api/auth/me')
      .then((user) => {
        if (!cancelled) setCurrentUser(user);
      })
      .catch(() => {
        if (!cancelled) logout();
      });
    return () => {
      cancelled = true;
    };
  }, [logout, session]);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      currentUser,
      isAuthenticated: session !== null,
      login,
      bootstrap,
      logout,
      hasAnyRole: (...roles) => {
        const owned = currentUser?.roles ?? session?.roles ?? [];
        return roles.some((role) => owned.includes(role));
      },
    }),
    [bootstrap, currentUser, login, logout, session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext);
  if (!value) throw new Error('useAuth must be used within AuthProvider.');
  return value;
}
