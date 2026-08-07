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
import type { DepartmentDto } from "@/features/admin/types"

const schema = z.object({
  name: z.string().min(1, "Nama departemen wajib diisi").max(100),
  code: z.string().min(1, "Kode wajib diisi").max(10),
})

type FormValues = z.infer<typeof schema>

interface DepartmentFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  department: DepartmentDto | null
  onSubmit: (payload: { name: string; code: string }) => Promise<void>
}

export function DepartmentFormDialog({
  open,
  onOpenChange,
  department,
  onSubmit,
}: DepartmentFormDialogProps) {
  const isEdit = department !== null
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { name: "", code: "" } })

  useEffect(() => {
    if (open) {
      reset({ name: department?.name ?? "", code: department?.code ?? "" })
    }
  }, [open, department, reset])

  const onFormSubmit = async (values: FormValues) => {
    try {
      await onSubmit(values)
      onOpenChange(false)
    } catch (err) {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data
        ?.message
      setError("root", { message: message ?? "Terjadi kesalahan. Silakan coba lagi." })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? "Edit Departemen" : "Tambah Departemen"}</DialogTitle>
          <DialogDescription>
            {isEdit
              ? `Perbarui detail ${department?.name}.`
              : "Tambahkan departemen baru untuk asosiasi user & auditee."}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onFormSubmit)} noValidate className="grid gap-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="grid grid-cols-subgrid col-span-2 gap-2">
              <Label htmlFor="name">Nama</Label>
              <Input id="name" {...register("name")} autoFocus />
              {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
            </div>
            <div className="grid grid-cols-subgrid col-span-2 gap-2">
              <Label htmlFor="code">Kode</Label>
              <Input id="code" {...register("code")} placeholder="cth: FIN, HR, IT" />
              {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
            </div>
          </div>
          {errors.root && <p className="text-sm text-destructive">{errors.root.message}</p>}
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
              {isSubmitting ? "Menyimpan..." : "Simpan"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}