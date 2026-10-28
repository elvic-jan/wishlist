import { useState } from 'react'
import { Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { LastingToast } from '@/components/LastingToast'
import { applyUpdate, useUpdateAvailable, useUpdateNote } from '@/lib/appUpdate'

/**
 * "New version available": shown once a newer release is deployed. It can't be
 * put off, so nobody keeps running a version the server has moved on from.
 */
export function UpdateToaster() {
  const available = useUpdateAvailable()
  const note = useUpdateNote()
  const [refreshing, setRefreshing] = useState(false)
  if (!available) return null

  return (
    <LastingToast icon={Sparkles} title="New version available" text={note ?? 'Refresh to get the latest GiftCircles.'}>
      <Button
        size="sm"
        disabled={refreshing}
        onClick={() => {
          setRefreshing(true)
          void applyUpdate()
        }}
      >
        Refresh
      </Button>
    </LastingToast>
  )
}
