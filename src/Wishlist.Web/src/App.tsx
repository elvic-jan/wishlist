import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { HomePage } from './pages/HomePage';
import { GroupDetailPage } from './pages/GroupDetailPage';
import { MyWishlistPage } from './pages/MyWishlistPage';
import { MemberWishlistPage } from './pages/MemberWishlistPage';
import { JoinGroupPage } from './pages/JoinGroupPage';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/join/:code" element={<JoinGroupPage />} />
          <Route path="/" element={<ProtectedRoute><HomePage /></ProtectedRoute>} />
          <Route path="/groups/:groupId" element={<ProtectedRoute><GroupDetailPage /></ProtectedRoute>} />
          <Route path="/wishlist/me" element={<ProtectedRoute><MyWishlistPage /></ProtectedRoute>} />
          <Route path="/wishlist/:userId" element={<ProtectedRoute><MemberWishlistPage /></ProtectedRoute>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
