import { useState, type FormEvent } from 'react';
import { adminApi } from '../api/admin';
import { PageShell } from '../components/PageShell';
import { ProductSelect } from '../components/ProductSelect';

export function CutsPage() {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [priceAdj, setPriceAdj] = useState('0');
  const [productId, setProductId] = useState('');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setErr('');
    setMsg('');
    setLoading(true);
    try {
      const res = await adminApi.createCut({
        name,
        description: description || undefined,
        image_url: imageUrl || undefined,
        price_adjustment: Number(priceAdj),
      });
      setMsg(`Cut created: ${res.name} (${res.id})`);
      if (productId) {
        await adminApi.linkCut(productId, res.id);
        setMsg((m) => `${m} — linked to product.`);
      }
      setName('');
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <PageShell title="Cut styles">
      <div className="card">
        <h3>Create cut style</h3>
        <form className="form-grid" onSubmit={onSubmit}>
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
            Create cut
          </button>
        </form>
        {msg ? <p className="success">{msg}</p> : null}
        {err ? <p className="error">{err}</p> : null}
      </div>
    </PageShell>
  );
}
