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
import { Pagination } from "@/components/ui/pagination"
import { Select } from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { toast } from "@/components/ui/toast"
import { useAuth } from "@/features/auth/auth-context"
import { auditApi } from "@/features/audit/audit-api"
import { AuditPlanFormDialog } from "@/features/audit/AuditPlanFormDialog"
import { AuditPlanStatusBadge } from "@/features/audit/AuditPlanStatusBadge"
import type { AuditPlanDto, AuditPlanStatus } from "@/features/audit/types"
import { AuditPlanStatusLabels } from "@/features/audit/types"
import { adminApi } from "@/features/admin/admin-api"

const plannerRoles = ["Auditor", "AuditManager", "Administrator"]
const PAGE_SIZE = 15

export function AuditPlansPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [statusFilter, setStatusFilter] = useState("")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [page, setPage] = useState(1)

  const isPlanner = user?.roles.some((r) => plannerRoles.includes(r)) ?? false

  const plans = useQuery({
    queryKey: ["audits", statusFilter, page],
    queryFn: () =>
      auditApi.list({
        status: statusFilter ? (statusFilter as AuditPlanStatus) : undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
  })
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments })
  const team = useQuery({ queryKey: ["audit-team"], queryFn: auditApi.team, enabled: isPlanner })

  const createPlan = useMutation({
    mutationFn: auditApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["audits"] })
      toast.success("Rencana audit berhasil dibuat")
    },
    onError: (err) => {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      toast.error(message ?? "Gagal membuat rencana audit.")
    },
  })

  const statusOrder: AuditPlanStatus[] = [
    "Draft",
    "Submitted",
    "Approved",
    "InProgress",
    "Completed",
  ]

  const handleStatusFilter = (v: string) => { setStatusFilter(v); setPage(1) }

  const items = plans.data?.items ?? []
  const totalCount = plans.data?.totalCount ?? 0

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
              <Select value={statusFilter} onChange={(e) => handleStatusFilter(e.target.value)}>
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
        <CardContent className="grid gap-3">
          <div className="overflow-x-auto">
            {plans.isLoading ? (
              <div className="grid gap-2 p-2">
                {Array.from({ length: 6 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full rounded-md" />
                ))}
              </div>
            ) : items.length === 0 ? (
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
                  {items.map((plan: AuditPlanDto) => (
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
                        <AuditPlanStatusBadge status={plan.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>
          <Pagination
            page={page}
            total={totalCount}
            pageSize={PAGE_SIZE}
            onPageChange={setPage}
          />
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