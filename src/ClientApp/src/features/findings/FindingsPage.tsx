import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query"
import { FileWarning, Plus, Pencil, Trash2 } from "lucide-react"
import { useState } from "react"
import { Link } from "react-router-dom"

import { AlertDialog } from "@/components/ui/alert-dialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
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
import { useAuth } from "@/features/auth/auth-context"
import { auditApi } from "@/features/audit/audit-api"
import { adminApi } from "@/features/admin/admin-api"
import { findingsApi } from "@/features/findings/findings-api"
import { FindingFormDialog } from "@/features/findings/FindingFormDialog"
import { RiskLevelLabels, type FindingDto, type RiskLevel } from "@/features/findings/types"

const findingRoles = ["Auditor", "AuditManager", "Administrator"]

const riskBadgeVariant: Record<RiskLevel, "outline" | "secondary" | "destructive"> = {
  Low: "outline",
  Medium: "secondary",
  High: "destructive",
  Critical: "destructive",
}

const PAGE_SIZE = 15

export function FindingsPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState("")
  const [riskFilter, setRiskFilter] = useState("")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<FindingDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<FindingDto | null>(null)
  const [page, setPage] = useState(1)

  const canManage = user?.roles.some((r) => findingRoles.includes(r)) ?? false

  const findings = useQuery({
    queryKey: ["findings", search, riskFilter],
    queryFn: () =>
      findingsApi.list({
        search: search || undefined,
        riskLevel: riskFilter ? (riskFilter as RiskLevel) : undefined,
      }),
  })
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments })
  const auditPlans = useQuery({ queryKey: ["audits"], queryFn: () => auditApi.list() })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["findings"] })

  const create = useMutation({
    mutationFn: findingsApi.create,
    onSuccess: () => {
      invalidate()
      setDialogOpen(false)
      setEditing(null)
    },
  })
  const update = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: Parameters<typeof findingsApi.update>[1] }) =>
      findingsApi.update(id, payload),
    onSuccess: () => {
      invalidate()
      setDialogOpen(false)
      setEditing(null)
    },
  })
  const remove = useMutation({
    mutationFn: findingsApi.remove,
    onSuccess: () => {
      invalidate()
      setDeleteTarget(null)
    },
  })

  const handleSubmit = (payload: Parameters<typeof findingsApi.create>[0]) =>
    editing ? update.mutateAsync({ id: editing.id, payload }) : create.mutateAsync(payload)

  // Reset page when filters change
  const handleSearch = (v: string) => { setSearch(v); setPage(1) }
  const handleRisk = (v: string) => { setRiskFilter(v); setPage(1) }

  const allFindings = findings.data ?? []
  const paged = allFindings.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-medium">Temuan Audit</h1>
          <p className="text-sm text-muted-foreground">
            Catatan temuan, tingkat risiko, dan bukti ber-versi.
          </p>
        </div>
        {canManage && (
          <Button
            onClick={() => {
              setEditing(null)
              setDialogOpen(true)
            }}
          >
            <Plus data-icon="inline-start" />
            Buat Temuan
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <Input
          placeholder="Cari judul / kategori / deskripsi..."
          value={search}
          onChange={(e) => handleSearch(e.target.value)}
          className="max-w-xs"
        />
        <div className="w-44">
          <Select value={riskFilter} onChange={(e) => handleRisk(e.target.value)}>
            <option value="">Semua Risiko</option>
            {(Object.keys(RiskLevelLabels) as RiskLevel[]).map((r) => (
              <option key={r} value={r}>
                {RiskLevelLabels[r]}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileWarning className="size-5" />
            {findings.data?.length ?? 0} Temuan
          </CardTitle>
        </CardHeader>
        <CardContent className="grid gap-3">
          <div className="overflow-x-auto">
            {findings.isLoading ? (
              <div className="grid gap-2 p-2">
                {Array.from({ length: 6 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full rounded-md" />
                ))}
              </div>
            ) : !findings.data || findings.data.length === 0 ? (
              <p className="p-6 text-center text-muted-foreground">Belum ada temuan.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Judul</TableHead>
                    <TableHead>Departemen</TableHead>
                    <TableHead>Kategori</TableHead>
                    <TableHead>Risiko</TableHead>
                    <TableHead>Tenggat</TableHead>
                    <TableHead>Bukti</TableHead>
                    <TableHead className="text-right">Aksi</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {paged.map((f) => (
                    <TableRow key={f.id}>
                      <TableCell>
                        <Link to={`/findings/${f.id}`} className="font-medium hover:text-primary">
                          {f.title}
                        </Link>
                      </TableCell>
                      <TableCell>{f.departmentName ?? "—"}</TableCell>
                      <TableCell>{f.category ?? "—"}</TableCell>
                      <TableCell>
                        <Badge variant={riskBadgeVariant[f.riskLevel]}>
                          {RiskLevelLabels[f.riskLevel]}
                        </Badge>
                      </TableCell>
                      <TableCell className="whitespace-nowrap text-sm">
                        {f.dueDate ? new Date(f.dueDate).toLocaleDateString() : "—"}
                      </TableCell>
                      <TableCell>{f.evidences.length}</TableCell>
                      <TableCell>
                        <div className="flex items-center justify-end gap-1">
                          {canManage && (
                            <>
                              <Button
                                variant="ghost"
                                size="icon"
                                title="Ubah"
                                onClick={() => {
                                  setEditing(f)
                                  setDialogOpen(true)
                                }}
                              >
                                <Pencil className="size-4" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                title="Hapus"
                                onClick={() => setDeleteTarget(f)}
                              >
                                <Trash2 className="size-4 text-destructive" />
                              </Button>
                            </>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>
          <Pagination
            page={page}
            total={allFindings.length}
            pageSize={PAGE_SIZE}
            onPageChange={setPage}
          />
        </CardContent>
      </Card>

      <FindingFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        finding={editing}
        departments={departments.data ?? []}
        auditPlans={auditPlans.data ?? []}
        onSubmit={handleSubmit}
      />

      <AlertDialog
        open={!!deleteTarget}
        title="Hapus temuan?"
        description={deleteTarget ? `Tindakan ini tidak bisa dibatalkan: "${deleteTarget.title}"` : undefined}
        confirmLabel="Hapus"
        destructive
        isPending={remove.isPending}
        onConfirm={() => { if (deleteTarget) remove.mutate(deleteTarget.id) }}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}