import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { login, bootstrap } = useAuth();
  const navigate = useNavigate();
  const [mode, setMode] = useState<'login' | 'bootstrap'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [bootstrapKey, setBootstrapKey] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      if (mode === 'login') await login(email, password);
      else await bootstrap(email, password, displayName, bootstrapKey);
      navigate('/', { replace: true });
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Authentication failed.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <main id="main-content" className="auth-layout">
      <section className="auth-card" aria-labelledby="auth-title">
        <div className="auth-brand">
          <img src="/logo.svg" width="56" height="56" alt="" />
          <div>
            <p className="eyebrow">Student management, without the clutter</p>
            <h1 id="auth-title">CampusCore</h1>
          </div>
        </div>

        <div className="segmented" role="group" aria-label="Authentication mode">
          <button type="button" className={mode === 'login' ? 'active' : ''} onClick={() => setMode('login')}>
            Sign in
          </button>
          <button type="button" className={mode === 'bootstrap' ? 'active' : ''} onClick={() => setMode('bootstrap')}>
            First-run setup
          </button>
        </div>

        <form onSubmit={submit} className="form-stack">
          {mode === 'bootstrap' ? (
            <label>
              <span>Display name</span>
              <input value={displayName} onChange={(event) => setDisplayName(event.target.value)} autoComplete="name" required />
            </label>
          ) : null}
          <label>
            <span>Email</span>
            <input value={email} onChange={(event) => setEmail(event.target.value)} type="email" autoComplete="email" required />
          </label>
          <label>
            <span>Password</span>
            <input value={password} onChange={(event) => setPassword(event.target.value)} type="password" autoComplete={mode === 'login' ? 'current-password' : 'new-password'} minLength={8} required />
          </label>
          {mode === 'bootstrap' ? (
            <label>
              <span>Bootstrap key</span>
              <input value={bootstrapKey} onChange={(event) => setBootstrapKey(event.target.value)} type="password" autoComplete="off" required />
              <small className="muted">Used only to create the first administrator. It is never stored by the web app.</small>
            </label>
          ) : null}
          {error ? <div className="form-error" role="alert">{error}</div> : null}
          <button className="button button-primary" type="submit" disabled={busy}>
            {busy ? 'Working…' : mode === 'login' ? 'Sign in' : 'Create first administrator'}
          </button>
        </form>

        <p className="auth-footnote">Open source · MIT licensed · Made by the Sanskar</p>
      </section>
    </main>
  );
}
