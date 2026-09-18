import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

const nav = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/product-types', label: 'Product types' },
  { to: '/products', label: 'Products' },
  { to: '/fabrics', label: 'Fabrics' },
  { to: '/cuts', label: 'Cuts' },
  { to: '/sizes', label: 'Sizes' },
  { to: '/printing', label: 'Printing & embroidery' },
  { to: '/assets', label: 'Design assets' },
  { to: '/orders', label: 'Orders' },
  { to: '/finance', label: 'Finance' },
  { to: '/designs', label: 'Designs' },
  { to: '/featured', label: 'Featured designs' },
  { to: '/ads', label: 'Ads / banners' },
  { to: '/users', label: 'Users' },
];

export function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-brand">zouq</div>
        <nav>
          {nav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => (isActive ? 'active' : undefined)}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          <p className="muted" style={{ fontSize: '0.8rem', margin: '0 0.75rem 0.5rem' }}>
            {user?.email}
          </p>
          <button type="button" className="btn btn-ghost btn-sm" style={{ width: '100%' }} onClick={logout}>
            Log out
          </button>
        </div>
      </aside>
      <div className="main">
        <Outlet />
      </div>
    </div>
  );
}
