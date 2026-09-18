import { useEffect, useState, type FormEvent } from 'react';
import { adminApi, type AdRow } from '../api/admin';
import { PageShell } from '../components/PageShell';

export function AdsPage() {
  const [ads, setAds] = useState<AdRow[]>([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [linkUrl, setLinkUrl] = useState('');
  const [actionType, setActionType] = useState('');
  const [placement, setPlacement] = useState('Banner');
  const [priority, setPriority] = useState('0');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);

  function loadAds() {
    adminApi
      .activeAds()
      .then(setAds)
      .catch((e: Error) => setErr(e.message));
  }

  useEffect(() => {
    loadAds();
  }, []);

  async function onCreate(e: FormEvent) {
    e.preventDefault();
    setErr('');
    setMsg('');
    setLoading(true);
    try {
      const res = await adminApi.createAd({
        title,
        description: description || undefined,
        image_url: imageUrl || undefined,
        link_url: linkUrl || undefined,
        action_type: actionType || undefined,
        placement,
        display_priority: Number(priority),
      });
      setMsg(`Ad created: ${res.title}`);
      setTitle('');
      loadAds();
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <PageShell title="Ads / banners">
      <p className="muted">
        Create via admin API. Active ads are listed from the public feed endpoint (update/delete not exposed on API yet).
      </p>
      <div className="card">
        <h3>Create ad / banner</h3>
        <form className="form-grid" onSubmit={onCreate}>
          <div>
            <label>Title</label>
            <input value={title} onChange={(e) => setTitle(e.target.value)} required />
          </div>
          <div>
            <label>Description</label>
            <input value={description} onChange={(e) => setDescription(e.target.value)} />
          </div>
          <div>
            <label>Image URL</label>
            <input value={imageUrl} onChange={(e) => setImageUrl(e.target.value)} />
          </div>
          <div>
            <label>Link URL</label>
            <input value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} />
          </div>
          <div className="form-row-2">
            <div>
              <label>Action type</label>
              <input value={actionType} onChange={(e) => setActionType(e.target.value)} />
            </div>
            <div>
              <label>Placement</label>
              <select value={placement} onChange={(e) => setPlacement(e.target.value)}>
                <option value="Banner">Banner</option>
                <option value="Popup">Popup</option>
                <option value="Feed">Feed</option>
                <option value="Splash">Splash</option>
              </select>
            </div>
          </div>
          <div>
            <label>Display priority</label>
            <input type="number" value={priority} onChange={(e) => setPriority(e.target.value)} />
          </div>
          <button type="submit" className="btn" disabled={loading}>
            Create ad
          </button>
        </form>
        {msg ? <p className="success">{msg}</p> : null}
        {err ? <p className="error">{err}</p> : null}
      </div>
      <div className="card">
        <h3>Active ads</h3>
        <button type="button" className="btn btn-ghost btn-sm" onClick={loadAds} style={{ marginBottom: '0.75rem' }}>
          Refresh
        </button>
        <table>
          <thead>
            <tr>
              <th>Title</th>
              <th>Placement</th>
              <th>Priority</th>
              <th>Link</th>
            </tr>
          </thead>
          <tbody>
            {ads.map((a) => (
              <tr key={a.id}>
                <td>{a.title}</td>
                <td>{a.placement}</td>
                <td>{a.display_priority}</td>
                <td>{a.link_url ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </PageShell>
  );
}
