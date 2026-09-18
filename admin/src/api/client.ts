export type ApiResponse<T> = {
  status: string;
  message?: string | null;
  data?: T;
};

const STORAGE_API = 'zouq_admin_api_base';
const STORAGE_TOKEN = 'zouq_admin_token';

export function getApiBase(): string {
  return (
    localStorage.getItem(STORAGE_API) ??
    import.meta.env.VITE_API_BASE ??
    'http://localhost:5280'
  ).replace(/\/$/, '');
}

export function setApiBase(base: string): void {
  localStorage.setItem(STORAGE_API, base.replace(/\/$/, ''));
}

export function getToken(): string | null {
  return localStorage.getItem(STORAGE_TOKEN);
}

export function setToken(token: string | null): void {
  if (token) localStorage.setItem(STORAGE_TOKEN, token);
  else localStorage.removeItem(STORAGE_TOKEN);
}

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit & { auth?: boolean } = {},
): Promise<T> {
  const { auth = true, ...init } = options;
  const headers = new Headers(init.headers);
  if (!headers.has('Content-Type') && init.body && !(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }
  if (auth) {
    const token = getToken();
    if (token) headers.set('Authorization', `Bearer ${token}`);
  }

  const res = await fetch(`${getApiBase()}${path}`, { ...init, headers });
  let json: ApiResponse<T> | null = null;
  try {
    json = (await res.json()) as ApiResponse<T>;
  } catch {
    if (!res.ok) throw new ApiError(res.statusText || 'Request failed', res.status);
    throw new ApiError('Invalid JSON response', res.status);
  }

  if (!res.ok || json.status === 'error') {
    throw new ApiError(json.message ?? res.statusText ?? 'Request failed', res.status);
  }

  return json.data as T;
}

export type AuthTokenResponse = {
  access_token: string;
  refresh_token: string;
  expires_in: number;
  user: {
    id: string;
    name: string;
    email: string;
    role: string;
    balance: number;
  };
};

export async function login(email: string, password: string): Promise<AuthTokenResponse> {
  return apiRequest<AuthTokenResponse>('/api/auth/login', {
    auth: false,
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}
