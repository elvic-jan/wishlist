import { useState } from 'react'
import { Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { LastingToast } from '@/components/LastingToast'
import { applyUpdate, useUpdateAvailable } from '@/lib/appUpdate'

/**
 * Shown once a newer release is deployed. It can't be put off, so nobody keeps
 * running a version the server has moved on from. What's new loads the new
 * version first, so the page lists that version's notes.
 */
export function UpdateToaster() {
  const available = useUpdateAvailable()
  const [refreshing, setRefreshing] = useState(false)
  if (!available) return null

  const update = (url?: string) => {
    setRefreshing(true)
    void applyUpdate(url)
  }

  return (
    <LastingToast icon={Sparkles} title="A new version of Kadoozle is available">
      <Button size="sm" variant="outline" disabled={refreshing} onClick={() => update('/whats-new')}>
        What's new
      </Button>
      <Button size="sm" disabled={refreshing} onClick={() => update()}>
        Refresh
      </Button>
    </LastingToast>
  )
}
