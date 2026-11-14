import { useState } from 'react'
import { Share, SquarePlus, Smartphone } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { DropdownMenuItem } from '@/components/ui/dropdown-menu'
import { promptInstall, useInstallOffer } from '@/lib/installPrompt'
import { Trans, useT } from '@/lib/i18n'

/**
 * "Install app" in the account menu; renders nothing where it can't help.
 * The iPhone help dialog must live outside the menu (which unmounts when it
 * closes), so the caller renders <IosInstallHelp> and passes the opener.
 */
export function InstallAppMenuItem({ onShowIosHelp }: { onShowIosHelp: () => void }) {
  const t = useT()
  const offer = useInstallOffer()
  if (offer !== 'prompt' && offer !== 'ios') return null

  return (
    <DropdownMenuItem onSelect={() => (offer === 'prompt' ? void promptInstall() : onShowIosHelp())}>
      <Smartphone className="mr-2 h-4 w-4" />
      {t('install.menuItem')}
    </DropdownMenuItem>
  )
}

/** A quiet "Install the app" link, for pages outside the app shell. */
export function InstallAppLink() {
  const t = useT()
  const offer = useInstallOffer()
  const [showIosHelp, setShowIosHelp] = useState(false)

  if (offer !== 'prompt' && offer !== 'ios') return null

  return (
    <>
      <button
        type="button"
        className="inline-flex items-center gap-1.5 text-sm text-primary underline-offset-4 hover:underline"
        onClick={() => (offer === 'prompt' ? void promptInstall() : setShowIosHelp(true))}
      >
        <Smartphone className="h-4 w-4" aria-hidden />
        {t('install.link')}
      </button>
      <IosInstallHelp open={showIosHelp} onOpenChange={setShowIosHelp} />
    </>
  )
}

export function IosInstallHelp({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const t = useT()
  const bold = { b: <strong /> }
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('install.ios.title')}</DialogTitle>
          <DialogDescription>
            {t('install.ios.description')}
          </DialogDescription>
        </DialogHeader>
        <ol className="space-y-3 text-sm">
          <li className="flex items-start gap-3">
            <Step n={1} />
            <span>
              <Trans
                k="install.ios.step1"
                components={{
                  ...bold,
                  icon: <Share className="inline h-4 w-4 align-text-bottom text-primary" aria-label={t('install.ios.share')} />,
                }}
              />
            </span>
          </li>
          <li className="flex items-start gap-3">
            <Step n={2} />
            <span>
              <Trans
                k="install.ios.step2"
                components={{ ...bold, icon: <SquarePlus className="inline h-4 w-4 align-text-bottom text-primary" aria-hidden /> }}
              />
            </span>
          </li>
          <li className="flex items-start gap-3">
            <Step n={3} />
            <span>
              <Trans k="install.ios.step3" components={bold} />
            </span>
          </li>
        </ol>
        <DialogFooter>
          <Button type="button" onClick={() => onOpenChange(false)}>
            {t('install.ios.gotIt')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function Step({ n }: { n: number }) {
  return (
    <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary/15 text-xs font-semibold text-primary">
      {n}
    </span>
  )
}
