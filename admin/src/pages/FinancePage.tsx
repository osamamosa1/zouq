import { useEffect, useState } from 'react';
import { adminApi } from '../api/admin';
import { PageShell } from '../components/PageShell';

type DesignRow = {
  id: string;
  title: string;
  status: string;
  visibility: string;
  is_featured: boolean;
  featured_priority: number;
  tags: string[];
  source_order_id?: string | null;
  owner: string;
  owner_id: string;
};

type LedgerRow = {
  id: string;
  user_id: string;
  entry_type: string;
  transaction_type: string;
  amount: number;
  balance_after: number;
  reason: string;
  reference: string;
  order_id?: string | null;
  created_at_utc: string;
};

type RewardRow = {
  id: string;
  order_id: string;
  creator_id: string;
  amount: number;
  commission_percent: number;
  status: string;
};

export function FinancePage() {
  const [ledger, setLedger] = useState<LedgerRow[]>([]);
  const [rewards, setRewards] = useState<RewardRow[]>([]);
  const [commission, setCommission] = useState('10');
  const [error, setError] = useState('');
  const [msg, setMsg] = useState('');

  async function load() {
    try {
      const [l, r, c] = await Promise.all([
        adminApi.ledger(),
        adminApi.creatorRewards(),
        adminApi.getCommission(),
      ]);
      setLedger(l);
      setRewards(r);
      setCommission(String(c.designer_commission_percent ?? 10));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load finance');
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function saveCommission() {
    setError('');
    setMsg('');
    try {
      await adminApi.setCommission(Number(commission));
      setMsg('Commission updated (applies to new orders only).');
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  return (
    <PageShell title="Finance">
      <div className="card">
        <h3>Designer commission %</h3>
        <div className="form-row-2">
          <input type="number" value={commission} onChange={(e) => setCommission(e.target.value)} />
          <button type="button" className="btn" onClick={saveCommission}>
            Save
          </button>
        </div>
        <p className="muted">Historical orders keep their CommissionPercentSnapshot.</p>
        {msg ? <p className="success">{msg}</p> : null}
        {error ? <p className="error">{error}</p> : null}
      </div>
      <div className="card">
        <h3>Creator rewards</h3>
        <table>
          <thead>
            <tr>
              <th>Order</th>
              <th>Creator</th>
              <th>Amount</th>
              <th>%</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {rewards.map((r) => (
              <tr key={r.id}>
                <td>
                  <code style={{ fontSize: '0.7rem' }}>{r.order_id}</code>
                </td>
                <td>
                  <code style={{ fontSize: '0.7rem' }}>{r.creator_id}</code>
                </td>
                <td>{r.amount}</td>
                <td>{r.commission_percent}</td>
                <td>{r.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="card">
        <h3>Ledger (latest)</h3>
        <table>
          <thead>
            <tr>
              <th>When</th>
              <th>Type</th>
              <th>Amount</th>
              <th>Balance after</th>
              <th>Reason</th>
              <th>Reference</th>
            </tr>
          </thead>
          <tbody>
            {ledger.map((l) => (
              <tr key={l.id}>
                <td>{new Date(l.created_at_utc).toLocaleString()}</td>
                <td>
                  {l.entry_type}/{l.transaction_type}
                </td>
                <td>{l.amount}</td>
                <td>{l.balance_after}</td>
                <td>{l.reason}</td>
                <td>
                  <code style={{ fontSize: '0.65rem' }}>{l.reference}</code>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </PageShell>
  );
}

export function DesignsAdminPage() {
  const [rows, setRows] = useState<DesignRow[]>([]);
  const [status, setStatus] = useState('Reusable');
  const [error, setError] = useState('');
  const [msg, setMsg] = useState('');

  async function load(filter = status) {
    try {
      setRows(await adminApi.designs(filter || undefined));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function publish(id: string) {
    setError('');
    setMsg('');
    try {
      await adminApi.adminPublishDesign(id);
      setMsg('Design published as reusable.');
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Publish failed');
    }
  }

  return (
    <PageShell title="Designs moderation">
      <div className="card">
        <div className="form-row-2">
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">All</option>
            <option value="Draft">Draft</option>
            <option value="Ordered">Ordered</option>
            <option value="DeliveredEligible">DeliveredEligible</option>
            <option value="Reusable">Reusable</option>
          </select>
          <button type="button" className="btn" onClick={() => load(status)}>
            Filter
          </button>
        </div>
        {msg ? <p className="success">{msg}</p> : null}
        {error ? <p className="error">{error}</p> : null}
        <table>
          <thead>
            <tr>
              <th>Title</th>
              <th>Owner</th>
              <th>Status</th>
              <th>Visibility</th>
              <th>Tags</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {rows.map((d) => (
              <tr key={d.id}>
                <td>{d.title}</td>
                <td>{d.owner}</td>
                <td>{d.status}</td>
                <td>{d.visibility}</td>
                <td>{(d.tags ?? []).join(', ')}</td>
                <td>
                  {d.status === 'DeliveredEligible' ? (
                    <button type="button" className="btn btn-sm" onClick={() => publish(d.id)}>
                      Publish
                    </button>
                  ) : null}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </PageShell>
  );
}
