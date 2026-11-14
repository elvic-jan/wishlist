import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import client, { refreshAccessToken, setAccessToken } from '../api/client';
import { getLanguage, parseLanguage, setLanguage } from '@/lib/i18n';

interface User {
  id: string;
  email: string;
  displayName: string;
  /** Site admin: shows the Admin menu item. The server checks again on every admin request. */
  isAdmin?: boolean;
  /** The language saved on the account ("en", "nl"); null when never chosen. */
  language?: string | null;
}

interface AuthContextType {
  user: User | null;
  /** True until the refresh-token cookie has been tried on page load. */
  restoring: boolean;
  login: (email: string, password: string) => Promise<void>;
  /** Sign-up always needs an invite; resolves to the group it joined. */
  register: (
    email: string,
    displayName: string,
    password: string,
    inviteCode: string,
  ) => Promise<string>;
  logout: () => Promise<void>;
  setUser: (user: User | null) => void;
  /** Takes over a session the server started another way (passkey sign-in or sign-up, recovery). */
  startSession: (result: { accessToken: string; user: User }) => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [restoring, setRestoring] = useState(true);

  // Someone signed in: their saved language wins on this device. An account
  // without one gets the language this device is showing, so their emails match.
  const adopt = useCallback((signedIn: User) => {
    setUser(signedIn);
    const saved = parseLanguage(signedIn.language);
    if (saved) {
      setLanguage(saved);
      return;
    }
    client
      .put<User>('/auth/me/language', { language: getLanguage() })
      .then(({ data }) => setUser(current => (current?.id === data.id ? data : current)))
      .catch(() => undefined);
  }, []);

  // The access token lives only in memory, so a reload starts signed out.
  // The HttpOnly refresh cookie (if any) gets us a new one.
  useEffect(() => {
    let cancelled = false;
    refreshAccessToken()
      .then(() => client.get<User>('/auth/me'))
      .then(({ data }) => {
        if (!cancelled) adopt(data);
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
  }, [adopt]);

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await client.post('/auth/login', { email, password });
    setAccessToken(data.accessToken);
    adopt(data.user);
  }, [adopt]);

  const register = useCallback(
    async (email: string, displayName: string, password: string, inviteCode: string) => {
      const { data } = await client.post('/auth/register', { email, displayName, password, inviteCode });
      setAccessToken(data.accessToken);
      adopt(data.user);
      return data.joinedGroupId as string;
    },
    [adopt],
  );

  const logout = useCallback(async () => {
    await client.post('/auth/logout');
    setAccessToken(null);
    setUser(null);
  }, []);

  const startSession = useCallback((result: { accessToken: string; user: User }) => {
    setAccessToken(result.accessToken);
    adopt(result.user);
  }, [adopt]);

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
