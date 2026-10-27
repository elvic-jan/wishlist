import { useEffect, useState } from 'react'
import { ChevronRight, Plus, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { CircleInvitationsCard } from '@/components/CircleInvitationsCard'
import client from '@/api/client'

interface Group {
  id: string
  name: string
  createdAt: string
}

export function HomePage() {
  const [groups, setGroups] = useState<Group[] | null>(null)
  const [newGroupName, setNewGroupName] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [error, setError] = useState('')

  const loadGroups = () =>
    client
      .get('/groups/me')
      .then(r => setGroups(r.data))
      .catch((err: unknown) => {
        const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
        setError(msg ?? 'Failed to load circles.')
      })

  useEffect(() => {
    loadGroups()
  }, [])

  const createGroup = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    try {
      const { data } = await client.post('/groups', { name: newGroupName })
      setGroups(prev => [...(prev ?? []), data])
      setNewGroupName('')
      setShowCreate(false)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setError(msg ?? 'Failed to create circle.')
    }
  }

  return (
    <div className="space-y-6">
      <div className="space-y-1.5">
        <div className="flex items-center gap-2">
          <Users className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">My circles</h1>
        </div>
        <p className="text-sm text-muted-foreground">
          Share wishlists with people you know. Create a circle, send the invite
          link, and you&rsquo;re in.
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <CircleInvitationsCard onJoined={loadGroups} />

      {!groups ? (
        // Still loading: never show "No circles yet" before the circles arrive.
        error ? null : (
          <div className="space-y-2" aria-label="Loading circles">
            <Skeleton className="h-14 w-full" />
            <Skeleton className="h-14 w-full" />
          </div>
        )
      ) : groups.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Users className="h-6 w-6" />
            </div>
            <div className="space-y-1">
              <p className="text-sm font-medium">No circles yet</p>
              <p className="text-sm text-muted-foreground">
                Create your first circle to start sharing.
              </p>
            </div>
            {!showCreate && (
              <Button onClick={() => setShowCreate(true)} className="mt-2">
                <Plus className="h-4 w-4" />
                New circle
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <ul className="space-y-2">
          {groups.map(g => (
            <li key={g.id}>
              <Link to={`/circles/${g.id}`} className="group block">
                <Card className="transition-colors hover:border-primary/40 hover:bg-accent/40">
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 py-4">
                    <CardTitle className="text-base font-medium">
                      {g.name}
                    </CardTitle>
                    <ChevronRight className="h-4 w-4 text-muted-foreground transition-colors group-hover:text-primary" />
                  </CardHeader>
                </Card>
              </Link>
            </li>
          ))}
        </ul>
      )}

      {showCreate ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">New circle</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={createGroup} className="space-y-3">
              <div className="space-y-1.5">
                <Label htmlFor="new-group-name">Circle name</Label>
                <Input
                  id="new-group-name"
                  value={newGroupName}
                  onChange={e => setNewGroupName(e.target.value)}
                  placeholder="Family, Friends, Work Secret Santa…"
                  required
                  autoFocus
                />
              </div>
              <div className="flex justify-end gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setShowCreate(false)
                    setNewGroupName('')
                  }}
                >
                  Cancel
                </Button>
                <Button type="submit">Create circle</Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : (
        groups && groups.length > 0 && (
          <Button onClick={() => setShowCreate(true)} variant="outline">
            <Plus className="h-4 w-4" />
            New circle
          </Button>
        )
      )}
    </div>
  )
}
