// Secret Santa drawings (Wishlist.API/Controllers/DrawingsController.cs).

export type ParticipantStatus = 'invited' | 'joined' | 'declined'

export interface DrawingParticipant {
  id: string
  name: string
  status: ParticipantStatus
  /** Set only when you may open this person's wishlist. */
  userId: string | null
  /** The typed address; for the organizer only, until the person joins. */
  email: string | null
  isMe: boolean
  canJoinForThem: boolean
}

export interface DrawingRecipient {
  drawingId: string
  drawingTitle: string
  name: string
  /** Set only when you may open the recipient's wishlist. */
  userId: string | null
  joined: boolean
}

export interface ExclusionPair {
  first: string
  second: string
}

export interface Drawing {
  id: string
  title: string
  /** yyyy-mm-dd */
  endDate: string
  budget: string | null
  organizerName: string
  isOrganizer: boolean
  drawn: boolean
  cancelled: boolean
  ended: boolean
  myParticipantId: string | null
  myStatus: ParticipantStatus | null
  participants: DrawingParticipant[]
  exclusions: ExclusionPair[]
  myRecipient: DrawingRecipient | null
}

export interface DrawingInvitation {
  drawingId: string
  participantId: string
  title: string
  organizerName: string
  endDate: string
  budget: string | null
  drawn: boolean
}

export interface DrawingSummary {
  endDate: string
  isOrganizer: boolean
  myStatus: ParticipantStatus | null
  drawn: boolean
  ended: boolean
  cancelled: boolean
}

export interface AddParticipantsRequest {
  circles?: { circleId: string; excludedUserIds?: string[] }[]
  userIds?: string[]
  invites?: { name: string; email: string }[]
}

/** A yyyy-mm-dd date as a local Date (no time zone shift). */
export function parseDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, month - 1, day)
}

/** Today as yyyy-mm-dd in local time. */
export function today(): string {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

/** The email rows that are filled in, trimmed; half-filled rows are sent too, so the server rejects them. */
export function filledInvites(rows: { name: string; email: string }[]): { name: string; email: string }[] {
  return rows
    .map(r => ({ name: r.name.trim(), email: r.email.trim() }))
    .filter(r => r.name !== '' || r.email !== '')
}
