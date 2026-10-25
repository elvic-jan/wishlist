import { ArrowLeft } from 'lucide-react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'

/**
 * Goes back to the screen the user came from. When the page was opened
 * directly (a link, a new tab), there is no earlier screen in the app, so it
 * goes to "My circles" instead of leaving the site.
 */
export function BackButton() {
  const navigate = useNavigate()
  const location = useLocation()
  // React Router gives the very first entry of the session the key "default".
  const hasPreviousScreen = location.key !== 'default'

  return (
    <Button
      variant="ghost"
      size="sm"
      className="-ml-2 mb-2"
      onClick={() => (hasPreviousScreen ? navigate(-1) : navigate('/'))}
    >
      <ArrowLeft className="h-4 w-4" />
      Back
    </Button>
  )
}
