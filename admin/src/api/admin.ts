import { apiRequest } from './client';

export type ProductRow = {
  id: string;
  name: string;
  slug: string;
  base_price: number;
  status: string;
  type: string;
};

export type OrderRow = {
  id: string;
  order_number: string;
  status: string;
  total: number;
  currency: string;
  created_at_utc: string;
  buyer: string;
  buyer_email: string;
};

export type UserRow = {
  id: string;
  name: string;
  email: string;
  role: string;
  balance: number;
  is_active: boolean;
  created_at_utc: string;
};

export type FeedItem = {
  design_id: string;
  title: string;
  preview_image_url?: string | null;
  owner_name: string;
  is_featured: boolean;
  featured_priority: number;
  tags: string[];
};

export type AdRow = {
  id: string;
  title: string;
  description?: string | null;
  image_url?: string | null;
  link_url?: string | null;
  action_type?: string | null;
  placement: string;
  display_priority: number;
};

export const adminApi = {
  products: () => apiRequest<ProductRow[]>('/api/admin/products'),
  orders: () => apiRequest<OrderRow[]>('/api/admin/orders'),
  setOrderStatus: (id: string, status: string) =>
    apiRequest<{ id: string; status: string }>(`/api/admin/orders/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  createFabric: (body: {
    name: string;
    description?: string;
    image_url?: string;
    price_adjustment: number;
  }) =>
    apiRequest<{ id: string; name: string }>('/api/admin/fabrics', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  linkFabric: (productId: string, fabricId: string) =>
    apiRequest(`/api/admin/products/${productId}/fabrics/${fabricId}`, { method: 'POST' }),
  createCut: (body: {
    name: string;
    description?: string;
    image_url?: string;
    price_adjustment: number;
  }) =>
    apiRequest<{ id: string; name: string }>('/api/admin/cuts', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  linkCut: (productId: string, cutId: string) =>
    apiRequest(`/api/admin/products/${productId}/cuts/${cutId}`, { method: 'POST' }),
  addSize: (
    productId: string,
    body: {
      code: string;
      name: string;
      width: number;
      height: number;
      depth?: number;
      unit?: string;
      price_adjustment: number;
    },
  ) =>
    apiRequest<{ id: string; code: string }>(`/api/admin/products/${productId}/sizes`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  setEmbroidery: (
    productId: string,
    body: { price_per_square_unit: number; minimum_charge?: number },
  ) =>
    apiRequest(`/api/admin/products/${productId}/embroidery-pricing`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }),
  addPrinting: (
    productId: string,
    body: {
      name: string;
      code: string;
      description?: string;
      price: number;
      included_surface_codes?: string[];
    },
  ) =>
    apiRequest<{ id: string; name: string; price: number }>(
      `/api/admin/products/${productId}/printing-options`,
      {
        method: 'POST',
        body: JSON.stringify(body),
      },
    ),
  createDesignAsset: (body: {
    name: string;
    file_url: string;
    thumbnail_url?: string;
    category_id?: string;
    tags?: string[];
    compatible_product_ids?: string[];
    compatible_surface_codes?: string[];
  }) =>
    apiRequest<{ id: string; name: string }>('/api/admin/design-assets', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  featureDesign: (id: string, body: { is_featured: boolean; priority: number }) =>
    apiRequest(`/api/admin/designs/${id}/feature`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  createAd: (body: {
    title: string;
    description?: string;
    image_url?: string;
    link_url?: string;
    action_type?: string;
    placement?: string;
    start_at_utc?: string;
    end_at_utc?: string;
    display_priority: number;
  }) =>
    apiRequest<{ id: string; title: string }>('/api/admin/ads', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  users: () => apiRequest<UserRow[]>('/api/admin/users'),
  adjustBalance: (id: string, body: { amount: number; reason?: string }) =>
    apiRequest<{ id: string; balance: number }>(`/api/admin/users/${id}/balance`, {
      method: 'PATCH',
      body: JSON.stringify(body),
    }),
  ledger: () => apiRequest<any[]>('/api/admin/ledger'),
  creatorRewards: () => apiRequest<any[]>('/api/admin/creator-rewards'),
  designs: (status?: string) =>
    apiRequest<any[]>(`/api/admin/designs${status ? `?status=${encodeURIComponent(status)}` : ''}`),
  adminPublishDesign: (id: string) =>
    apiRequest(`/api/admin/designs/${id}/publish`, { method: 'POST' }),
  getCommission: () =>
    apiRequest<{ designer_commission_percent: number }>('/api/admin/catalog/commission'),
  setCommission: (designer_commission_percent: number) =>
    apiRequest('/api/admin/catalog/commission', {
      method: 'PUT',
      body: JSON.stringify({ designer_commission_percent }),
    }),
  orderDetail: (id: string) => apiRequest<any>(`/api/admin/orders/${id}`),
  feedForYou: () => apiRequest<FeedItem[]>('/api/feed/for-you', { auth: false }),
  activeAds: () => apiRequest<AdRow[]>('/api/feed/ads', { auth: false }),
};
