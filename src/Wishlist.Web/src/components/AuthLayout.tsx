import { Gift } from 'lucide-react'
import { Link, Outlet } from 'react-router-dom'

export function AuthLayout() {
  return (
    <div className="min-h-svh bg-background">
      <header className="mx-auto flex h-14 max-w-md items-center justify-center px-4">
        <Link
          to="/login"
          className="flex items-center gap-2 text-sm font-semibold tracking-tight text-foreground"
        >
          <Gift className="h-5 w-5 text-primary" aria-hidden />
          <span>GiftCircles</span>
        </Link>
      </header>
      <main className="flex min-h-[calc(100svh-3.5rem)] flex-col items-center px-4 py-8 sm:py-12">
        <div className="w-full max-w-sm">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
