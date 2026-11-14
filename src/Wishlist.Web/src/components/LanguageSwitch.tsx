import { Languages } from 'lucide-react'
import client from '@/api/client'
import {
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
} from '@/components/ui/dropdown-menu'
import { useAuth } from '@/context/AuthContext'
import { LANGUAGES, parseLanguage, setLanguage, useLanguage, useT, type Language } from '@/lib/i18n'
import { cn } from '@/lib/utils'

/**
 * Switches the app's language at once and remembers it on this device; for a
 * signed-in person it is also saved on the account, so it follows them to
 * other devices and into their emails.
 */
function useChooseLanguage() {
  const { user, setUser } = useAuth()
  return (language: Language) => {
    setLanguage(language)
    if (!user) return
    client
      .put('/auth/me/language', { language })
      .then(({ data }) => setUser(data))
      // Not saved on the account (offline, say): this device still remembers.
      .catch(() => undefined)
  }
}

/** "Language" in the account menu, with the languages in a submenu. */
export function LanguageMenu() {
  const t = useT()
  const language = useLanguage()
  const choose = useChooseLanguage()

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <Languages className="mr-2 h-4 w-4" />
        {t('language.label')}
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent>
        <DropdownMenuRadioGroup
          value={language}
          onValueChange={value => {
            const chosen = parseLanguage(value)
            if (chosen) choose(chosen)
          }}
        >
          {LANGUAGES.map(l => (
            // Each language is named in itself, so it must not be translated.
            <DropdownMenuRadioItem key={l.code} value={l.code} lang={l.code} translate="no">
              {l.name}
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}

/** The languages as quiet links, for the signed-out pages. */
export function LanguageLinks({ className }: { className?: string }) {
  const t = useT()
  const language = useLanguage()
  const choose = useChooseLanguage()

  return (
    <nav aria-label={t('language.label')} className={cn('flex items-center justify-center gap-3 text-sm', className)}>
      <Languages className="h-4 w-4 text-muted-foreground" aria-hidden />
      {LANGUAGES.map(l => (
        <button
          key={l.code}
          type="button"
          lang={l.code}
          translate="no"
          aria-current={l.code === language ? 'true' : undefined}
          onClick={() => choose(l.code)}
          className={cn(
            'rounded-sm underline-offset-4 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
            l.code === language ? 'font-semibold text-foreground' : 'text-muted-foreground hover:text-foreground hover:underline',
          )}
        >
          {l.name}
        </button>
      ))}
    </nav>
  )
}
