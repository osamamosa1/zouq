import { useState, type FormEvent } from 'react';
import { adminApi } from '../api/admin';
import { PageShell } from '../components/PageShell';
import { ProductSelect } from '../components/ProductSelect';

export function FabricsPage() {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [priceAdj, setPriceAdj] = useState('0');
  const [productId, setProductId] = useState('');
  const [lastFabricId, setLastFabricId] = useState('');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);

  async function onCreate(e: FormEvent) {
    e.preventDefault();
    setErr('');
    setMsg('');
    setLoading(true);
    try {
      const res = await adminApi.createFabric({
        name,
        description: description || undefined,
        image_url: imageUrl || undefined,
        price_adjustment: Number(priceAdj),
      });
      setLastFabricId(res.id);
      setMsg(`Fabric created: ${res.name} (${res.id})`);
      if (productId) {
        await adminApi.linkFabric(productId, res.id);
        setMsg((m) => `${m} — linked to product.`);
      }
      setName('');
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  async function linkExisting(e: FormEvent) {
    e.preventDefault();
    if (!productId || !lastFabricId) {
      setErr('Select a product and create a fabric first, or paste fabric ID below.');
      return;
    }
    setErr('');
    setLoading(true);
    try {
      await adminApi.linkFabric(productId, lastFabricId);
      setMsg('Linked fabric to product.');
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : 'Link failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <PageShell title="Fabrics">
      <div className="card">
        <h3>Create fabric</h3>
        <form className="form-grid" onSubmit={onCreate}>
          <div>
            <label>Name</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required />
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
            <label>Price adjustment</label>
            <input type="number" step="0.01" value={priceAdj} onChange={(e) => setPriceAdj(e.target.value)} />
          </div>
          <div>
            <label>Link to product (optional)</label>
            <ProductSelect value={productId} onChange={setProductId} />
          </div>
          <button type="submit" className="btn" disabled={loading}>
            Create fabric
          </button>
        </form>
        {msg ? <p className="success">{msg}</p> : null}
        {err ? <p className="error">{err}</p> : null}
      </div>
      {lastFabricId ? (
        <div className="card">
          <h3>Last created fabric ID</h3>
          <code>{lastFabricId}</code>
          <form onSubmit={linkExisting} style={{ marginTop: '1rem' }}>
            <label>Re-link to product</label>
            <ProductSelect value={productId} onChange={setProductId} />
            <button type="submit" className="btn btn-sm" style={{ marginTop: '0.5rem' }} disabled={loading}>
              Link
            </button>
          </form>
        </div>
      ) : null}
    </PageShell>
  );
}
