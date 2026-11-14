import { Link } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { useT } from '@/lib/i18n'

/** Shown instead of the join/sign-up options when an invite link can't be used. */
export function InvalidInvitation({ message }: { message: string }) {
  const t = useT()
  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('invitation.invalid.title')}</CardTitle>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive">
          <AlertDescription>{message}</AlertDescription>
        </Alert>
      </CardContent>
      <CardFooter className="justify-center">
        <Button asChild variant="outline">
          <Link to="/login">{t('login.title')}</Link>
        </Button>
      </CardFooter>
    </Card>
  )
}
