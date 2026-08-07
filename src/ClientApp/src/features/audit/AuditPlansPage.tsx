import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { ClipboardList, Plus } from "lucide-react"
import { useState } from "react"
import { Link } from "react-router-dom"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Select } from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { useAuth } from "@/features/auth/auth-context"
import { auditApi } from "@/features/audit/audit-api"
import { AuditPlanFormDialog } from "@/features/audit/AuditPlanFormDialog"
import type { AuditPlanDto, AuditPlanStatus } from "@/features/audit/types"
import { AuditPlanStatusLabels } from "@/features/audit/types"
import { adminApi } from "@/features/admin/admin-api"

const plannerRoles = ["Auditor", "AuditManager", "Administrator"]

export function AuditPlansPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [statusFilter, setStatusFilter] = useState("")
  const [dialogOpen, setDialogOpen] = useState(false)

  const isPlanner = user?.roles.some((r) => plannerRoles.includes(r)) ?? false

  const plans = useQuery({
    queryKey: ["audits", statusFilter],
    queryFn: () => auditApi.list(statusFilter ? { status: statusFilter as AuditPlanStatus } : undefined),
  })
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments })
  const team = useQuery({ queryKey: ["audit-team"], queryFn: auditApi.team, enabled: isPlanner })

  const createPlan = useMutation({
    mutationFn: auditApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["audits"] })
    },
  })

  const statusOrder: AuditPlanStatus[] = [
    "Draft",
    "Submitted",
    "Approved",
    "InProgress",
    "Completed",
  ]

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-medium">Rencana Audit</h1>
          <p className="text-sm text-muted-foreground">
            Kelola alur perencanaan audit dari draf hingga selesai.
          </p>
        </div>
        {isPlanner && (
          <Button onClick={() => setDialogOpen(true)}>
            <Plus data-icon="inline-start" />
            Buat Rencana Audit
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <div className="w-44">
              <Select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">Semua Status</option>
                {statusOrder.map((s) => (
                  <option key={s} value={s}>
                    {AuditPlanStatusLabels[s]}
                  </option>
                ))}
              </Select>
            </div>
          </CardTitle>
        </CardHeader>
        <CardContent className="overflow-x-auto">
          {plans.isLoading ? (
            <p className="p-6 text-center text-muted-foreground">Memuat data...</p>
          ) : (plans.data ?? []).length === 0 ? (
            <div className="grid place-items-center gap-2 p-10 text-center text-muted-foreground">
              <ClipboardList className="size-8" />
              <p>Belum ada rencana audit.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Judul</TableHead>
                  <TableHead>Standar</TableHead>
                  <TableHead>Departemen</TableHead>
                  <TableHead>Jadwal</TableHead>
                  <TableHead>Tim</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(plans.data ?? []).map((plan: AuditPlanDto) => (
                  <TableRow key={plan.id} className="cursor-pointer">
                    <TableCell>
                      <Link
                        to={`/audits/${plan.id}`}
                        className="font-medium hover:text-primary"
                      >
                        {plan.title}
                      </Link>
                    </TableCell>
                    <TableCell>{plan.standard ?? "—"}</TableCell>
                    <TableCell>{plan.departmentName ?? "—"}</TableCell>
                    <TableCell className="whitespace-nowrap text-xs">
                      {plan.startDate ? new Date(plan.startDate).toLocaleDateString() : "—"}
                      {" s/d "}
                      {plan.endDate ? new Date(plan.endDate).toLocaleDateString() : "—"}
                    </TableCell>
                    <TableCell>
                      <div className="flex max-w-56 flex-wrap gap-1">
                        {plan.assignments.map((a) => (
                          <Badge key={a.userId} variant="secondary">
                            {a.fullName}
                          </Badge>
                        ))}
                        {plan.assignments.length === 0 && <span className="text-muted-foreground">—</span>}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{AuditPlanStatusLabels[plan.status]}</Badge>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <AuditPlanFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        departments={departments.data ?? []}
        team={team.data ?? []}
        onSubmit={createPlan.mutateAsync}
      />
    </div>
  )
}