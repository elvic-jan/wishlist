import { useState } from 'react'
import { Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { LastingToast } from '@/components/LastingToast'
import { applyUpdate, putOffUpdateNotice, useUpdateNoticeShown } from '@/lib/appUpdate'

/** "New version available": shown once a newer release is deployed. */
export function UpdateToaster() {
  const shown = useUpdateNoticeShown()
  const [refreshing, setRefreshing] = useState(false)
  if (!shown) return null

  return (
    <LastingToast
      icon={Sparkles}
      title="New version available"
      text="Refresh to get the latest GiftCircles."
      onClose={putOffUpdateNotice}
    >
      <Button size="sm" variant="ghost" onClick={putOffUpdateNotice}>
        Not now
      </Button>
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
