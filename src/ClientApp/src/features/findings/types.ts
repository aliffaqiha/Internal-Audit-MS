export type RiskLevel = "Low" | "Medium" | "High" | "Critical"

export const RiskLevelLabels: Record<RiskLevel, string> = {
  Low: "Rendah",
  Medium: "Sedang",
  High: "Tinggi",
  Critical: "Kritis",
}

export interface FindingEvidenceDto {
  id: string
  originalFileName: string
  contentType: string
  sizeBytes: number
  version: number
  uploadedAt: string
}

export interface FindingDto {
  id: string
  title: string
  description: string | null
  departmentId: string | null
  departmentName: string | null
  riskLevel: RiskLevel
  category: string | null
  recommendation: string | null
  dueDate: string | null
  auditPlanId: string | null
  evidences: FindingEvidenceDto[]
}

export interface FindingsFilter {
  riskLevel?: RiskLevel
  departmentId?: string
  search?: string
}

export interface CreateFindingPayload {
  title: string
  description: string | null
  departmentId: string | null
  riskLevel: RiskLevel
  category: string | null
  recommendation: string | null
  dueDate: string | null
  auditPlanId: string | null
}