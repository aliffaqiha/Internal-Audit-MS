import { api } from "@/lib/api"

import type { AuditLogDto } from "./types"

export interface AuditLogsQuery {
  search?: string
  entity?: string
  take?: number
}

export const auditLogsApi = {
  list: (query?: AuditLogsQuery) =>
    api
      .get<AuditLogDto[]>("/audit-logs", { params: query })
      .then((r) => r.data),
}