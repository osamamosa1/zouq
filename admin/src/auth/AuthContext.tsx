import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import {
  getToken,
  login as apiLogin,
  setToken,
  type AuthTokenResponse,
} from '../api/client';

type AuthState = {
  token: string | null;
  user: AuthTokenResponse['user'] | null;
};

type AuthContextValue = AuthState & {
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => {
    const token = getToken();
    const raw = localStorage.getItem('zouq_admin_user');
    let user: AuthTokenResponse['user'] | null = null;
    if (raw) {
      try {
        user = JSON.parse(raw) as AuthTokenResponse['user'];
      } catch {
        user = null;
      }
    }
    return { token, user };
  });

  const login = useCallback(async (email: string, password: string) => {
    const data = await apiLogin(email, password);
    if (data.user.role !== 'Admin') {
      throw new Error('This account is not an admin.');
    }
    setToken(data.access_token);
    localStorage.setItem('zouq_admin_user', JSON.stringify(data.user));
    setState({ token: data.access_token, user: data.user });
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    localStorage.removeItem('zouq_admin_user');
    setState({ token: null, user: null });
  }, []);

  const value = useMemo(
    () => ({ ...state, login, logout }),
    [state, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
