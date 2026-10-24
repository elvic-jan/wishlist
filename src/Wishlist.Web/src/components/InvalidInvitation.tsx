import { Link } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'

/** Shown instead of the join/sign-up options when an invite link can't be used. */
export function InvalidInvitation({ message }: { message: string }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Invitation can&rsquo;t be used</CardTitle>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive">
          <AlertDescription>{message}</AlertDescription>
        </Alert>
      </CardContent>
      <CardFooter className="justify-center">
        <Button asChild variant="outline">
          <Link to="/login">Sign in</Link>
        </Button>
      </CardFooter>
    </Card>
  )
}
