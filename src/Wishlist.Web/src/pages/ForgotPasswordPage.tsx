import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { requestPasswordReset } from '@/api/auth'
import { Trans, useT } from '@/lib/i18n'

export function ForgotPasswordPage() {
  const t = useT()
  const [email, setEmail] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      await requestPasswordReset(email)
      setSubmitted(true)
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status
      if (status === 429) {
        setError(t('forgot.tooMany'))
      } else {
        setError(t('forgot.failed'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (submitted) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>{t('forgot.sent.title')}</CardTitle>
          <CardDescription>
            <Trans k="forgot.sent.text" values={{ email }} components={{ b: <strong /> }} />
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3 text-sm text-muted-foreground">
          <p>
            <Trans
              k="forgot.sent.noEmail"
              components={{
                a: (
                  <button
                    type="button"
                    onClick={() => setSubmitted(false)}
                    className="text-primary underline-offset-4 hover:underline"
                  />
                ),
              }}
            />
          </p>
          <p>{t('forgot.sent.unconfirmed')}</p>
        </CardContent>
        <CardFooter className="justify-center">
          <Link
            to="/login"
            className="text-sm text-primary underline-offset-4 hover:underline"
          >
            {t('common.backToSignIn')}
          </Link>
        </CardFooter>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('forgot.title')}</CardTitle>
        <CardDescription>
          {t('forgot.description')}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {error && (
          <Alert variant="destructive" className="mb-4">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="forgot-email">{t('common.email')}</Label>
            <Input
              id="forgot-email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
            />
          </div>
          <Button type="submit" className="w-full" disabled={submitting}>
            {submitting ? t('forgot.sending') : t('forgot.submit')}
          </Button>
        </form>
      </CardContent>
      <CardFooter className="justify-center">
        <Link
          to="/login"
          className="text-sm text-primary underline-offset-4 hover:underline"
        >
          {t('common.backToSignIn')}
        </Link>
      </CardFooter>
    </Card>
  )
}
