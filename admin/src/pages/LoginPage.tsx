import { useState, type FormEvent } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { getApiBase, setApiBase } from '../api/client';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { token, login } = useAuth();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/';

  const [apiBase, setApiBaseLocal] = useState(getApiBase());
  const [email, setEmail] = useState('admin@zouq.app');
  const [password, setPassword] = useState('Admin@12345');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  if (token) return <Navigate to={from} replace />;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    setApiBase(apiBase);
    try {
      await login(email, password);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <h1>zouq</h1>
        <p className="muted">Admin dashboard</p>
        <form onSubmit={onSubmit}>
          <div>
            <label htmlFor="api-base">API base URL</label>
            <input
              id="api-base"
              value={apiBase}
              onChange={(e) => setApiBaseLocal(e.target.value)}
              placeholder="http://localhost:5280"
            />
          </div>
          <div>
            <label htmlFor="email">Email</label>
            <input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div>
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          <button type="submit" className="btn" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
          {error ? <p className="error">{error}</p> : null}
        </form>
      </div>
    </div>
  );
}
