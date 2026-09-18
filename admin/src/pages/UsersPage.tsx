import { useEffect, useState } from 'react';
import { adminApi, type UserRow } from '../api/admin';
import { PageShell } from '../components/PageShell';

export function UsersPage() {
  const [rows, setRows] = useState<UserRow[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [amounts, setAmounts] = useState<Record<string, string>>({});
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [updating, setUpdating] = useState<string | null>(null);

  function load() {
    setLoading(true);
    adminApi
      .users()
      .then(setRows)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
  }, []);

  async function adjust(user: UserRow) {
    const raw = amounts[user.id] ?? '0';
    const amount = Number(raw);
    const reason = (reasons[user.id] ?? '').trim();
    if (Number.isNaN(amount) || amount === 0) {
      setError('Enter a non-zero amount.');
      return;
    }
    if (!reason) {
      setError('Reason is required for balance adjustments.');
      return;
    }
    if (!confirm(`Adjust ${user.email} by ${amount}? Reason: ${reason}`)) return;
    setUpdating(user.id);
    setError('');
    try {
      const res = await adminApi.adjustBalance(user.id, {
        amount,
        reason,
      });
      setRows((prev) => prev.map((u) => (u.id === user.id ? { ...u, balance: res.balance } : u)));
      setAmounts((a) => ({ ...a, [user.id]: '' }));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    } finally {
      setUpdating(null);
    }
  }

  return (
    <PageShell title="Users">
      <div className="toolbar">
        <span className="muted">{rows.length} users</span>
        <button type="button" className="btn btn-ghost btn-sm" onClick={load}>
          Refresh
        </button>
      </div>
      {loading ? <p className="muted">Loading…</p> : null}
      {error ? <p className="error">{error}</p> : null}
      {!loading ? (
        <div className="card">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Balance</th>
                <th>Adjust (+/−)</th>
                <th>Reason</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {rows.map((u) => (
                <tr key={u.id}>
                  <td>{u.name}</td>
                  <td>{u.email}</td>
                  <td>{u.role}</td>
                  <td>{u.balance}</td>
                  <td>
                    <input
                      type="number"
                      step="0.01"
                      placeholder="10 or -5"
                      value={amounts[u.id] ?? ''}
                      onChange={(e) => setAmounts((a) => ({ ...a, [u.id]: e.target.value }))}
                      style={{ minWidth: '100px' }}
                    />
                  </td>
                  <td>
                    <input
                      value={reasons[u.id] ?? ''}
                      onChange={(e) => setReasons((r) => ({ ...r, [u.id]: e.target.value }))}
                      placeholder="Reason"
                      style={{ minWidth: '120px' }}
                    />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="btn btn-sm"
                      disabled={updating === u.id}
                      onClick={() => adjust(u)}
                    >
                      Apply
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </PageShell>
  );
}
