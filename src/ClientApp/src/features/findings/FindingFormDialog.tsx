import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { useForm } from "react-hook-form"
import { z } from "zod"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select } from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import type { DepartmentDto } from "@/features/admin/types"
import type { AuditPlanDto } from "@/features/audit/types"
import { RiskLevelLabels, type CreateFindingPayload, type FindingDto, type RiskLevel } from "@/features/findings/types"

const formSchema = z.object({
  title: z.string().min(1, "Judul temuan wajib diisi"),
  description: z.string().max(4000).nullable(),
  departmentId: z.string().nullable(),
  riskLevel: z.enum(["Low", "Medium", "High", "Critical"]),
  category: z.string().max(100).nullable(),
  recommendation: z.string().max(2000).nullable(),
  dueDate: z.string().nullable(),
  auditPlanId: z.union([z.string(), z.literal(""), z.null()]),
})

type FormValues = z.infer<typeof formSchema>

interface FindingFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  finding: FindingDto | null
  departments: DepartmentDto[]
  auditPlans: AuditPlanDto[]
  onSubmit: (payload: CreateFindingPayload) => Promise<unknown>
}

const toFormValues = (f: FindingDto | null): FormValues => ({
  title: f?.title ?? "",
  description: f?.description ?? "",
  departmentId: f?.departmentId ?? null,
  riskLevel: f?.riskLevel ?? "Medium",
  category: f?.category ?? "",
  recommendation: f?.recommendation ?? "",
  dueDate: f?.dueDate ? f.dueDate.slice(0, 10) : null,
  auditPlanId: f?.auditPlanId ?? null,
})

export function FindingFormDialog({
  open,
  onOpenChange,
  finding,
  departments,
  auditPlans,
  onSubmit,
}: FindingFormDialogProps) {
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: toFormValues(null),
  })

  useEffect(() => {
    if (open) reset(toFormValues(finding))
  }, [open, finding, reset])

  const onFormSubmit = async (values: FormValues) => {
    const payload: CreateFindingPayload = {
      title: values.title.trim(),
      description: values.description?.trim() || null,
      departmentId: values.departmentId || null,
      riskLevel: values.riskLevel as RiskLevel,
      category: values.category?.trim() || null,
      recommendation: values.recommendation?.trim() || null,
      dueDate: values.dueDate ? new Date(values.dueDate).toISOString() : null,
      auditPlanId: values.auditPlanId && values.auditPlanId !== "" ? values.auditPlanId : null,
    }
    try {
      await onSubmit(payload)
    } catch (err) {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data
        ?.message
      setError("root", { message: message ?? "Terjadi kesalahan. Silakan coba lagi." })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{finding ? "Ubah Temuan" : "Buat Temuan"}</DialogTitle>
          <DialogDescription>
            Catat temuan audit lengkap dengan tingkat risiko dan rekomendasi.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onFormSubmit)} noValidate className="grid gap-4">
          <div className="grid max-h-[60vh] gap-4 overflow-y-auto pr-1">
            <div className="grid gap-2">
              <Label htmlFor="title">Judul Temuan</Label>
              <Input id="title" {...register("title")} autoFocus />
              {errors.title && <p className="text-sm text-destructive">{errors.title.message}</p>}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="description">Deskripsi</Label>
              <Textarea id="description" rows={4} {...register("description")} />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="riskLevel">Tingkat Risiko</Label>
                <Select {...register("riskLevel")}>
                  {(Object.keys(RiskLevelLabels) as RiskLevel[]).map((r) => (
                    <option key={r} value={r}>
                      {RiskLevelLabels[r]}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="category">Kategori</Label>
                <Input id="category" {...register("category")} />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="departmentId">Departemen</Label>
                <Select {...register("departmentId")}>
                  <option value="">— Pilih —</option>
                  {departments.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="dueDate">Tenggat</Label>
                <Input id="dueDate" type="date" {...register("dueDate")} />
              </div>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="auditPlanId">Rencana Audit (opsional)</Label>
              <Select {...register("auditPlanId")}>
                <option value="">— Tanpa rencana —</option>
                {auditPlans.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.title}
                  </option>
                ))}
              </Select>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="recommendation">Rekomendasi</Label>
              <Textarea id="recommendation" rows={3} {...register("recommendation")} />
            </div>

            {errors.root && <p className="text-sm text-destructive">{errors.root.message}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Batal
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {finding ? "Simpan Perubahan" : "Buat Temuan"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}