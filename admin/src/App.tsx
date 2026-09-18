import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { Layout } from './components/Layout';
import { RequireAuth } from './components/RequireAuth';
import { AdsPage } from './pages/AdsPage';
import { AssetsPage } from './pages/AssetsPage';
import { CutsPage } from './pages/CutsPage';
import { DashboardPage } from './pages/DashboardPage';
import { DesignsAdminPage, FinancePage } from './pages/FinancePage';
import { FabricsPage } from './pages/FabricsPage';
import { FeaturedPage } from './pages/FeaturedPage';
import { LoginPage } from './pages/LoginPage';
import { OrdersPage } from './pages/OrdersPage';
import { PrintingPage } from './pages/PrintingPage';
import { ProductsPage } from './pages/ProductsPage';
import { ProductTypesPage } from './pages/ProductTypesPage';
import { SizesPage } from './pages/SizesPage';
import { UsersPage } from './pages/UsersPage';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            element={
              <RequireAuth>
                <Layout />
              </RequireAuth>
            }
          >
            <Route index element={<DashboardPage />} />
            <Route path="product-types" element={<ProductTypesPage />} />
            <Route path="products" element={<ProductsPage />} />
            <Route path="fabrics" element={<FabricsPage />} />
            <Route path="cuts" element={<CutsPage />} />
            <Route path="sizes" element={<SizesPage />} />
            <Route path="printing" element={<PrintingPage />} />
            <Route path="assets" element={<AssetsPage />} />
            <Route path="orders" element={<OrdersPage />} />
            <Route path="finance" element={<FinancePage />} />
            <Route path="designs" element={<DesignsAdminPage />} />
            <Route path="featured" element={<FeaturedPage />} />
            <Route path="ads" element={<AdsPage />} />
            <Route path="users" element={<UsersPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
