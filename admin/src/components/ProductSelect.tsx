import { useEffect, useState } from 'react';
import { adminApi, type ProductRow } from '../api/admin';

export function ProductSelect({
  value,
  onChange,
}: {
  value: string;
  onChange: (id: string) => void;
}) {
  const [products, setProducts] = useState<ProductRow[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    adminApi
      .products()
      .then(setProducts)
      .catch((e: Error) => setError(e.message));
  }, []);

  if (error) return <p className="error">{error}</p>;

  return (
    <select value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">Select product…</option>
      {products.map((p) => (
        <option key={p.id} value={p.id}>
          {p.name} ({p.slug})
        </option>
      ))}
    </select>
  );
}
