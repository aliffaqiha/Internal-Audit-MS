export interface AuditStatusDistributionDto {
  status: string
  count: number
}

export interface FindingRiskDistributionDto {
  risk: string
  count: number
}

export interface FindingDepartmentDistributionDto {
  department: string
  count: number
}

export interface FindingCategoryDistributionDto {
  category: string
  count: number
}

export interface AuditorWorkloadDto {
  fullName: string
  auditCount: number
}

export interface DashboardAnalyticsDto {
  totalAudits: number
  auditProgressPercent: number
  totalFindings: number
  totalOpenCaps: number
  capsDueTomorrow: number
  capsOverdue: number
  averageFindingResolutionDays: number | null
  auditStatusDistribution: AuditStatusDistributionDto[]
  findingRiskDistribution: FindingRiskDistributionDto[]
  findingDepartmentDistribution: FindingDepartmentDistributionDto[]
  findingCategoryDistribution: FindingCategoryDistributionDto[]
  auditorWorkload: AuditorWorkloadDto[]
}
