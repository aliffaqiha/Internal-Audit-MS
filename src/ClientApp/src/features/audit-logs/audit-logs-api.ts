import { api, type PagedResult } from "@/lib/api"

import type { AuditLogDto } from "./types"

export interface AuditLogsQuery {
  search?: string
  entity?: string
  page?: number
  pageSize?: number
}

export const auditLogsApi = {
  list: (query?: AuditLogsQuery) =>
    api
      .get<PagedResult<AuditLogDto>>("/audit-logs", { params: query })
      .then((r) => r.data),
}