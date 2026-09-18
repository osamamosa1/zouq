import { useState, type FormEvent } from 'react';
import { adminApi } from '../api/admin';
import { PageShell } from '../components/PageShell';
import { ProductSelect } from '../components/ProductSelect';

export function PrintingPage() {
  const [productId, setProductId] = useState('');
  const [printName, setPrintName] = useState('');
  const [printCode, setPrintCode] = useState('');
  const [printDesc, setPrintDesc] = useState('');
  const [printPrice, setPrintPrice] = useState('');
  const [surfaces, setSurfaces] = useState('');
  const [embPrice, setEmbPrice] = useState('');
  const [embMin, setEmbMin] = useState('');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);

  async function onPrinting(e: FormEvent) {
    e.preventDefault();
    if (!productId) {
      setErr('Select a product.');
      return;
    }
    setErr('');
    setMsg('');
    setLoading(true);
    try {
      const codes = surfaces
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean);
      const res = await adminApi.addPrinting(productId, {
        name: printName,
        code: printCode,
        description: printDesc || undefined,
        price: Number(printPrice),
        included_surface_codes: codes.length ? codes : undefined,
      });
      setMsg(`Printing option added: ${res.name}`);
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  async function onEmbroidery(e: FormEvent) {
    e.preventDefault();
    if (!productId) {
      setErr('Select a product.');
      return;
    }
    setErr('');
    setMsg('');
    setLoading(true);
    try {
      await adminApi.setEmbroidery(productId, {
        price_per_square_unit: Number(embPrice),
        minimum_charge: embMin ? Number(embMin) : undefined,
      });
      setMsg('Embroidery pricing updated.');
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <PageShell title="Printing & embroidery">
      <div className="card">
        <h3>Product</h3>
        <ProductSelect value={productId} onChange={setProductId} />
      </div>
      <div className="card">
        <h3>Add printing option</h3>
        <form className="form-grid" onSubmit={onPrinting}>
          <div>
            <label>Name</label>
            <input value={printName} onChange={(e) => setPrintName(e.target.value)} required />
          </div>
          <div>
            <label>Code</label>
            <input value={printCode} onChange={(e) => setPrintCode(e.target.value)} required />
          </div>
          <div>
            <label>Description</label>
            <input value={printDesc} onChange={(e) => setPrintDesc(e.target.value)} />
          </div>
          <div>
            <label>Price</label>
            <input type="number" step="0.01" value={printPrice} onChange={(e) => setPrintPrice(e.target.value)} required />
          </div>
          <div>
            <label>Included surface codes (comma-separated)</label>
            <input value={surfaces} onChange={(e) => setSurfaces(e.target.value)} placeholder="front, back" />
          </div>
          <button type="submit" className="btn" disabled={loading}>
            Add printing option
          </button>
        </form>
      </div>
      <div className="card">
        <h3>Embroidery pricing</h3>
        <form className="form-grid" onSubmit={onEmbroidery}>
          <div>
            <label>Price per square unit</label>
            <input type="number" step="0.01" value={embPrice} onChange={(e) => setEmbPrice(e.target.value)} required />
          </div>
          <div>
            <label>Minimum charge (optional)</label>
            <input type="number" step="0.01" value={embMin} onChange={(e) => setEmbMin(e.target.value)} />
          </div>
          <button type="submit" className="btn" disabled={loading}>
            Save embroidery pricing
          </button>
        </form>
      </div>
      {msg ? <p className="success">{msg}</p> : null}
      {err ? <p className="error">{err}</p> : null}
    </PageShell>
  );
}
