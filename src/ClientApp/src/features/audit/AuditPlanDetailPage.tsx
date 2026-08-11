import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { ArrowLeft, CheckCircle2, Send, Rocket, Flag, FileText, Download } from "lucide-react"
import { useState } from "react"
import { Link, useParams } from "react-router-dom"

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
import { Input } from "@/components/ui/input"
import { useAuth } from "@/features/auth/auth-context"
import { auditApi } from "@/features/audit/audit-api"
import type {
  AuditPlanChecklistItemDto,
  ChecklistItemStatus,
} from "@/features/audit/types"
import { AuditPlanStatusLabels, ChecklistItemStatusLabels } from "@/features/audit/types"

const plannerRoles = ["Auditor", "AuditManager", "Administrator"]
const approverRoles = ["AuditManager", "Administrator"]

export function AuditPlanDetailPage() {
  const { id = "" } = useParams()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [noteDraft, setNoteDraft] = useState<Record<string, string>>({})

  const isPlanner = user?.roles.some((r) => plannerRoles.includes(r)) ?? false
  const isApprover = user?.roles.some((r) => approverRoles.includes(r)) ?? false

  const plan = useQuery({ queryKey: ["audits", id], queryFn: () => auditApi.get(id) })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["audits"] })
    queryClient.invalidateQueries({ queryKey: ["audits", id] })
  }

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
      invalidate()
    } catch (err) {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data
        ?.message
      setError(message ?? "Terjadi kesalahan.")
    }
  }

  const checklistUpdate = useMutation({
    mutationFn: ({
      itemId,
      status,
      note,
    }: {
      itemId: string
      status: ChecklistItemStatus
      note: string | null
    }) => auditApi.updateChecklistItem(id, itemId, { status, note }),
    onSuccess: invalidate,
  })

  const canHaveReport =
    plan.data != null && ["Approved", "InProgress", "Completed"].includes(plan.data.status)

  const reportMeta = useQuery({
    queryKey: ["audits", id, "report"],
    queryFn: () => auditApi.reportMeta(id),
    enabled: canHaveReport,
    retry: false,
  })

  const generateReport = useMutation({
    mutationFn: () => auditApi.generateReport(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["audits", id, "report"] })
    },
  })

  const downloadReport = useMutation({
    mutationFn: async () => {
      const meta = reportMeta.data
      if (!meta) return
      await auditApi.downloadReport(id, meta.fileName)
    },
  })

  const data = plan.data
  if (!data) {
    return <p className="p-6 text-center text-muted-foreground">Memuat...</p>
  }

  const handleStatusChange = (item: AuditPlanChecklistItemDto, status: ChecklistItemStatus) => {
    checklistUpdate.mutate({ itemId: item.id, status, note: noteDraft[item.id] ?? item.note })
  }

  const handleNoteBlur = (item: AuditPlanChecklistItemDto) => {
    checklistUpdate.mutate({ itemId: item.id, status: item.status, note: noteDraft[item.id] ?? null })
  }

  const actions: Record<string, { label: string; icon: typeof Send; action: () => void } | null> = {
    Draft: isPlanner
      ? { label: "Submit untuk Persetujuan", icon: Send, action: () => run(() => auditApi.submit(id)) }
      : null,
    Submitted: isApprover
      ? { label: "Setujui", icon: CheckCircle2, action: () => run(() => auditApi.approve(id, null)) }
      : null,
    Approved: isPlanner
      ? { label: "Mulai Audit", icon: Rocket, action: () => run(() => auditApi.start(id)) }
      : null,
    InProgress: isPlanner
      ? { label: "Selesaikan Audit", icon: Flag, action: () => run(() => auditApi.complete(id)) }
      : null,
    Completed: null,
  }

  const action = actions[data.status]

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Link
          to="/audits"
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" />
          Kembali
        </Link>
        <div className="flex items-center gap-2">
          {error && <p className="text-sm text-destructive">{error}</p>}
          {action && (
            <Button onClick={action.action} disabled={checklistUpdate.isPending}>
              <action.icon data-icon="inline-start" />
              {action.label}
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-2">
            <CardTitle className="text-lg">{data.title}</CardTitle>
            <Badge variant="outline">{AuditPlanStatusLabels[data.status]}</Badge>
          </div>
        </CardHeader>
        <CardContent className="grid gap-4 text-sm">
          <div className="grid gap-1">
            <span className="font-medium">Objektif</span>
            <span className="text-muted-foreground">{data.objective ?? "—"}</span>
          </div>
          <div className="grid gap-1">
            <span className="font-medium">Ruang Lingkup</span>
            <span className="text-muted-foreground">{data.scope ?? "—"}</span>
          </div>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <div>
              <div className="font-medium">Standar</div>
              <div className="text-muted-foreground">{data.standard ?? "—"}</div>
            </div>
            <div>
              <div className="font-medium">Departemen</div>
              <div className="text-muted-foreground">{data.departmentName ?? "—"}</div>
            </div>
            <div>
              <div className="font-medium">Mulai</div>
              <div className="text-muted-foreground">
                {data.startDate ? new Date(data.startDate).toLocaleDateString() : "—"}
              </div>
            </div>
            <div>
              <div className="font-medium">Selesai</div>
              <div className="text-muted-foreground">
                {data.endDate ? new Date(data.endDate).toLocaleDateString() : "—"}
              </div>
            </div>
          </div>
          <div className="grid gap-1">
            <span className="font-medium">Tim Audit</span>
            <div className="flex flex-wrap gap-1">
              {data.assignments.map((a) => (
                <Badge key={a.userId} variant="secondary">
                  {a.fullName}
                  {a.roleInPlan ? ` — ${a.roleInPlan}` : ""}
                </Badge>
              ))}
              {data.assignments.length === 0 && <span className="text-muted-foreground">—</span>}
            </div>
          </div>
        </CardContent>
      </Card>

      {canHaveReport && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileText className="size-4" />
              Laporan Audit
            </CardTitle>
          </CardHeader>
          <CardContent className="grid gap-3 text-sm">
            {reportMeta.data ? (
              <>
                <div className="grid gap-1">
                  <span className="font-medium">File</span>
                  <span className="text-muted-foreground">
                    {reportMeta.data.fileName} (
                    {(reportMeta.data.sizeBytes / 1024).toFixed(0)} kB)
                  </span>
                </div>
                <div className="grid gap-1">
                  <span className="font-medium">Dibuat</span>
                  <span className="text-muted-foreground">
                    {new Date(reportMeta.data.generatedAt).toLocaleString("id-ID")}
                  </span>
                </div>
              </>
            ) : (
              <p className="text-muted-foreground">
                Belum ada laporan untuk rencana audit ini. Laporan berisi ringkasan eksekutif,
                hasil checklist, temuan & rekomendasi, serta kesimpulan.
              </p>
            )}
            <div className="flex flex-wrap gap-2">
              {isPlanner && (
                <Button
                  onClick={() => generateReport.mutate()}
                  disabled={generateReport.isPending || reportMeta.isLoading}
                >
                  {generateReport.isPending ? "Membuat..." : reportMeta.data ? "Perbarui Laporan" : "Buat Laporan"}
                </Button>
              )}
              {reportMeta.data && (
                <Button
                  variant="outline"
                  onClick={() => downloadReport.mutate()}
                  disabled={downloadReport.isPending}
                >
                  <Download data-icon="inline-start" />
                  Unduh PDF
                </Button>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle>
            Checklist ({data.checklistItems.length} item)
            {data.status === "InProgress" && (
              <span className="ml-2 text-xs font-normal text-muted-foreground">
                Update status & catatan selama audit berjalan
              </span>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {data.checklistItems.length === 0 ? (
            <p className="p-6 text-center text-muted-foreground">Belum ada item checklist.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Kategori</TableHead>
                  <TableHead>Pertanyaan / Kontrol</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Catatan</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.checklistItems.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell className="whitespace-nowrap">{item.category ?? "—"}</TableCell>
                    <TableCell className="max-w-md">{item.question}</TableCell>
                    <TableCell>
                      {data.status === "InProgress" && isPlanner ? (
                        <Select
                          value={item.status}
                          className="w-32"
                          onChange={(e) => handleStatusChange(item, e.target.value as ChecklistItemStatus)}
                        >
                          {(["Pending", "Pass", "Fail", "NotApplicable"] as ChecklistItemStatus[]).map(
                            (s) => (
                              <option key={s} value={s}>
                                {ChecklistItemStatusLabels[s]}
                              </option>
                            )
                          )}
                        </Select>
                      ) : (
                        <Badge variant={item.status === "Fail" ? "destructive" : "outline"}>
                          {ChecklistItemStatusLabels[item.status]}
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell>
                      {data.status === "InProgress" && isPlanner ? (
                        <Input
                          defaultValue={item.note ?? ""}
                          placeholder="Catatan hasil audit"
                          onBlur={(e) => {
                            setNoteDraft((prev) => ({ ...prev, [item.id]: e.target.value }))
                            handleNoteBlur(item)
                          }}
                        />
                      ) : (
                        <span className="text-muted-foreground">{item.note ?? "—"}</span>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}