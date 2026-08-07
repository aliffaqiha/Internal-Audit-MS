import { api } from "@/lib/api"

import type {
  AuditPlanDto,
  AuditPlanStatus,
  CreateAuditPlanPayload,
  AuditTeamMemberDto,
  ChecklistItemStatus,
} from "./types"

export const auditApi = {
  team: () => api.get<AuditTeamMemberDto[]>("/audits/team").then((r) => r.data),

  list: (params?: { status?: AuditPlanStatus }) =>
    api.get<AuditPlanDto[]>("/audits", { params }).then((r) => r.data),

  get: (id: string) => api.get<AuditPlanDto>(`/audits/${id}`).then((r) => r.data),

  create: (payload: CreateAuditPlanPayload) =>
    api.post<{ id: string }>("/audits", payload).then((r) => r.data.id),

  submit: (id: string) => api.post(`/audits/${id}/submit`).then(() => undefined),
  approve: (id: string, comment: string | null) =>
    api.post(`/audits/${id}/approve`, { comment }).then(() => undefined),
  start: (id: string) => api.post(`/audits/${id}/start`).then(() => undefined),
  complete: (id: string) => api.post(`/audits/${id}/complete`).then(() => undefined),

  updateChecklistItem: (
    planId: string,
    itemId: string,
    payload: { status: ChecklistItemStatus; note: string | null }
  ) => api.put(`/audits/${planId}/checklist/${itemId}`, payload).then(() => undefined),
}