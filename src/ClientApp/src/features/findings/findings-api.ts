import { api, type PagedResult } from "@/lib/api"

import type { CreateFindingPayload, FindingDto, FindingsFilter } from "./types"

export const findingsApi = {
  list: (params?: FindingsFilter) =>
    api.get<PagedResult<FindingDto>>("/findings", { params }).then((r) => r.data),

  get: (id: string) => api.get<FindingDto>(`/findings/${id}`).then((r) => r.data),

  create: (payload: CreateFindingPayload) =>
    api.post<{ id: string }>("/findings", payload).then((r) => r.data.id),

  update: (id: string, payload: CreateFindingPayload) =>
    api.put(`/findings/${id}`, payload).then(() => undefined),

  remove: (id: string) => api.delete(`/findings/${id}`).then(() => undefined),

  uploadEvidence: (findingId: string, file: File) => {
    const form = new FormData()
    form.append("file", file)
    return api
      .post<{ id: string }>(`/findings/${findingId}/evidence`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data.id)
  },

  downloadUrl: (findingId: string, evidenceId: string) =>
    `/api/findings/${findingId}/evidence/${evidenceId}`,
}