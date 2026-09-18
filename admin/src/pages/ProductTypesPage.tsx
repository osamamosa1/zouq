import { useEffect, useState, type FormEvent } from 'react';
import { apiRequest } from '../api/client';
import { PageShell } from '../components/PageShell';

type ProductTypeRow = {
  id: string;
  name: string;
  slug: string;
  status: string;
  description?: string | null;
};

export function ProductTypesPage() {
  const [rows, setRows] = useState<ProductTypeRow[]>([]);
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');
  const [msg, setMsg] = useState('');
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const data = await apiRequest<ProductTypeRow[]>('/api/admin/catalog/product-types');
      setRows(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setMsg('');
    try {
      await apiRequest('/api/admin/catalog/product-types', {
        method: 'POST',
        body: JSON.stringify({
          name,
          slug: slug || undefined,
          description: description || undefined,
        }),
      });
      setMsg('Product type created.');
      setName('');
      setSlug('');
      setDescription('');
      await load();
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Create failed');
    }
  }

  return (
    <PageShell title="Product types">
      <div className="card">
        <h3>Create product type</h3>
        <form className="form-grid" onSubmit={onCreate}>
          <div>
            <label>Name</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
          <div>
            <label>Slug</label>
            <input value={slug} onChange={(e) => setSlug(e.target.value)} placeholder="optional" />
          </div>
          <div>
            <label>Description</label>
            <input value={description} onChange={(e) => setDescription(e.target.value)} />
          </div>
          <button type="submit" className="btn" disabled={!name}>
            Create
          </button>
        </form>
        {msg ? <p className="ok">{msg}</p> : null}
        {error ? <p className="error">{error}</p> : null}
      </div>

      <div className="card" style={{ marginTop: '1rem' }}>
        <h3>All types</h3>
        {loading ? <p className="muted">Loading…</p> : null}
        {!loading ? (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Slug</th>
                <th>Status</th>
                <th>ID</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id}>
                  <td>{r.name}</td>
                  <td>{r.slug}</td>
                  <td>{r.status}</td>
                  <td>
                    <code style={{ fontSize: '0.75rem' }}>{r.id}</code>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
        {!loading && rows.length === 0 ? <p className="muted">No product types.</p> : null}
      </div>
    </PageShell>
  );
}
