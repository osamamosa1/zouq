import { useEffect, useState } from 'react';
import { adminApi, type FeedItem } from '../api/admin';
import { PageShell } from '../components/PageShell';

export function FeaturedPage() {
  const [items, setItems] = useState<FeedItem[]>([]);
  const [designId, setDesignId] = useState('');
  const [isFeatured, setIsFeatured] = useState(true);
  const [priority, setPriority] = useState('0');
  const [error, setError] = useState('');
  const [msg, setMsg] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    adminApi
      .feedForYou()
      .then(setItems)
      .catch((e: Error) => setError(e.message));
  }, []);

  async function apply() {
    if (!designId.trim()) {
      setError('Enter a design ID.');
      return;
    }
    setError('');
    setMsg('');
    setLoading(true);
    try {
      await adminApi.featureDesign(designId.trim(), {
        is_featured: isFeatured,
        priority: Number(priority),
      });
      setMsg('Featured status updated.');
      const feed = await adminApi.feedForYou();
      setItems(feed);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  async function toggleFromRow(item: FeedItem) {
    setDesignId(item.design_id);
    setLoading(true);
    setError('');
    try {
      await adminApi.featureDesign(item.design_id, {
        is_featured: !item.is_featured,
        priority: item.featured_priority,
      });
      const feed = await adminApi.feedForYou();
      setItems(feed);
      setMsg(`Toggled ${item.title}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <PageShell title="Featured designs">
      <div className="card">
        <h3>Set featured by design ID</h3>
        <div className="form-grid">
          <div>
            <label>Design ID (GUID)</label>
            <input value={designId} onChange={(e) => setDesignId(e.target.value)} placeholder="uuid" />
          </div>
          <div className="form-row-2">
            <div>
              <label>Featured</label>
              <select value={isFeatured ? 'yes' : 'no'} onChange={(e) => setIsFeatured(e.target.value === 'yes')}>
                <option value="yes">Yes</option>
                <option value="no">No</option>
              </select>
            </div>
            <div>
              <label>Priority</label>
              <input type="number" value={priority} onChange={(e) => setPriority(e.target.value)} />
            </div>
          </div>
          <button type="button" className="btn" onClick={apply} disabled={loading}>
            Save
          </button>
        </div>
        {msg ? <p className="success">{msg}</p> : null}
        {error ? <p className="error">{error}</p> : null}
      </div>
      <div className="card">
        <h3>Feed designs (toggle featured)</h3>
        <p className="muted">Loaded from GET /api/feed/for-you</p>
        <table>
          <thead>
            <tr>
              <th>Title</th>
              <th>Owner</th>
              <th>Featured</th>
              <th>Priority</th>
              <th>Design ID</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.design_id}>
                <td>{item.title}</td>
                <td>{item.owner_name}</td>
                <td>{item.is_featured ? 'Yes' : 'No'}</td>
                <td>{item.featured_priority}</td>
                <td>
                  <code style={{ fontSize: '0.7rem' }}>{item.design_id}</code>
                </td>
                <td>
                  <button
                    type="button"
                    className="btn btn-sm btn-ghost"
                    disabled={loading}
                    onClick={() => toggleFromRow(item)}
                  >
                    Toggle
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {items.length === 0 ? <p className="muted">No designs in feed.</p> : null}
      </div>
    </PageShell>
  );
}
