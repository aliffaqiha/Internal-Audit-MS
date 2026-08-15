import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { CheckCircle2, ClipboardCheck, Pencil, Send, XCircle } from "lucide-react"
import { useState } from "react"
import { Link } from "react-router-dom"

import { AlertDialog } from "@/components/ui/alert-dialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Pagination } from "@/components/ui/pagination"
import { RejectDialog } from "@/components/ui/reject-dialog"
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
import { CapFormDialog } from "@/features/caps/CapFormDialog"
import { capsApi } from "@/features/caps/caps-api"
import { CapStatusLabels, type CapStatus, type CorrectiveActionDto } from "@/features/caps/types"

const editorRoles = ["Auditee", "Auditor", "AuditManager", "Administrator"]
const verifierRoles = ["Auditor", "AuditManager", "Administrator"]

const statusVariant: Record<CapStatus, "outline" | "secondary" | "destructive" | "default"> = {
  Open: "outline",
  InProgress: "secondary",
  PendingVerification: "destructive",
  Closed: "default",
}

const PAGE_SIZE = 15

export function CapsPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [statusFilter, setStatusFilter] = useState("")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<CorrectiveActionDto | null>(null)
  const [page, setPage] = useState(1)

  // Dialog states replacing confirm() / prompt()
  const [verifyTarget, setVerifyTarget] = useState<CorrectiveActionDto | null>(null)
  const [rejectTarget, setRejectTarget] = useState<CorrectiveActionDto | null>(null)

  const isEditor = user?.roles.some((r) => editorRoles.includes(r)) ?? false
  const isVerifier = user?.roles.some((r) => verifierRoles.includes(r)) ?? false

  const caps = useQuery({
    queryKey: ["caps", statusFilter, page],
    queryFn: () =>
      capsApi.list({
        status: statusFilter ? (statusFilter as CapStatus) : undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["caps"] })

  const run = async (fn: () => Promise<unknown>, successMsg = "Aksi berhasil diproses.") => {
    try {
      await fn()
      invalidate()
      toast.success(successMsg)
    } catch (err) {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data
        ?.message
      toast.error(message ?? "Terjadi kesalahan.")
    }
  }

  const update = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: Parameters<typeof capsApi.update>[1] }) =>
      capsApi.update(id, payload),
    onSuccess: () => {
      invalidate()
      setDialogOpen(false)
      toast.success("CAP berhasil diperbarui")
    },
    onError: (err) => {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      toast.error(message ?? "Gagal memperbarui CAP.")
    },
  })

  const verifyMutation = useMutation({
    mutationFn: ({ id, approve, note }: { id: string; approve: boolean; note: string | null }) =>
      capsApi.verify(id, { approve, note }),
    onSuccess: (_, variables) => {
      invalidate()
      setVerifyTarget(null)
      setRejectTarget(null)
      if (variables.approve) {
        toast.success("CAP berhasil disetujui dan ditutup")
      } else {
        toast.warning("CAP ditolak dan dibuka kembali untuk perbaikan")
      }
    },
    onError: (err) => {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      toast.error(message ?? "Gagal memproses verifikasi CAP.")
    },
  })

  const handleSubmit = (values: { action: string; picName: string | null; targetDate: string | null; progress: number }) =>
    update.mutateAsync({ id: editing!.id, payload: values })

  const handleStatusFilter = (v: string) => { setStatusFilter(v); setPage(1) }

  const items = caps.data?.items ?? []
  const totalCount = caps.data?.totalCount ?? 0

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-medium">Rencana Tindak Lanjut (CAP)</h1>
          <p className="text-sm text-muted-foreground">
            {isEditor && isVerifier
              ? "Kelola CAP dari pengisian auditee hingga verifikasi auditor."
              : isVerifier
                ? "Antrian CAP yang menunggu verifikasi."
                : "Kelola tindak lanjut temuan Anda."}
          </p>
        </div>
        <div className="w-48">
          <Select value={statusFilter} onChange={(e) => handleStatusFilter(e.target.value)}>
            <option value="">Semua Status</option>
            {(Object.keys(CapStatusLabels) as CapStatus[]).map((s) => (
              <option key={s} value={s}>
                {CapStatusLabels[s]}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ClipboardCheck className="size-5" />
            {totalCount} CAP
          </CardTitle>
        </CardHeader>
        <CardContent className="grid gap-3">
          <div className="overflow-x-auto">
            {caps.isLoading ? (
              <div className="grid gap-2 p-2">
                {Array.from({ length: 6 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full rounded-md" />
                ))}
              </div>
            ) : !caps.data || items.length === 0 ? (
              <p className="p-6 text-center text-muted-foreground">Belum ada CAP.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Temuan</TableHead>
                    <TableHead>Tindak Lanjut</TableHead>
                    <TableHead>PIC</TableHead>
                    <TableHead>Target</TableHead>
                    <TableHead>Progres</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="text-right">Aksi</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((c) => (
                    <TableRow key={c.id}>
                      <TableCell className="max-w-48">
                        <Link
                          to={`/findings/${c.findingId}`}
                          className="line-clamp-2 font-medium hover:text-primary"
                        >
                          {c.findingTitle}
                        </Link>
                      </TableCell>
                      <TableCell className="max-w-md">
                        <span className="line-clamp-2">{c.action}</span>
                        {c.rejectionReason && (
                          <span className="mt-1 block text-xs text-destructive">
                            Alasan tolak: {c.rejectionReason}
                          </span>
                        )}
                      </TableCell>
                      <TableCell>{c.picName ?? "—"}</TableCell>
                      <TableCell className="whitespace-nowrap text-sm">
                        {c.targetDate ? new Date(c.targetDate).toLocaleDateString() : "—"}
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <div className="h-2 w-20 overflow-hidden rounded-full bg-muted">
                            <div
                              className="h-full bg-primary"
                              style={{ width: `${Math.min(100, c.progress)}%` }}
                            />
                          </div>
                          <span className="text-xs text-muted-foreground">{c.progress}%</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={statusVariant[c.status]}>{CapStatusLabels[c.status]}</Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center justify-end gap-1">
                          {isEditor && (c.status === "Open" || c.status === "InProgress") && (
                            <Button
                              variant="ghost"
                              size="icon"
                              title="Ubah"
                              onClick={() => {
                                setEditing(c)
                                setDialogOpen(true)
                              }}
                            >
                              <Pencil className="size-4" />
                            </Button>
                          )}
                          {isEditor && c.status === "Open" && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => run(() => capsApi.start(c.id))}
                            >
                              Mulai
                            </Button>
                          )}
                          {isEditor && c.status === "InProgress" && c.progress === 100 && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => run(() => capsApi.submit(c.id))}
                            >
                              <Send data-icon="inline-start" />
                              Ajukan
                            </Button>
                          )}
                          {isVerifier && c.status === "PendingVerification" && (
                            <>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setRejectTarget(c)}
                              >
                                <XCircle data-icon="inline-start" />
                                Tolak
                              </Button>
                              <Button
                                variant="default"
                                size="sm"
                                onClick={() => setVerifyTarget(c)}
                              >
                                <CheckCircle2 data-icon="inline-start" />
                                Setujui
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
            total={totalCount}
            pageSize={PAGE_SIZE}
            onPageChange={setPage}
          />
        </CardContent>
      </Card>

      <CapFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        cap={editing}
        onSubmit={handleSubmit}
      />

      {/* Approve confirm dialog */}
      <AlertDialog
        open={!!verifyTarget}
        title="Setujui dan tutup CAP?"
        description={
          verifyTarget
            ? `CAP akan ditandai Selesai dan tidak bisa diubah kembali.`
            : undefined
        }
        confirmLabel="Setujui"
        isPending={verifyMutation.isPending}
        onConfirm={() => {
          if (verifyTarget)
            verifyMutation.mutate({ id: verifyTarget.id, approve: true, note: null })
        }}
        onCancel={() => setVerifyTarget(null)}
      />

      {/* Reject reason dialog */}
      <RejectDialog
        open={!!rejectTarget}
        isPending={verifyMutation.isPending}
        onConfirm={(reason) => {
          if (rejectTarget)
            verifyMutation.mutate({ id: rejectTarget.id, approve: false, note: reason || null })
        }}
        onCancel={() => setRejectTarget(null)}
      />
    </div>
  )
}