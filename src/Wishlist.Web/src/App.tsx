import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from '@/context/AuthContext'
import { ThemeProvider } from '@/context/ThemeContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { AppShell } from '@/components/AppShell'
import { AuthLayout } from '@/components/AuthLayout'
import { Toaster } from '@/components/ui/toaster'
import { TooltipProvider } from '@/components/ui/tooltip'
import { LoginPage } from '@/pages/LoginPage'
import { RegisterPage } from '@/pages/RegisterPage'
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage'
import { ResetPasswordPage } from '@/pages/ResetPasswordPage'
import { HomePage } from '@/pages/HomePage'
import { GroupDetailPage } from '@/pages/GroupDetailPage'
import { MyWishlistPage } from '@/pages/MyWishlistPage'
import { MemberWishlistPage } from '@/pages/MemberWishlistPage'
import { JoinGroupPage } from '@/pages/JoinGroupPage'

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
                <Route path="/reset-password" element={<ResetPasswordPage />} />
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
                <Route path="/groups/:groupId" element={<GroupDetailPage />} />
                <Route path="/wishlist/me" element={<MyWishlistPage />} />
                <Route path="/wishlist/:userId" element={<MemberWishlistPage />} />
              </Route>

              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
            <Toaster />
          </BrowserRouter>
        </TooltipProvider>
      </AuthProvider>
    </ThemeProvider>
  )
}
