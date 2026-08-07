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
import { Textarea } from "@/components/ui/textarea"
import type { CorrectiveActionDto } from "@/features/caps/types"

const formSchema = z.object({
  action: z.string().min(1, "Tindak lanjut wajib diisi"),
  picName: z.string().max(150).nullable(),
  targetDate: z.union([z.string(), z.null(), z.literal("")]),
  progress: z.number().min(0).max(100),
})

type FormValues = z.infer<typeof formSchema>

interface CapFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  cap: CorrectiveActionDto | null
  onSubmit: (values: { action: string; picName: string | null; targetDate: string | null; progress: number }) => Promise<unknown>
}

const toFormValues = (cap: CorrectiveActionDto | null): FormValues => ({
  action: cap?.action ?? "",
  picName: cap?.picName ?? "",
  targetDate: cap?.targetDate ? cap.targetDate.slice(0, 10) : null,
  progress: cap?.progress ?? 0,
})

export function CapFormDialog({ open, onOpenChange, cap, onSubmit }: CapFormDialogProps) {
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
    if (open) reset(toFormValues(cap))
  }, [open, cap, reset])

  const onFormSubmit = async (values: FormValues) => {
    try {
      await onSubmit({
        action: values.action.trim(),
        picName: values.picName?.trim() || null,
        targetDate: values.targetDate ? new Date(values.targetDate).toISOString() : null,
        progress: values.progress,
      })
      onOpenChange(false)
    } catch (err) {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data
        ?.message
      setError("root", { message: message ?? "Terjadi kesalahan. Silakan coba lagi." })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>{cap ? "Ubah CAP" : "Buat Rencana Tindak Lanjut (CAP)"}</DialogTitle>
          <DialogDescription>
            Isikan tindakan korektif, PIC, target tanggal, dan progres untuk mencari solusi.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onFormSubmit)} noValidate className="grid gap-4">
          <div className="grid gap-4">
            <div className="grid gap-2">
              <Label htmlFor="action">Tindak Lanjut</Label>
              <Textarea id="action" rows={3} {...register("action")} autoFocus />
              {errors.action && <p className="text-sm text-destructive">{errors.action.message}</p>}
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="picName">PIC</Label>
                <Input id="picName" {...register("picName")} placeholder="Nama penanggung jawab" />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="targetDate">Target Tanggal</Label>
                <Input id="targetDate" type="date" {...register("targetDate")} />
              </div>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="progress">Progres (%)</Label>
              <Input id="progress" type="number" min={0} max={100} {...register("progress", { valueAsNumber: true })} />
              {errors.progress && (
                <p className="text-sm text-destructive">{errors.progress.message}</p>
              )}
            </div>
            {errors.root && <p className="text-sm text-destructive">{errors.root.message}</p>}
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Batal
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {cap ? "Simpan Perubahan" : "Buat CAP"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}