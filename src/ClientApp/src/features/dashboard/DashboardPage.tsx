import { useQuery } from "@tanstack/react-query"
import {
  AlertTriangle,
  CalendarClock,
  ClipboardCheck,
  ClipboardList,
  Clock,
  FileWarning,
} from "lucide-react"
import type { ReactNode } from "react"
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"

import { Badge } from "@/components/ui/badge"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { useAuth } from "@/features/auth/auth-context"
import { HomePage } from "@/features/auth/HomePage"
import { dashboardApi } from "@/features/dashboard/dashboard-api"
import type { DashboardAnalyticsDto } from "@/features/dashboard/types"

const dashboardRoles = ["AuditManager", "TopManagement", "Administrator"]

const statusLabels: Record<string, string> = {
  Draft: "Draf",
  Submitted: "Dikirim",
  Approved: "Disetujui",
  InProgress: "Berjalan",
  Completed: "Selesai",
}

const riskColors: Record<string, string> = {
  Low: "#10b981",
  Medium: "#f59e0b",
  High: "#f97316",
  Critical: "#ef4444",
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

function HBarCard({
  title,
  description,
  data,
  emptyText,
  color,
}: {
  title: string
  description?: string
  data: { name: string; count: number }[]
  emptyText: string
  color: string
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        {description && <CardDescription>{description}</CardDescription>}
      </CardHeader>
      <CardContent>
        {data.length === 0 ? (
          <p className="py-10 text-center text-muted-foreground">{emptyText}</p>
        ) : (
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data} layout="vertical" margin={{ left: 8, right: 8 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={false} />
                <XAxis type="number" allowDecimals={false} />
                <YAxis type="category" dataKey="name" width={110} />
                <Tooltip />
                <Bar dataKey="count" name="Jumlah" fill={color} radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
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
    return <p className="p-10 text-center text-muted-foreground">Memuat data...</p>
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

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Distribusi Status Audit</CardTitle>
            <CardDescription>Jumlah rencana audit per tahap alur kerja</CardDescription>
          </CardHeader>
          <CardContent>
            {statusData.every((s) => s.count === 0) ? (
              <p className="py-10 text-center text-muted-foreground">Belum ada rencana audit.</p>
            ) : (
              <div className="h-64">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={statusData}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis dataKey="name" />
                    <YAxis allowDecimals={false} />
                    <Tooltip />
                    <Bar dataKey="count" name="Jumlah" fill="#6366f1" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Temuan per Tingkat Risiko</CardTitle>
            <CardDescription>Sebaran risiko temuan audit</CardDescription>
          </CardHeader>
          <CardContent>
            {riskData.every((r) => r.count === 0) ? (
              <p className="py-10 text-center text-muted-foreground">Belum ada temuan.</p>
            ) : (
              <div className="h-64">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={riskData}
                      dataKey="count"
                      nameKey="name"
                      innerRadius={50}
                      outerRadius={85}
                      paddingAngle={2}
                    >
                      {riskData.map((entry) => (
                        <Cell key={entry.name} fill={riskColors[entry.name] ?? "#94a3b8"} />
                      ))}
                    </Pie>
                    <Tooltip />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <HBarCard
          title="Temuan per Departemen"
          data={departmentData}
          emptyText="Belum ada temuan per departemen."
          color="#0ea5e9"
        />
        <HBarCard
          title="Temuan per Kategori"
          data={categoryData}
          emptyText="Belum ada temuan per kategori."
          color="#8b5cf6"
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <HBarCard
          title="Beban Audit per Auditor"
          description="Jumlah rencana audit yang dipegang tiap auditor"
          data={workloadData}
          emptyText="Belum ada penugasan auditor."
          color="#ec4899"
        />

        <Card>
          <CardHeader>
            <CardTitle>CAP Perlu Perhatian</CardTitle>
            <CardDescription>Tindak lanjut yang mendekati atau melewati tenggat</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-3">
            <div className="flex items-center justify-between rounded-lg border p-3">
              <div className="flex items-center gap-2">
                <CalendarClock className="size-4 text-blue-600" />
                <span className="text-sm">Jatuh tempo besok</span>
              </div>
              <Badge variant="outline" className="text-blue-700">
                {data.capsDueTomorrow}
              </Badge>
            </div>
            <div className="flex items-center justify-between rounded-lg border border-red-200 bg-red-50/50 p-3">
              <div className="flex items-center gap-2">
                <AlertTriangle className="size-4 text-red-600" />
                <span className="text-sm">Melewati tenggat</span>
              </div>
              <Badge variant="outline" className="border-red-300 text-red-700">
                {data.capsOverdue}
              </Badge>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
