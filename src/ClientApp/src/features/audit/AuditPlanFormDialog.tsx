import { zodResolver } from "@hookform/resolvers/zod"
import { Plus, Trash2 } from "lucide-react"
import { useEffect } from "react"
import { useFieldArray, useForm } from "react-hook-form"
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
import type { AuditTeamMemberDto, CreateAuditPlanPayload } from "@/features/audit/types"
import { IT_TEMPLATE, StandardOptions } from "@/features/audit/types"

const checklistItemSchema = z.object({
  question: z.string().min(1, "Pertanyaan wajib diisi"),
  category: z.string().nullable(),
  isRequired: z.boolean(),
})

const assignmentSchema = z.object({
  userId: z.string().min(1, "Pilih anggota"),
  roleInPlan: z.string().nullable(),
})

const formSchema = z
  .object({
    title: z.string().min(1, "Judul wajib diisi").max(200),
    standard: z.string().nullable(),
    objective: z.string().max(1000).nullable(),
    scope: z.string().max(1000).nullable(),
    departmentId: z.string().nullable(),
    startDate: z.string().nullable(),
    endDate: z.string().nullable(),
    assignments: z.array(assignmentSchema),
    checklistItems: z.array(checklistItemSchema).min(1, "Minimal satu item checklist"),
  })
  .superRefine((data, ctx) => {
    if (data.startDate && data.endDate && data.endDate < data.startDate) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["endDate"],
        message: "Tanggal selesai harus setelah tanggal mulai",
      })
    }
  })

type FormValues = z.infer<typeof formSchema>

interface AuditPlanFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  departments: DepartmentDto[]
  team: AuditTeamMemberDto[]
  onSubmit: (payload: CreateAuditPlanPayload) => Promise<unknown>
}

export function AuditPlanFormDialog({
  open,
  onOpenChange,
  departments,
  team,
  onSubmit,
}: AuditPlanFormDialogProps) {
  const {
    register,
    control,
    handleSubmit,
    setValue,
    watch,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      title: "",
      standard: null,
      objective: null,
      scope: null,
      departmentId: null,
      startDate: null,
      endDate: null,
      assignments: [],
      checklistItems: [],
    },
  })

  const checklist = useFieldArray({ control, name: "checklistItems" })
  const assignments = useFieldArray({ control, name: "assignments" })

  const selectedStandard = watch("standard")

  useEffect(() => {
    if (open) reset()
  }, [open, reset])

  useEffect(() => {
    if (selectedStandard === "IT" && checklist.fields.length === 0) {
      setValue("checklistItems", IT_TEMPLATE.map((i) => ({ ...i })), {
        shouldDirty: true,
      })
    }
  }, [selectedStandard, checklist.fields.length, setValue])

  const onFormSubmit = async (values: FormValues) => {
    const payload: CreateAuditPlanPayload = {
      title: values.title,
      standard: values.standard || null,
      objective: values.objective || null,
      scope: values.scope || null,
      departmentId: values.departmentId || null,
      startDate: values.startDate ? new Date(values.startDate).toISOString() : null,
      endDate: values.endDate ? new Date(values.endDate).toISOString() : null,
      assignments: values.assignments.map((a) => ({
        userId: a.userId,
        roleInPlan: a.roleInPlan || null,
      })),
      checklistItems: values.checklistItems
        .filter((i) => i.question.trim() !== "")
        .map((i) => ({
          question: i.question.trim(),
          category: i.category || null,
          isRequired: i.isRequired,
        })),
    }
    try {
      await onSubmit(payload)
      onOpenChange(false)
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
          <DialogTitle>Buat Rencana Audit</DialogTitle>
          <DialogDescription>
            Rencana dibuat sebagai Draf. Dapat disubmit setelah lengkap.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onFormSubmit)} noValidate className="grid gap-4">
          <div className="grid max-h-[60vh] gap-4 overflow-y-auto pr-1">
            <div className="grid gap-2">
              <Label htmlFor="title">Judul</Label>
              <Input id="title" {...register("title")} autoFocus />
              {errors.title && <p className="text-sm text-destructive">{errors.title.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label htmlFor="standard">Standar</Label>
                <Select id="standard" {...register("standard")}>
                  <option value="">— Pilih —</option>
                  {StandardOptions.map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </Select>
                <p className="text-xs text-muted-foreground">
                  Pilih IT untuk mengisi template checklist otomatis.
                </p>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="departmentId">Departemen Diaudit</Label>
                <Select id="departmentId" {...register("departmentId")}>
                  <option value="">— Pilih —</option>
                  {departments.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name}
                    </option>
                  ))}
                </Select>
              </div>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="objective">Objektif</Label>
              <Textarea id="objective" rows={2} {...register("objective")} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="scope">Ruang Lingkup</Label>
              <Textarea id="scope" rows={2} {...register("scope")} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label htmlFor="startDate">Tanggal Mulai</Label>
                <Input id="startDate" type="date" {...register("startDate")} />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="endDate">Tanggal Selesai</Label>
                <Input id="endDate" type="date" {...register("endDate")} />
                {errors.endDate && (
                  <p className="text-sm text-destructive">{errors.endDate.message}</p>
                )}
              </div>
            </div>

            <div className="grid gap-2">
              <Label>Anggota Tim</Label>
              <div className="grid gap-2 rounded-md border p-3">
                {assignments.fields.map((field, index) => (
                  <div key={field.id} className="flex items-center gap-2">
                    <Select className="flex-1" {...register(`assignments.${index}.userId`)}>
                      <option value="">— Pilih anggota —</option>
                      {team.map((m) => (
                        <option key={m.userId} value={m.userId}>
                          {m.fullName} ({m.username})
                        </option>
                      ))}
                    </Select>
                    <Input
                      className="w-36"
                      placeholder="Peran (cth: Lead)"
                      {...register(`assignments.${index}.roleInPlan`)}
                    />
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => assignments.remove(index)}
                    >
                      <Trash2 />
                    </Button>
                  </div>
                ))}
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="justify-self-start"
                  onClick={() =>
                    assignments.append({ userId: "", roleInPlan: null })
                  }
                >
                  <Plus data-icon="inline-start" />
                  Tambah Anggota
                </Button>
              </div>
            </div>

            <div className="grid gap-2">
              <Label>Checklist</Label>
              <div className="grid gap-2 rounded-md border p-3">
                {checklist.fields.map((field, index) => (
                  <div key={field.id} className="grid grid-cols-[1fr_9rem_auto] items-center gap-2">
                    <Input
                      placeholder="Pertanyaan / kontrol yang diaudit"
                      {...register(`checklistItems.${index}.question`)}
                    />
                    <Input
                      placeholder="Kategori"
                      {...register(`checklistItems.${index}.category`)}
                    />
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => checklist.remove(index)}
                    >
                      <Trash2 />
                    </Button>
                  </div>
                ))}
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="justify-self-start"
                  onClick={() =>
                    checklist.append({ question: "", category: null, isRequired: true })
                  }
                >
                  <Plus data-icon="inline-start" />
                  Tambah Item
                </Button>
              </div>
              {errors.checklistItems && (
                <p className="text-sm text-destructive">{errors.checklistItems.message}</p>
              )}
            </div>

            {errors.root && <p className="text-sm text-destructive">{errors.root.message}</p>}
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Batal
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Menyimpan..." : "Buat Draf"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}