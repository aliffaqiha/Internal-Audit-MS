export interface AuditLogDto {
  id: string
  userId: string | null
  userName: string | null
  action: string
  entity: string
  entityId: string | null
  ipAddress: string | null
  oldValues: string | null
  newValues: string | null
  createdAt: string
}