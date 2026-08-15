import { useQuery } from "@tanstack/react-query"
import {
  ClipboardCheck,
  ClipboardList,
  Clock,
  FileWarning,
} from "lucide-react"
import { lazy, Suspense, type ReactNode } from "react"

import {
  Card,
  CardContent,
  CardHeader,
} from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { useAuth } from "@/features/auth/auth-context"
import { HomePage } from "@/features/auth/HomePage"
import { dashboardApi } from "@/features/dashboard/dashboard-api"
import type { DashboardAnalyticsDto } from "@/features/dashboard/types"

const DashboardCharts = lazy(() =>
  import("@/features/dashboard/DashboardCharts").then((m) => ({ default: m.DashboardCharts }))
)

const dashboardRoles = ["AuditManager", "TopManagement", "Administrator"]

const statusLabels: Record<string, string> = {
  Draft: "Draf",
  Submitted: "Dikirim",
  Approved: "Disetujui",
  InProgress: "Berjalan",
  Completed: "Selesai",
}

function StatCard({
  icon,
  label,
  value,
  hint,
}: {
  icon: ReactNode
  label: string
  value: ReactNode
  hint?: ReactNode
}) {
  return (
    <Card>
      <CardContent className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm text-muted-foreground">{label}</p>
          <p className="mt-1 text-2xl font-medium">{value}</p>
          {hint && <p className="mt-1 text-xs text-muted-foreground">{hint}</p>}
        </div>
        <div className="rounded-lg bg-muted p-2">{icon}</div>
      </CardContent>
    </Card>
  )
}

export function DashboardPage() {
  const { user } = useAuth()
  const hasAccess = user?.roles.some((r) => dashboardRoles.includes(r)) ?? false

  const analytics = useQuery({
    queryKey: ["dashboard", "analytics"],
    queryFn: dashboardApi.analytics,
    enabled: hasAccess,
    refetchInterval: 5 * 60 * 1000,
  })

  if (!hasAccess) return <HomePage />

  if (analytics.isLoading) {
    return (
      <div className="grid gap-4">
        <div>
          <Skeleton className="h-6 w-40" />
          <Skeleton className="mt-1 h-4 w-72" />
        </div>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="flex items-start justify-between gap-3">
                <div className="grid gap-2">
                  <Skeleton className="h-4 w-28" />
                  <Skeleton className="h-7 w-16" />
                </div>
                <Skeleton className="size-9 rounded-lg" />
              </CardContent>
            </Card>
          ))}
        </div>
        <div className="grid gap-4 lg:grid-cols-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}>
              <CardHeader>
                <Skeleton className="h-5 w-40" />
                <Skeleton className="h-4 w-52" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-64 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  if (analytics.isError) {
    return (
      <p className="p-10 text-center text-muted-foreground">
        Gagal memuat data dashboard. Coba muat ulang halaman.
      </p>
    )
  }

  const data: DashboardAnalyticsDto = analytics.data!

  const statusData = data.auditStatusDistribution.map((s) => ({
    name: statusLabels[s.status] ?? s.status,
    count: s.count,
  }))
  const riskData = data.findingRiskDistribution.map((r) => ({ name: r.risk, count: r.count }))
  const departmentData = data.findingDepartmentDistribution.map((d) => ({
    name: d.department,
    count: d.count,
  }))
  const categoryData = data.findingCategoryDistribution.map((c) => ({
    name: c.category,
    count: c.count,
  }))
  const workloadData = data.auditorWorkload.map((w) => ({
    name: w.fullName,
    count: w.auditCount,
  }))

  return (
    <div className="grid gap-4">
      <div>
        <h1 className="text-xl font-medium">Dashboard Audit</h1>
        <p className="text-sm text-muted-foreground">
          Ringkasan kinerja program audit, temuan, dan tindak lanjut CAP.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          icon={<ClipboardList className="size-5" />}
          label="Total Rencana Audit"
          value={data.totalAudits}
          hint={
            <span>
              <b className="font-medium text-foreground">{data.auditProgressPercent}%</b> selesai
            </span>
          }
        />
        <StatCard
          icon={<FileWarning className="size-5" />}
          label="Total Temuan"
          value={data.totalFindings}
        />
        <StatCard
          icon={<ClipboardCheck className="size-5" />}
          label="CAP Terbuka"
          value={data.totalOpenCaps}
          hint={
            data.capsOverdue > 0 ? (
              <span className="font-medium text-red-600">{data.capsOverdue} terlambat</span>
            ) : undefined
          }
        />
        <StatCard
          icon={<Clock className="size-5" />}
          label="Waktu Penyelesaian Temuan"
          value={
            data.averageFindingResolutionDays !== null && data.averageFindingResolutionDays !== undefined
              ? `${data.averageFindingResolutionDays} hari`
              : "—"
          }
          hint="Rata-rata sejak temuan dibuat"
        />
      </div>

      <Suspense
        fallback={
          <div className="grid gap-4 lg:grid-cols-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <Card key={i}>
                <CardHeader>
                  <Skeleton className="h-5 w-40" />
                  <Skeleton className="h-4 w-52" />
                </CardHeader>
                <CardContent>
                  <Skeleton className="h-64 w-full" />
                </CardContent>
              </Card>
            ))}
          </div>
        }
      >
        <DashboardCharts
          statusData={statusData}
          riskData={riskData}
          departmentData={departmentData}
          categoryData={categoryData}
          workloadData={workloadData}
          capsDueTomorrow={data.capsDueTomorrow}
          capsOverdue={data.capsOverdue}
        />
      </Suspense>
    </div>
  )
}
