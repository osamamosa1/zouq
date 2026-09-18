import { useEffect, useState } from 'react';
import { adminApi } from '../api/admin';
import { PageShell } from '../components/PageShell';

export function DashboardPage() {
  const [stats, setStats] = useState({
    products: '—',
    orders: '—',
    users: '—',
    pendingOrders: '—',
  });

  useEffect(() => {
    Promise.all([adminApi.products(), adminApi.orders(), adminApi.users()])
      .then(([products, orders, users]) => {
        setStats({
          products: String(products.length),
          orders: String(orders.length),
          users: String(users.length),
          pendingOrders: String(orders.filter((o) => o.status === 'Pending').length),
        });
      })
      .catch(() => {
        setStats({
          products: '?',
          orders: '?',
          users: '?',
          pendingOrders: '?',
        });
      });
  }, []);

  return (
    <PageShell title="Dashboard">
      <p className="muted">Overview of your zouq catalog and operations.</p>
      <div className="stats-grid" style={{ marginTop: '1.25rem' }}>
        <div className="stat">
          <div className="stat-value">{stats.products}</div>
          <div className="stat-label">Products</div>
        </div>
        <div className="stat">
          <div className="stat-value">{stats.orders}</div>
          <div className="stat-label">Orders</div>
        </div>
        <div className="stat">
          <div className="stat-value">{stats.pendingOrders}</div>
          <div className="stat-label">Pending orders</div>
        </div>
        <div className="stat">
          <div className="stat-value">{stats.users}</div>
          <div className="stat-label">Users</div>
        </div>
      </div>
      <div className="card" style={{ marginTop: '1.5rem' }}>
        <h3>Quick links</h3>
        <p className="muted" style={{ margin: 0 }}>
          Use the sidebar to manage fabrics, cuts, sizes, printing, design assets, orders, featured designs, ads, and user balances.
        </p>
      </div>
    </PageShell>
  );
}
