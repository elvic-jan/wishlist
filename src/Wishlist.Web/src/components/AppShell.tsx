import { Download, Gift, LogOut, ShieldCheck } from 'lucide-react'
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Suspense, useState } from 'react'
import { AccountBanners } from '@/components/AccountBanners'
import { InstallAppMenuItem, IosInstallHelp } from '@/components/InstallApp'
import { MessageToaster } from '@/components/MessageToaster'
import { ModeToggle } from '@/components/ModeToggle'
import { useAuth } from '@/context/AuthContext'
import { cn } from '@/lib/utils'

function initialsOf(name: string | undefined): string {
  if (!name) return '?'
  const parts = name.trim().split(/\s+/).slice(0, 2)
  return parts.map(p => p[0]?.toUpperCase() ?? '').join('') || '?'
}

function navLinkClasses(isActive: boolean): string {
  return cn(
    'rounded-md px-3 py-1.5 text-sm font-medium transition-colors',
    isActive
      ? 'bg-primary/10 text-primary'
      : 'text-muted-foreground hover:bg-accent hover:text-foreground',
  )
}

export function AppShell() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const inWishlists = pathname === '/wishlists' || pathname.startsWith('/wishlist/')
  const [showIosInstallHelp, setShowIosInstallHelp] = useState(false)

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <div className="min-h-svh bg-background">
      <header className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="mx-auto flex h-14 max-w-3xl items-center justify-between gap-4 px-4 sm:px-6">
          <div className="flex items-center gap-4 sm:gap-6">
            <Link
              to="/"
              className="flex items-center gap-2 text-sm font-semibold tracking-tight text-foreground hover:text-foreground"
            >
              <Gift className="h-5 w-5 text-primary" aria-hidden />
              <span>Kadoozle</span>
            </Link>
            <nav className="flex items-center gap-1">
              <NavLink
                to="/"
                end
                className={({ isActive }) => navLinkClasses(isActive)}
              >
                Circles
              </NavLink>
              {/* Highlighted on the overview and on every wishlist, mine or someone else's. */}
              <Link
                to="/wishlists"
                aria-current={inWishlists ? 'page' : undefined}
                className={navLinkClasses(inWishlists)}
              >
                Wishlists
              </Link>
            </nav>
          </div>

          <div className="flex items-center gap-1">
            <ModeToggle />
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className="rounded-full"
                  aria-label="Account menu"
                >
                  <Avatar className="h-8 w-8">
                    <AvatarFallback className="bg-primary/15 text-primary text-xs font-semibold">
                      {initialsOf(user?.displayName)}
                    </AvatarFallback>
                  </Avatar>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <DropdownMenuLabel className="flex flex-col gap-0.5">
                  <span className="text-sm font-medium leading-none">
                    {user?.displayName ?? 'Signed in'}
                  </span>
                  {user?.email && (
                    <span className="text-xs font-normal text-muted-foreground">
                      {user.email}
                    </span>
                  )}
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <InstallAppMenuItem onShowIosHelp={() => setShowIosInstallHelp(true)} />
                <DropdownMenuItem onClick={() => navigate('/import')}>
                  <Download className="mr-2 h-4 w-4" />
                  Import from old site
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => navigate('/settings/security')}>
                  <ShieldCheck className="mr-2 h-4 w-4" />
                  Sign-in &amp; security
                </DropdownMenuItem>
                <DropdownMenuItem onClick={handleLogout}>
                  <LogOut className="mr-2 h-4 w-4" />
                  Sign out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-3xl px-4 py-8 sm:px-6 md:py-12">
        <AccountBanners />
        <Suspense fallback={null}>
          <Outlet />
        </Suspense>
        <IosInstallHelp open={showIosInstallHelp} onOpenChange={setShowIosInstallHelp} />
      </main>
      <MessageToaster hiddenOn={['/import']} />
    </div>
  )
}
