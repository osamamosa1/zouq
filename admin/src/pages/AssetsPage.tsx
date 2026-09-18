import { useState, type FormEvent } from 'react';
import { adminApi } from '../api/admin';
import { PageShell } from '../components/PageShell';

export function AssetsPage() {
  const [name, setName] = useState('');
  const [fileUrl, setFileUrl] = useState('');
  const [thumbUrl, setThumbUrl] = useState('');
  const [tags, setTags] = useState('');
  const [productIds, setProductIds] = useState('');
  const [surfaces, setSurfaces] = useState('');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setErr('');
    setMsg('');
    setLoading(true);
    try {
      const res = await adminApi.createDesignAsset({
        name,
        file_url: fileUrl,
        thumbnail_url: thumbUrl || undefined,
        tags: tags
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean),
        compatible_product_ids: productIds
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean),
        compatible_surface_codes: surfaces
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean),
      });
      setMsg(`Asset created: ${res.name} (${res.id})`);
      setName('');
      setFileUrl('');
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <PageShell title="Design assets">
      <p className="muted">
        Register a design asset by URL (upload files via the customer API at POST /api/uploads if needed).
      </p>
      <div className="card">
        <h3>Create design asset</h3>
        <form className="form-grid" onSubmit={onSubmit}>
          <div>
            <label>Name</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
          <div>
            <label>File URL</label>
            <input value={fileUrl} onChange={(e) => setFileUrl(e.target.value)} required />
          </div>
          <div>
            <label>Thumbnail URL</label>
            <input value={thumbUrl} onChange={(e) => setThumbUrl(e.target.value)} />
          </div>
          <div>
            <label>Tags (comma-separated)</label>
            <input value={tags} onChange={(e) => setTags(e.target.value)} />
          </div>
          <div>
            <label>Compatible product IDs (comma-separated GUIDs)</label>
            <input value={productIds} onChange={(e) => setProductIds(e.target.value)} />
          </div>
          <div>
            <label>Compatible surface codes (comma-separated)</label>
            <input value={surfaces} onChange={(e) => setSurfaces(e.target.value)} />
          </div>
          <button type="submit" className="btn" disabled={loading}>
            Create asset
          </button>
        </form>
        {msg ? <p className="success">{msg}</p> : null}
        {err ? <p className="error">{err}</p> : null}
      </div>
    </PageShell>
  );
}
