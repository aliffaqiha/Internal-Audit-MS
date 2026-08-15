export type CapStatus = "Open" | "InProgress" | "PendingVerification" | "Closed"

export const CapStatusLabels: Record<CapStatus, string> = {
  Open: "Terbuka",
  InProgress: "Berjalan",
  PendingVerification: "Menunggu Verifikasi",
  Closed: "Ditutup",
}

export interface CapAttachmentDto {
  fileName: string | null
  contentType: string | null
  sizeBytes: number | null
  uploadedAt: string | null
}

export interface CorrectiveActionDto {
  id: string
  findingId: string
  findingTitle: string
  action: string
  picName: string | null
  targetDate: string | null
  progress: number
  status: CapStatus
  rejectionReason: string | null
  verificationNote: string | null
  verifiedAt: string | null
  attachment: CapAttachmentDto | null
}

export interface CapsFilter {
  status?: CapStatus
  findingId?: string
  departmentId?: string
  page?: number
  pageSize?: number
}

export interface CreateCapPayload {
  findingId: string
  action: string
  picName: string | null
  targetDate: string | null
  progress: number
}

export interface UpdateCapPayload {
  action: string
  picName: string | null
  targetDate: string | null
  progress: number
}