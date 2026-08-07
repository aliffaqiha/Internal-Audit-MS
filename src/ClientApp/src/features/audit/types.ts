export type AuditPlanStatus =
  | "Draft"
  | "Submitted"
  | "Approved"
  | "InProgress"
  | "Completed"

export const AuditPlanStatusLabels: Record<AuditPlanStatus, string> = {
  Draft: "Draf",
  Submitted: "Dikirim",
  Approved: "Disetujui",
  InProgress: "Berjalan",
  Completed: "Selesai",
}

export type ChecklistItemStatus = "Pending" | "Pass" | "Fail" | "NotApplicable"

export const ChecklistItemStatusLabels: Record<ChecklistItemStatus, string> = {
  Pending: "Pending",
  Pass: "Lulus",
  Fail: "Gagal",
  NotApplicable: "N/A",
}

export interface AuditTeamMemberDto {
  userId: string
  username: string
  fullName: string
}

export interface AuditAssignmentInput {
  userId: string
  roleInPlan: string | null
}

export interface AuditChecklistItemInput {
  question: string
  category: string | null
  isRequired: boolean
}

export interface CreateAuditPlanPayload {
  title: string
  objective: string | null
  scope: string | null
  standard: string | null
  startDate: string | null
  endDate: string | null
  departmentId: string | null
  assignments: AuditAssignmentInput[]
  checklistItems: AuditChecklistItemInput[]
}

export interface AuditPlanAssignmentDto {
  userId: string
  username: string
  fullName: string
  roleInPlan: string | null
}

export interface AuditPlanChecklistItemDto {
  id: string
  question: string
  category: string | null
  isRequired: boolean
  status: ChecklistItemStatus
  note: string | null
}

export interface AuditPlanDto {
  id: string
  title: string
  objective: string | null
  scope: string | null
  standard: string | null
  startDate: string | null
  endDate: string | null
  status: AuditPlanStatus
  departmentId: string | null
  departmentName: string | null
  assignments: AuditPlanAssignmentDto[]
  checklistItems: AuditPlanChecklistItemDto[]
}

export const IT_TEMPLATE: AuditChecklistItemInput[] = [
  { question: "Backup data otomatis terjadwal dan tercatat di lokasi terpisah.", category: "Backup", isRequired: true },
  { question: "Backup rutin diuji pemulihan (restore test) secara berkala.", category: "Backup", isRequired: true },
  { question: "Firewall aktif pada perimeter jaringan dan konfigurasinya terdokumentasi.", category: "Firewall", isRequired: true },
  { question: "Perubahan aturan firewall melalui proses approval dan ditinjau berkala.", category: "Firewall", isRequired: true },
  { question: "Hak akses pengguna sesuai prinsip least-privilege.", category: "Access Control", isRequired: true },
  { question: "Akun bekas karyawan dinonaktifkan atau dihapus tepat waktu.", category: "Access Control", isRequired: true },
  { question: "Server dan aplikasi menerapkan patch keamanan terkini.", category: "Patch", isRequired: true },
  { question: "Asset lunak diaudit untuk versi dan kerentanan dikenal secara berkala.", category: "Patch", isRequired: true },
]

export const StandardOptions = ["IT", "ISO 27001", "Internal Finance", "Procurement", "Lainnya"]