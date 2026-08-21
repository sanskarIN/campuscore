import { useEffect, useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useTheme } from '../theme/ThemeContext';

const navItems = [
  ['/', 'Dashboard'],
  ['/search', 'Search'],
  ['/students', 'Students'],
  ['/academics', 'Academics'],
  ['/operations', 'Operations'],
  ['/staff', 'Staff'],
  ['/announcements', 'Announcements'],
  ['/about', 'About'],
] as const;

export function AppShell() {
  const { currentUser, session, logout, hasAnyRole } = useAuth();
  const { preference, setPreference } = useTheme();
  const [online, setOnline] = useState(() => navigator.onLine);

  useEffect(() => {
    const onOnline = () => setOnline(true);
    const onOffline = () => setOnline(false);
    window.addEventListener('online', onOnline);
    window.addEventListener('offline', onOffline);
    return () => {
      window.removeEventListener('online', onOnline);
      window.removeEventListener('offline', onOffline);
    };
  }, []);

  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Primary navigation">
        <NavLink className="brand" to="/" aria-label="CampusCore dashboard">
          <img src="/logo.svg" alt="" width="38" height="38" />
          <span>CampusCore</span>
        </NavLink>
        <nav className="nav-list">
          {navItems.map(([to, label]) => (
            <NavLink key={to} to={to} end={to === '/'} className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
              {label}
            </NavLink>
          ))}
          {hasAnyRole('Administrator') ? (
            <>
              <NavLink to="/catalog" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>Academic catalog</NavLink>
              <NavLink to="/settings" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>Settings & audit</NavLink>
            </>
          ) : null}
        </nav>
        <div className="sidebar-footer">
          <span className={online ? 'status-pill success' : 'status-pill warning'}>{online ? 'Online' : 'Offline shell'}</span>
          <small>Made by the Sanskar</small>
        </div>
      </aside>

      <div className="app-main">
        <header className="topbar">
          <div>
            <strong>{currentUser?.name ?? session?.displayName ?? 'CampusCore user'}</strong>
            <span className="muted topbar-roles">{(currentUser?.roles ?? session?.roles ?? []).join(' · ') || 'Authenticated'}</span>
          </div>
          <div className="topbar-actions">
            <label className="compact-field">
              <span className="sr-only">Theme</span>
              <select value={preference} onChange={(event) => setPreference(event.target.value as 'light' | 'dark' | 'system')}>
                <option value="system">System theme</option>
                <option value="light">Light theme</option>
                <option value="dark">Dark theme</option>
              </select>
            </label>
            <button className="button button-secondary" type="button" onClick={logout}>Sign out</button>
          </div>
        </header>
        {!online ? (
          <div className="offline-banner" role="status">
            You are offline. Previously loaded app assets remain available, but private API data is never cached.
          </div>
        ) : null}
        <main id="main-content" className="content" tabIndex={-1}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
