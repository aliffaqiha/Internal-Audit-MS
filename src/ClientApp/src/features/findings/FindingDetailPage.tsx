import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { ArrowLeft, Download, FileText, Upload } from "lucide-react"
import { useRef, useState } from "react"
import { Link, useParams } from "react-router-dom"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
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
import { CapSection } from "@/features/caps/CapSection"
import { findingsApi } from "@/features/findings/findings-api"
import { RiskLevelLabels, type RiskLevel } from "@/features/findings/types"

const findingRoles = ["Auditor", "AuditManager", "Administrator"]

const riskBadgeVariant: Record<RiskLevel, "outline" | "secondary" | "destructive"> = {
  Low: "outline",
  Medium: "secondary",
  High: "destructive",
  Critical: "destructive",
}

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function FindingDetailPage() {
  const { id = "" } = useParams()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)
  const [uploadingFor, setUploadingFor] = useState<string | null>(null)

  const canManage = user?.roles.some((r) => findingRoles.includes(r)) ?? false

  const finding = useQuery({ queryKey: ["findings", id], queryFn: () => findingsApi.get(id) })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["findings"] })
    queryClient.invalidateQueries({ queryKey: ["findings", id] })
  }

  const upload = useMutation({
    mutationFn: ({ evidenceId, file }: { evidenceId: string; file: File }) =>
      findingsApi.uploadEvidence(evidenceId, file),
    onSuccess: () => {
      invalidate()
      toast.success("Bukti temuan berhasil diunggah!")
    },
    onError: (err) => {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      toast.error(message ?? "Upload bukti gagal.")
    },
  })

  const handleFile = (file: File | undefined) => {
    if (!file) return
    setUploadingFor(id)
    upload.mutate(
      { evidenceId: id, file },
      {
        onSettled: () => setUploadingFor(null),
      }
    )
    if (fileRef.current) fileRef.current.value = ""
  }

  const data = finding.data
  if (!data) {
    return <p className="p-6 text-center text-muted-foreground">Memuat...</p>
  }

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Link
          to="/findings"
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" />
          Kembali
        </Link>
        {canManage && (
          <Button onClick={() => fileRef.current?.click()}>
            <Upload data-icon="inline-start" />
            Unggah Bukti
          </Button>
        )}
        <input
          ref={fileRef}
          type="file"
          className="hidden"
          onChange={(e) => handleFile(e.target.files?.[0])}
        />
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <CardTitle className="text-lg">{data.title}</CardTitle>
            <Badge variant={riskBadgeVariant[data.riskLevel]}>
              Risiko {RiskLevelLabels[data.riskLevel]}
            </Badge>
          </div>
        </CardHeader>
        <CardContent className="grid gap-4 text-sm">
          <div className="grid gap-1">
            <span className="font-medium">Deskripsi</span>
            <span className="text-muted-foreground">{data.description ?? "—"}</span>
          </div>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <div>
              <div className="font-medium">Departemen</div>
              <div className="text-muted-foreground">{data.departmentName ?? "—"}</div>
            </div>
            <div>
              <div className="font-medium">Kategori</div>
              <div className="text-muted-foreground">{data.category ?? "—"}</div>
            </div>
            <div>
              <div className="font-medium">Tenggat</div>
              <div className="text-muted-foreground">
                {data.dueDate ? new Date(data.dueDate).toLocaleDateString() : "—"}
              </div>
            </div>
            <div>
              <div className="font-medium">Rencana Audit</div>
              <div className="truncate">
                {data.auditPlanId ? (
                  <Link
                    to={`/audits/${data.auditPlanId}`}
                    className="font-medium text-primary hover:underline inline-block truncate max-w-full"
                    title={data.auditPlanTitle ?? "Lihat Rencana Audit"}
                  >
                    {data.auditPlanTitle ?? "Lihat Rencana Audit"}
                  </Link>
                ) : (
                  <span className="text-muted-foreground">—</span>
                )}
              </div>
            </div>
          </div>
          {data.recommendation && (
            <div className="grid gap-1">
              <span className="font-medium">Rekomendasi</span>
              <span className="text-muted-foreground">{data.recommendation}</span>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>
            Bukti ({data.evidences.length})
            {canManage && (
              <span className="ml-2 text-xs font-normal text-muted-foreground">
                Setiap unggahan dicatat sebagai versi baru.
              </span>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {data.evidences.length === 0 ? (
            <p className="grid place-items-center gap-2 p-8 text-center text-muted-foreground">
              <FileText className="size-8" />
              <span>Belum ada bukti. Unggah file sebagai bukti temuan.</span>
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Versi</TableHead>
                  <TableHead>Nama File</TableHead>
                  <TableHead>Tipe</TableHead>
                  <TableHead>Ukuran</TableHead>
                  <TableHead>Diunggah</TableHead>
                  <TableHead className="text-right">Aksi</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.evidences.map((e) => (
                  <TableRow key={e.id}>
                    <TableCell>
                      <Badge variant="outline">v{e.version}</Badge>
                    </TableCell>
                    <TableCell className="max-w-xs truncate">{e.originalFileName}</TableCell>
                    <TableCell className="text-xs text-muted-foreground">{e.contentType}</TableCell>
                    <TableCell className="whitespace-nowrap">
                      {formatBytes(e.sizeBytes)}
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-xs">
                      {new Date(e.uploadedAt).toLocaleString()}
                    </TableCell>
                    <TableCell className="text-right">
                      <a href={findingsApi.downloadUrl(id, e.id)}>
                        <Button variant="outline" size="sm">
                          <Download data-icon="inline-start" />
                          Unduh
                        </Button>
                      </a>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
          {uploadingFor && <p className="pt-2 text-sm text-muted-foreground">Mengunggah...</p>}
        </CardContent>
      </Card>

      <CapSection findingId={data.id} />
    </div>
  )
}