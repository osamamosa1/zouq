import { useState, type FormEvent } from 'react';
import { adminApi } from '../api/admin';
import { PageShell } from '../components/PageShell';
import { ProductSelect } from '../components/ProductSelect';

export function SizesPage() {
  const [productId, setProductId] = useState('');
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [width, setWidth] = useState('');
  const [height, setHeight] = useState('');
  const [depth, setDepth] = useState('');
  const [unit, setUnit] = useState('Centimeter');
  const [priceAdj, setPriceAdj] = useState('0');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!productId) {
      setErr('Select a product.');
      return;
    }
    setErr('');
    setMsg('');
    setLoading(true);
    try {
      const res = await adminApi.addSize(productId, {
        code,
        name,
        width: Number(width),
        height: Number(height),
        depth: depth ? Number(depth) : undefined,
        unit,
        price_adjustment: Number(priceAdj),
      });
      setMsg(`Size added: ${res.code} (${res.id})`);
      setCode('');
      setName('');
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <PageShell title="Product sizes">
      <div className="card">
        <h3>Add size to product</h3>
        <form className="form-grid" onSubmit={onSubmit}>
          <div>
            <label>Product</label>
            <ProductSelect value={productId} onChange={setProductId} />
          </div>
          <div className="form-row-2">
            <div>
              <label>Code</label>
              <input value={code} onChange={(e) => setCode(e.target.value)} required placeholder="M" />
            </div>
            <div>
              <label>Name</label>
              <input value={name} onChange={(e) => setName(e.target.value)} required placeholder="Medium" />
            </div>
          </div>
          <div className="form-row-2">
            <div>
              <label>Width</label>
              <input type="number" step="0.01" value={width} onChange={(e) => setWidth(e.target.value)} required />
            </div>
            <div>
              <label>Height</label>
              <input type="number" step="0.01" value={height} onChange={(e) => setHeight(e.target.value)} required />
            </div>
          </div>
          <div className="form-row-2">
            <div>
              <label>Depth (optional)</label>
              <input type="number" step="0.01" value={depth} onChange={(e) => setDepth(e.target.value)} />
            </div>
            <div>
              <label>Unit</label>
              <select value={unit} onChange={(e) => setUnit(e.target.value)}>
                <option value="Centimeter">Centimeter</option>
                <option value="Inch">Inch</option>
              </select>
            </div>
          </div>
          <div>
            <label>Price adjustment</label>
            <input type="number" step="0.01" value={priceAdj} onChange={(e) => setPriceAdj(e.target.value)} />
          </div>
          <button type="submit" className="btn" disabled={loading}>
            Add size
          </button>
        </form>
        {msg ? <p className="success">{msg}</p> : null}
        {err ? <p className="error">{err}</p> : null}
      </div>
    </PageShell>
  );
}
