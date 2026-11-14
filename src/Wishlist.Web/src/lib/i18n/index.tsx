/* eslint-disable react-refresh/only-export-components -- the hooks and helpers live with the two small components. */
import { cloneElement, Fragment, useSyncExternalStore, type ReactElement, type ReactNode } from 'react'
import { en } from './en'
import { nl } from './nl'

/**
 * The app's texts in every language.
 *
 * - `en.ts` is the source: every text has a key there. `nl.ts` must have the
 *   same keys (the compiler checks it).
 * - In a component: `const t = useT()` and `t('login.title')`. Outside React
 *   (helpers, toasts built in plain functions): `t` from this module.
 * - Values go in as `{name}`: `t('buddies.added', { name })`.
 * - Counts: keys ending in `_one` / `_other`, used through `plural('wishes.count', n)`.
 * - A sentence with markup or a name in it: `<Trans k="…" values={…} components={…} />`,
 *   so translators see the whole sentence. Values are marked `translate="no"`:
 *   what people typed (names, gift titles) is never translated, not by the app
 *   and not by the browser's page translation.
 *
 * Adding a language: a catalog like `nl.ts`, an entry in `LANGUAGES` and
 * `catalogs`, and the server's `Languages` + dictionary.
 */

export const LANGUAGES = [
  { code: 'en', name: 'English', locale: 'en-GB' },
  { code: 'nl', name: 'Nederlands', locale: 'nl-BE' },
] as const

export type Language = (typeof LANGUAGES)[number]['code']
export type TextKey = keyof typeof en
/** The base of a pair of keys `…_one` / `…_other`. */
export type PluralKey = {
  [K in TextKey]: K extends `${infer Base}_other` ? (`${Base}_one` extends TextKey ? Base : never) : never
}[TextKey]
export type Values = Record<string, string | number>

const catalogs: Record<Language, Record<TextKey, string>> = { en, nl }
const STORAGE_KEY = 'kadoozle-language'
const DEFAULT: Language = 'en'

/** The language of a tag such as "nl", "nl-BE" or "NL_be"; null when the app doesn't offer it. */
export function parseLanguage(tag: string | null | undefined): Language | null {
  const primary = tag?.trim().split(/[-_]/)[0]?.toLowerCase()
  return LANGUAGES.find(l => l.code === primary)?.code ?? null
}

function stored(): Language | null {
  try {
    return parseLanguage(localStorage.getItem(STORAGE_KEY))
  } catch {
    return null
  }
}

/** The first of the browser's preferred languages that the app offers. */
function fromBrowser(): Language | null {
  const preferred = typeof navigator === 'undefined' ? [] : (navigator.languages ?? [navigator.language])
  for (const tag of preferred) {
    const language = parseLanguage(tag)
    if (language) return language
  }
  return null
}

let current: Language = stored() ?? fromBrowser() ?? DEFAULT
const listeners = new Set<() => void>()

function show(language: Language) {
  if (typeof document !== 'undefined') document.documentElement.lang = language
}
show(current)

export function getLanguage(): Language {
  return current
}

/** The BCP 47 locale for dates and numbers in the current language. */
export function getLocale(): string {
  return LANGUAGES.find(l => l.code === current)!.locale
}

/**
 * Switches the app's language and remembers it on this device. Saving it on
 * the account is the caller's job (see AuthContext and LanguageSwitch).
 */
export function setLanguage(language: Language) {
  try {
    localStorage.setItem(STORAGE_KEY, language)
  } catch {
    // Private mode or blocked storage: the choice lasts until the page closes.
  }
  if (language === current) return
  current = language
  show(language)
  listeners.forEach(listener => listener())
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

function fill(template: string, values?: Values): string {
  return values ? template.replace(/\{(\w+)\}/g, (whole, name: string) => (name in values ? String(values[name]) : whole)) : template
}

function template(key: TextKey): string {
  return catalogs[current][key] ?? en[key] ?? key
}

/** The text for `key` in the current language. Call it while rendering (or later), never at module load. */
export function t(key: TextKey, values?: Values): string {
  return fill(template(key), values)
}

/** The `_one` or `_other` key for a count, for `<Trans>`. */
export function pluralKey(key: PluralKey, count: number): TextKey {
  const rule = new Intl.PluralRules(getLocale()).select(count)
  return `${key}_${rule === 'one' ? 'one' : 'other'}` as TextKey
}

/** The `_one` or `_other` text for a count; `{count}` is filled in. */
export function plural(key: PluralKey, count: number, values?: Values): string {
  return t(pluralKey(key, count), { count, ...values })
}

/** `t` that re-renders the component when the language changes. */
export function useT(): typeof t {
  useSyncExternalStore(subscribe, getLanguage, getLanguage)
  return t
}

/** The current language, re-rendering on change. */
export function useLanguage(): Language {
  return useSyncExternalStore(subscribe, getLanguage, getLanguage)
}

/** A date in the current language, e.g. "14 November 2026" / "14 november 2026". */
export function formatDate(date: Date, options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' }): string {
  return new Intl.DateTimeFormat(getLocale(), options).format(date)
}

/** Something people typed (a name, a gift title): shown as entered, never translated. */
export function UserText({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <span translate="no" className={className}>
      {children}
    </span>
  )
}

const TOKEN = /<(\w+)>(.*?)<\/\1>|<(\w+)\/>|\{(\w+)\}/g

function render(text: string, values: Record<string, ReactNode>, components: Record<string, ReactElement>, prefix: string): ReactNode[] {
  const out: ReactNode[] = []
  let last = 0
  let n = 0
  for (const match of text.matchAll(TOKEN)) {
    const [whole, tag, inner, selfClosing, value] = match
    if (match.index > last) out.push(text.slice(last, match.index))
    const key = `${prefix}${n++}`
    if (tag && components[tag]) {
      out.push(cloneElement(components[tag], { key }, ...render(inner, values, components, `${key}.`)))
    } else if (selfClosing && components[selfClosing]) {
      out.push(cloneElement(components[selfClosing], { key }))
    } else if (value && value in values) {
      const node = values[value]
      out.push(typeof node === 'number' ? node : <UserText key={key}>{node}</UserText>)
    } else {
      out.push(whole)
    }
    last = match.index + whole.length
  }
  if (last < text.length) out.push(text.slice(last))
  return out
}

/**
 * A translated sentence with markup: `<b>…</b>` and `<icon/>` in the text are
 * rendered with the elements in `components`; `{name}` with `values`.
 */
export function Trans({
  k,
  values = {},
  components = {},
}: {
  k: TextKey
  values?: Record<string, ReactNode>
  components?: Record<string, ReactElement>
}) {
  useSyncExternalStore(subscribe, getLanguage, getLanguage)
  return <Fragment>{render(template(k), values, components, '')}</Fragment>
}
