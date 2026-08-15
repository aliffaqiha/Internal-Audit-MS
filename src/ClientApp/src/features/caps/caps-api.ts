import { api, downloadBlob, type PagedResult } from "@/lib/api"

import type {
  CorrectiveActionDto,
  CreateCapPayload,
  UpdateCapPayload,
  CapsFilter,
} from "./types"

export interface VerifyPayload {
  approve: boolean
  note: string | null
}

export const capsApi = {
  list: (params?: CapsFilter) =>
    api.get<PagedResult<CorrectiveActionDto>>("/caps", { params }).then((r) => r.data),

  getByFinding: (findingId: string) =>
    api.get<CorrectiveActionDto | null>(`/caps/finding/${findingId}`).then((r) => r.data),

  create: (payload: CreateCapPayload) =>
    api.post<{ findingId: string }>("/caps", payload).then((r) => r.data),

  update: (id: string, payload: UpdateCapPayload) =>
    api.put(`/caps/${id}`, payload).then(() => undefined),

  start: (id: string) => api.post(`/caps/${id}/start`).then(() => undefined),
  submit: (id: string) => api.post(`/caps/${id}/submit`).then(() => undefined),
  verify: (id: string, payload: VerifyPayload) =>
    api.post(`/caps/${id}/verify`, { ...payload, id }).then(() => undefined),

  uploadAttachment: (capId: string, file: File) => {
    const form = new FormData()
    form.append("file", file)
    return api.post(`/caps/${capId}/attachment`, form, {
      headers: { "Content-Type": "multipart/form-data" },
    })
  },

  download: (capId: string, fileName: string) =>
    downloadBlob(`/caps/${capId}/attachment`, fileName),
}