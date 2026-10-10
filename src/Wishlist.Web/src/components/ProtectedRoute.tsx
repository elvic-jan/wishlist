import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { ReactNode } from 'react';

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user, restoring } = useAuth();
  if (restoring) return null;
  if (!user) return <Navigate to="/login" replace />;
  return <>{children}</>;
}
