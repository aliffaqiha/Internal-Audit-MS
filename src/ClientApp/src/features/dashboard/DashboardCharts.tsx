import { AlertTriangle, CalendarClock } from "lucide-react"
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

interface BarDatum {
  name: string
  count: number
}

interface DashboardChartsProps {
  statusData: BarDatum[]
  riskData: BarDatum[]
  departmentData: BarDatum[]
  categoryData: BarDatum[]
  workloadData: BarDatum[]
  capsDueTomorrow: number
  capsOverdue: number
}

const riskColors: Record<string, string> = {
  Low: "#10b981",
  Medium: "#f59e0b",
  High: "#f97316",
  Critical: "#ef4444",
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
  data: BarDatum[]
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

export function DashboardCharts({
  statusData,
  riskData,
  departmentData,
  categoryData,
  workloadData,
  capsDueTomorrow,
  capsOverdue,
}: DashboardChartsProps) {
  return (
    <>
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
                {capsDueTomorrow}
              </Badge>
            </div>
            <div className="flex items-center justify-between rounded-lg border border-red-200 bg-red-50/50 p-3">
              <div className="flex items-center gap-2">
                <AlertTriangle className="size-4 text-red-600" />
                <span className="text-sm">Melewati tenggat</span>
              </div>
              <Badge variant="outline" className="border-red-300 text-red-700">
                {capsOverdue}
              </Badge>
            </div>
          </CardContent>
        </Card>
      </div>
    </>
  )
}
