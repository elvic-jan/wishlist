import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import client, { refreshAccessToken, setAccessToken } from '../api/client';

interface User {
  id: string;
  email: string;
  displayName: string;
}

interface AuthContextType {
  user: User | null;
  /** True until the refresh-token cookie has been tried on page load. */
  restoring: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, displayName: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  setUser: (user: User | null) => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [restoring, setRestoring] = useState(true);

  // The access token lives only in memory, so a reload starts signed out.
  // The HttpOnly refresh cookie (if any) gets us a new one.
  useEffect(() => {
    let cancelled = false;
    refreshAccessToken()
      .then(() => client.get<User>('/auth/me'))
      .then(({ data }) => {
        if (!cancelled) setUser(data);
      })
      .catch(() => {
        setAccessToken(null);
      })
      .finally(() => {
        if (!cancelled) setRestoring(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await client.post('/auth/login', { email, password });
    setAccessToken(data.accessToken);
    setUser(data.user);
  }, []);

  const register = useCallback(async (email: string, displayName: string, password: string) => {
    const { data } = await client.post('/auth/register', { email, displayName, password });
    setAccessToken(data.accessToken);
    setUser(data.user);
  }, []);

  const logout = useCallback(async () => {
    await client.post('/auth/logout');
    setAccessToken(null);
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, restoring, login, register, logout, setUser }}>
      {children}
    </AuthContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components -- hook lives alongside provider; HMR caveat is acceptable.
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
