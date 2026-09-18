import { useEffect, useState, type FormEvent } from 'react';
import { adminApi, type ProductRow } from '../api/admin';
import { apiRequest } from '../api/client';
import { PageShell } from '../components/PageShell';

type ProductTypeRow = { id: string; name: string; slug: string };

export function ProductsPage() {
  const [rows, setRows] = useState<ProductRow[]>([]);
  const [types, setTypes] = useState<ProductTypeRow[]>([]);
  const [error, setError] = useState('');
  const [msg, setMsg] = useState('');
  const [loading, setLoading] = useState(true);
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [basePrice, setBasePrice] = useState('100');
  const [productTypeId, setProductTypeId] = useState('');
  const [surfaceProductId, setSurfaceProductId] = useState('');
  const [surfaceCode, setSurfaceCode] = useState('front');
  const [surfaceName, setSurfaceName] = useState('Front');
  const [realW, setRealW] = useState('30');
  const [realH, setRealH] = useState('35');

  async function load() {
    setLoading(true);
    setError('');
    try {
      const [p, t] = await Promise.all([
        adminApi.products(),
        apiRequest<ProductTypeRow[]>('/api/admin/catalog/product-types'),
      ]);
      setRows(p);
      setTypes(t);
      if (!productTypeId && t[0]) setProductTypeId(t[0].id);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
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
      await apiRequest('/api/admin/catalog/products', {
        method: 'POST',
        body: JSON.stringify({
          product_type_id: productTypeId,
          name,
          slug: slug || undefined,
          base_price: Number(basePrice),
          measurement_unit: 'Centimeter',
          status: 'Active',
        }),
      });
      setMsg('Product created.');
      setName('');
      setSlug('');
      await load();
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Create failed');
    }
  }

  async function deactivate(id: string) {
    if (!confirm('Deactivate this product? Prefer deactivate over delete for historical safety.')) return;
    setError('');
    try {
      await apiRequest(`/api/admin/catalog/products/${id}/deactivate`, { method: 'POST' });
      setMsg('Product deactivated.');
      await load();
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Deactivate failed');
    }
  }

  async function addSurface(e: FormEvent) {
    e.preventDefault();
    if (!surfaceProductId) return;
    setError('');
    setMsg('');
    try {
      await apiRequest(`/api/admin/catalog/products/${surfaceProductId}/surfaces`, {
        method: 'POST',
        body: JSON.stringify({
          code: surfaceCode,
          name: surfaceName,
          sort_order: 0,
          is_required: true,
          design_area: {
            norm_x: 0.25,
            norm_y: 0.2,
            norm_width: 0.5,
            norm_height: 0.45,
            real_width: Number(realW),
            real_height: Number(realH),
          },
        }),
      });
      setMsg('Surface + design area added.');
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Surface failed');
    }
  }

  return (
    <PageShell title="Products">
      <div className="card">
        <h3>Create product</h3>
        <form className="form-grid" onSubmit={onCreate}>
          <div>
            <label>Product type</label>
            <select value={productTypeId} onChange={(e) => setProductTypeId(e.target.value)} required>
              {types.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label>Name</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
          <div>
            <label>Slug</label>
            <input value={slug} onChange={(e) => setSlug(e.target.value)} />
          </div>
          <div>
            <label>Base price</label>
            <input type="number" step="0.01" value={basePrice} onChange={(e) => setBasePrice(e.target.value)} />
          </div>
          <button type="submit" className="btn" disabled={!name || !productTypeId}>
            Create
          </button>
        </form>
      </div>

      <div className="card" style={{ marginTop: '1rem' }}>
        <h3>Add surface + design area</h3>
        <p className="muted">Embroidery pricing uses real width × real height from the design area.</p>
        <form className="form-grid" onSubmit={addSurface}>
          <div>
            <label>Product</label>
            <select value={surfaceProductId} onChange={(e) => setSurfaceProductId(e.target.value)} required>
              <option value="">Select…</option>
              {rows.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label>Code</label>
            <input value={surfaceCode} onChange={(e) => setSurfaceCode(e.target.value)} required />
          </div>
          <div>
            <label>Name</label>
            <input value={surfaceName} onChange={(e) => setSurfaceName(e.target.value)} required />
          </div>
          <div>
            <label>Real width (cm)</label>
            <input type="number" step="0.01" value={realW} onChange={(e) => setRealW(e.target.value)} />
          </div>
          <div>
            <label>Real height (cm)</label>
            <input type="number" step="0.01" value={realH} onChange={(e) => setRealH(e.target.value)} />
          </div>
          <button type="submit" className="btn">
            Add surface
          </button>
        </form>
      </div>

      {msg ? <p className="ok">{msg}</p> : null}
      {error ? <p className="error">{error}</p> : null}
      {loading ? <p className="muted">Loading…</p> : null}
      {!loading ? (
        <div className="card" style={{ marginTop: '1rem' }}>
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Slug</th>
                <th>Type</th>
                <th>Base price</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {rows.map((p) => (
                <tr key={p.id}>
                  <td>{p.name}</td>
                  <td>{p.slug}</td>
                  <td>{p.type}</td>
                  <td>{p.base_price}</td>
                  <td>{p.status}</td>
                  <td>
                    <button type="button" className="btn btn-ghost btn-sm" onClick={() => deactivate(p.id)}>
                      Deactivate
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {rows.length === 0 ? <p className="muted">No products found.</p> : null}
        </div>
      ) : null}
    </PageShell>
  );
}
