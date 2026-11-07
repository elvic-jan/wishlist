import client from './client'

export interface AdminTotals {
  users: number
  circles: number
  wishes: number
  activeReservations: number
}

export interface MonthlySignUps {
  year: number
  month: number
  count: number
}

export interface ActiveUsers {
  days: number
  current: number
  /** Null while tracking doesn't cover the whole previous period. */
  previous: number | null
}

export interface AdminOverview {
  totals: AdminTotals
  signUpsThisMonth: number
  signUpsLastMonth: number
  signUpsByMonth: MonthlySignUps[]
  active7: ActiveUsers
  active30: ActiveUsers
  /** First recorded active day (yyyy-mm-dd), or null. */
  trackingSince: string | null
}

export interface AdminUser {
  id: string
  displayName: string
  email: string
  createdAt: string
  emailConfirmed: boolean
  hasPassword: boolean
  hasPasskey: boolean
  isAdmin: boolean
  circles: number
  lastSignInAt: string | null
  signIns: number
  /** yyyy-mm-dd, or null. */
  lastActiveOn: string | null
}

export async function getAdminOverview(): Promise<AdminOverview> {
  return (await client.get<AdminOverview>('/admin/overview')).data
}

export async function getAdminUsers(): Promise<AdminUser[]> {
  return (await client.get<AdminUser[]>('/admin/users')).data
}
