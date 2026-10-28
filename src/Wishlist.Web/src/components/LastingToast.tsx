import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { X } from 'lucide-react'

/**
 * The card for toasts that stay until handled (messages, the new-version
 * notice): icon, title, text, a close button and the given buttons.
 */
export function LastingToast({
  icon: Icon,
  title,
  text,
  onClose,
  children,
}: {
  icon: LucideIcon
  title: string
  text: string
  onClose: () => void
  children: ReactNode
}) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed inset-x-0 bottom-0 z-50 flex justify-center p-4 pb-[max(1rem,env(safe-area-inset-bottom))] sm:inset-x-auto sm:right-0 sm:justify-end"
    >
      <div className="w-full max-w-sm rounded-lg border bg-card p-4 text-card-foreground shadow-lg">
        <div className="flex items-start gap-3">
          <Icon className="mt-0.5 h-4 w-4 shrink-0 text-primary" aria-hidden />
          <div className="min-w-0 flex-1 space-y-1">
            <p className="text-sm font-semibold">{title}</p>
            <p className="text-sm text-muted-foreground">{text}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="-m-1 rounded-sm p-1 text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="Close"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
        <div className="mt-3 flex flex-wrap justify-end gap-2">{children}</div>
      </div>
    </div>
  )
}
