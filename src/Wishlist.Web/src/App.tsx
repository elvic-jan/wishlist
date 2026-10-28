import { BrowserRouter, Routes, Route, Navigate, useParams } from 'react-router-dom'
import { AuthProvider } from '@/context/AuthContext'
import { ThemeProvider } from '@/context/ThemeContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { AppShell } from '@/components/AppShell'
import { AuthLayout } from '@/components/AuthLayout'
import { Toaster } from '@/components/ui/toaster'
import { UpdateToaster } from '@/components/UpdateToaster'
import { TooltipProvider } from '@/components/ui/tooltip'
import { LoginPage } from '@/pages/LoginPage'
import { RegisterPage } from '@/pages/RegisterPage'
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage'
import { RecoverPage } from '@/pages/RecoverPage'
import { ConfirmEmailPage } from '@/pages/ConfirmEmailPage'
import { SecurityPage } from '@/pages/SecurityPage'
import { HomePage } from '@/pages/HomePage'
import { GroupDetailPage } from '@/pages/GroupDetailPage'
import { MyWishlistPage } from '@/pages/MyWishlistPage'
import { WishlistsPage } from '@/pages/WishlistsPage'
import { MemberWishlistPage } from '@/pages/MemberWishlistPage'
import { JoinGroupPage } from '@/pages/JoinGroupPage'
import { ImportPage } from '@/pages/ImportPage'

function LegacyGroupRedirect() {
  const { groupId } = useParams()
  return <Navigate to={`/circles/${groupId}`} replace />
}

export default function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <TooltipProvider delayDuration={150}>
          <BrowserRouter>
            <Routes>
              {/* Unauthenticated — minimal centered layout. */}
              <Route element={<AuthLayout />}>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                <Route path="/recover" element={<RecoverPage />} />
                {/* Links sent before recovery offered passkeys. */}
                <Route path="/reset-password" element={<RecoverPage />} />
                <Route path="/confirm-email" element={<ConfirmEmailPage />} />
                <Route path="/join/:code" element={<JoinGroupPage />} />
              </Route>

              {/* Authenticated — persistent app shell with top nav. */}
              <Route
                element={
                  <ProtectedRoute>
                    <AppShell />
                  </ProtectedRoute>
                }
              >
                <Route path="/" element={<HomePage />} />
                <Route path="/circles/:groupId" element={<GroupDetailPage />} />
                {/* Circles used to be called groups; keep old bookmarks working. */}
                <Route path="/groups/:groupId" element={<LegacyGroupRedirect />} />
                <Route path="/settings/security" element={<SecurityPage />} />
                <Route path="/import" element={<ImportPage />} />
                <Route path="/wishlists" element={<WishlistsPage />} />
                <Route path="/wishlist/me" element={<MyWishlistPage />} />
                <Route path="/wishlist/:userId" element={<MemberWishlistPage />} />
              </Route>

              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
            <Toaster />
            <UpdateToaster />
          </BrowserRouter>
        </TooltipProvider>
      </AuthProvider>
    </ThemeProvider>
  )
}
