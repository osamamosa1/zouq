import { useEffect, useState } from 'react';
import { adminApi, type OrderRow } from '../api/admin';
import { PageShell } from '../components/PageShell';

const STATUSES = [
  'Pending',
  'Confirmed',
  'InProduction',
  'Shipped',
  'Delivered',
  'Cancelled',
  'Refunded',
];

export function OrdersPage() {
  const [rows, setRows] = useState<OrderRow[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState<string | null>(null);

  function load() {
    setLoading(true);
    adminApi
      .orders()
      .then(setRows)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
  }, []);

  async function changeStatus(id: string, status: string) {
    setUpdating(id);
    setError('');
    try {
      await adminApi.setOrderStatus(id, status);
      setRows((prev) => prev.map((o) => (o.id === id ? { ...o, status } : o)));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Update failed');
    } finally {
      setUpdating(null);
    }
  }

  return (
    <PageShell title="Orders">
      <div className="toolbar">
        <span className="muted">{rows.length} orders</span>
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
                <th>Order #</th>
                <th>Buyer</th>
                <th>Total</th>
                <th>Status</th>
                <th>Created</th>
                <th>Update</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((o) => (
                <tr key={o.id}>
                  <td>{o.order_number}</td>
                  <td>
                    {o.buyer}
                    <br />
                    <span className="muted" style={{ fontSize: '0.8rem' }}>
                      {o.buyer_email}
                    </span>
                  </td>
                  <td>
                    {o.total} {o.currency}
                  </td>
                  <td>{o.status}</td>
                  <td>{new Date(o.created_at_utc).toLocaleString()}</td>
                  <td>
                    <select
                      value={o.status}
                      disabled={updating === o.id}
                      onChange={(e) => changeStatus(o.id, e.target.value)}
                    >
                      {STATUSES.map((s) => (
                        <option key={s} value={s}>
                          {s}
                        </option>
                      ))}
                    </select>
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
