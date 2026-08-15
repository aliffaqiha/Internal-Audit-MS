/**
 * RejectDialog — replaces native window.prompt() for rejection reason input.
 * Used in CapsPage for the "Tolak" (Reject) action.
 *
 * Usage:
 *   const [rejectTarget, setRejectTarget] = useState<CorrectiveActionDto | null>(null)
 *   <RejectDialog
 *     open={!!rejectTarget}
 *     onConfirm={(reason) => { doReject(rejectTarget!, reason); setRejectTarget(null) }}
 *     onCancel={() => setRejectTarget(null)}
 *   />
 */

import { useEffect } from "react"
import { useForm } from "react-hook-form"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"

interface RejectDialogProps {
  open: boolean
  isPending?: boolean
  onConfirm: (reason: string) => void
  onCancel: () => void
}

export function RejectDialog({ open, isPending = false, onConfirm, onCancel }: RejectDialogProps) {
  const { register, handleSubmit, reset, formState: { errors } } = useForm<{ reason: string }>({
    defaultValues: { reason: "" },
  })

  useEffect(() => {
    if (open) reset({ reason: "" })
  }, [open, reset])

  const onSubmit = ({ reason }: { reason: string }) => {
    onConfirm(reason.trim())
  }

  return (
    <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onCancel() }}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Tolak CAP</DialogTitle>
          <DialogDescription>
            CAP akan dibuka kembali untuk diperbaiki oleh auditee. Berikan alasan penolakan yang jelas.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="grid gap-4">
          <div className="grid gap-2">
            <Label htmlFor="reason">Alasan penolakan</Label>
            <Textarea
              id="reason"
              rows={3}
              placeholder="Jelaskan alasan penolakan..."
              autoFocus
              {...register("reason")}
            />
            {errors.reason && (
              <p className="text-sm text-destructive">{errors.reason.message}</p>
            )}
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onCancel} disabled={isPending}>
              Batal
            </Button>
            <Button type="submit" variant="destructive" disabled={isPending}>
              {isPending ? "Memproses..." : "Tolak CAP"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
