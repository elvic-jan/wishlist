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
  /** Resolves to the group joined via the invite, or null for a codeless (bootstrap) sign-up. */
  register: (
    email: string,
    displayName: string,
    password: string,
    inviteCode?: string,
  ) => Promise<string | null>;
  logout: () => Promise<void>;
  setUser: (user: User | null) => void;
  /** Takes over a session the server started another way (passkey sign-in or sign-up, recovery). */
  startSession: (result: { accessToken: string; user: User }) => void;
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

  const register = useCallback(
    async (email: string, displayName: string, password: string, inviteCode?: string) => {
      const { data } = await client.post('/auth/register', { email, displayName, password, inviteCode });
      setAccessToken(data.accessToken);
      setUser(data.user);
      return (data.joinedGroupId as string | null | undefined) ?? null;
    },
    [],
  );

  const logout = useCallback(async () => {
    await client.post('/auth/logout');
    setAccessToken(null);
    setUser(null);
  }, []);

  const startSession = useCallback((result: { accessToken: string; user: User }) => {
    setAccessToken(result.accessToken);
    setUser(result.user);
  }, []);

  return (
    <AuthContext.Provider value={{ user, restoring, login, register, logout, setUser, startSession }}>
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
