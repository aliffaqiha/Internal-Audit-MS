import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { CheckCircle2, ClipboardPlus, Download, Paperclip, Pencil, Send, Upload, XCircle } from "lucide-react"
import { useRef, useState } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useAuth } from "@/features/auth/auth-context"
import { CapFormDialog } from "@/features/caps/CapFormDialog"
import { capsApi } from "@/features/caps/caps-api"
import { CapStatusLabels, type CorrectiveActionDto } from "@/features/caps/types"

const editorRoles = ["Auditee", "Auditor", "AuditManager", "Administrator"]
const verifierRoles = ["Auditor", "AuditManager", "Administrator"]

const statusVariant: Record<CorrectiveActionDto["status"], "outline" | "secondary" | "destructive" | "default"> = {
  Open: "outline",
  InProgress: "secondary",
  PendingVerification: "destructive",
  Closed: "default",
}

export function CapSection({ findingId }: { findingId: string }) {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<CorrectiveActionDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  const isEditor = user?.roles.some((r) => editorRoles.includes(r)) ?? false
  const isVerifier = user?.roles.some((r) => verifierRoles.includes(r)) ?? false

  const cap = useQuery({
    queryKey: ["caps", "finding", findingId],
    queryFn: () => capsApi.getByFinding(findingId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["caps"] })

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

  const create = useMutation({
    mutationFn: capsApi.create,
    onSuccess: () => {
      invalidate()
      setDialogOpen(false)
    },
  })
  const update = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: Parameters<typeof capsApi.update>[1] }) =>
      capsApi.update(id, payload),
    onSuccess: () => {
      invalidate()
      setDialogOpen(false)
    },
  })

  const handleSubmit = (values: { action: string; picName: string | null; targetDate: string | null; progress: number }) =>
    editing ? update.mutateAsync({ id: editing.id, payload: values }) : create.mutateAsync({ ...values, findingId })

  const handleVerify = (approve: boolean) => {
    if (!cap.data) return
    if (approve && !confirm("Setujui dan tutup CAP ini?")) return
    const note = approve
      ? null
      : prompt("Alasan penolakan (CAP akan dibuka kembali)?", "")
    if (note === null) return
    run(() => capsApi.verify(cap.data!.id, { approve, note: approve ? null : note || null }))
  }

  const handleFile = (file: File | undefined) => {
    if (!file || !cap.data) return
    setError(null)
    run(() => capsApi.uploadAttachment(cap.data!.id, file)).then(() => {
      if (fileRef.current) fileRef.current.value = ""
    })
  }

  const data = cap.data
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ClipboardPlus className="size-5" />
          Rencana Tindak Lanjut (CAP)
          {data && <Badge variant={statusVariant[data.status]}>{CapStatusLabels[data.status]}</Badge>}
        </CardTitle>
      </CardHeader>
      <CardContent>
        {cap.isLoading ? (
          <p className="text-sm text-muted-foreground">Memuat...</p>
        ) : !data ? (
          <div className="grid gap-2">
            <p className="text-sm text-muted-foreground">
              Temuan ini belum memiliki rencana tindak lanjut.
            </p>
            {isEditor && (
              <Button
                className="justify-self-start"
                onClick={() => {
                  setEditing(null)
                  setDialogOpen(true)
                }}
              >
                Buat CAP
              </Button>
            )}
          </div>
        ) : (
          <div className="grid gap-4 text-sm">
            {error && <p className="text-destructive">{error}</p>}
            <div className="grid gap-1">
              <span className="font-medium">Tindak Lanjut</span>
              <span className="text-muted-foreground">{data.action}</span>
            </div>
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
              <div>
                <div className="font-medium">PIC</div>
                <div className="text-muted-foreground">{data.picName ?? "—"}</div>
              </div>
              <div>
                <div className="font-medium">Target</div>
                <div className="text-muted-foreground">
                  {data.targetDate ? new Date(data.targetDate).toLocaleDateString() : "—"}
                </div>
              </div>
              <div>
                <div className="font-medium">Progres</div>
                <div className="text-muted-foreground">{data.progress}%</div>
              </div>
              <div>
                <div className="font-medium">Verifikasi</div>
                <div className="text-muted-foreground">
                  {data.verifiedAt ? new Date(data.verifiedAt).toLocaleDateString() : "—"}
                </div>
              </div>
            </div>

            {data.progress < 100 && (
              <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                <div className="h-full bg-primary" style={{ width: `${data.progress}%` }} />
              </div>
            )}

            {data.rejectionReason && (
              <p className="rounded-md bg-destructive/10 p-2 text-destructive">
                Ditolak: {data.rejectionReason}
              </p>
            )}
            {data.verificationNote && data.status === "Closed" && (
              <p className="rounded-md bg-muted p-2 text-muted-foreground">
                Catatan verifikasi: {data.verificationNote}
              </p>
            )}

            {data.attachment && (
              <div className="flex items-center gap-2">
                <Paperclip className="size-4 text-muted-foreground" />
                <span className="truncate">{data.attachment.fileName}</span>
                <a href={capsApi.downloadUrl(data.id)}>
                  <Button variant="outline" size="sm">
                    <Download data-icon="inline-start" />
                    Unduh
                  </Button>
                </a>
              </div>
            )}

            <div className="flex flex-wrap gap-2">
              {isEditor && (data.status === "Open" || data.status === "InProgress") && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setEditing(data)
                    setDialogOpen(true)
                  }}
                >
                  <Pencil data-icon="inline-start" />
                  Ubah
                </Button>
              )}
              {isEditor && data.status === "Open" && (
                <Button variant="outline" size="sm" onClick={() => run(() => capsApi.start(data.id))}>
                  Mulai
                </Button>
              )}
              {isEditor && data.status === "InProgress" && data.progress === 100 && (
                <Button variant="outline" size="sm" onClick={() => run(() => capsApi.submit(data.id))}>
                  <Send data-icon="inline-start" />
                  Ajukan Verifikasi
                </Button>
              )}
              {isEditor && data.status !== "Closed" && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => fileRef.current?.click()}
                >
                  <Upload data-icon="inline-start" />
                  Lampirkan
                </Button>
              )}
              {isVerifier && data.status === "PendingVerification" && (
                <>
                  <Button variant="outline" size="sm" onClick={() => handleVerify(false)}>
                    <XCircle data-icon="inline-start" />
                    Tolak
                  </Button>
                  <Button variant="default" size="sm" onClick={() => handleVerify(true)}>
                    <CheckCircle2 data-icon="inline-start" />
                    Setujui & Tutup
                  </Button>
                </>
              )}
            </div>

            <input
              ref={fileRef}
              type="file"
              className="hidden"
              onChange={(e) => handleFile(e.target.files?.[0])}
            />
          </div>
        )}
      </CardContent>
      <CapFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        cap={editing}
        onSubmit={handleSubmit}
      />
    </Card>
  )
}