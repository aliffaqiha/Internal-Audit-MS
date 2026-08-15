/**
 * AlertDialog — reusable confirmation dialog.
 * Replaces all native window.confirm() calls across the app.
 *
 * Usage:
 *   const [target, setTarget] = useState<{ id: string; label: string } | null>(null)
 *   <AlertDialog
 *     open={!!target}
 *     title="Hapus temuan?"
 *     description={`Tindakan ini tidak bisa dibatalkan: "${target?.label}"`}
 *     confirmLabel="Hapus"
 *     destructive
 *     onConfirm={() => { remove.mutate(target!.id); setTarget(null) }}
 *     onCancel={() => setTarget(null)}
 *   />
 */

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface AlertDialogProps {
  open: boolean
  title: string
  description?: string
  confirmLabel?: string
  cancelLabel?: string
  /** When true, the confirm button is rendered as destructive (red) */
  destructive?: boolean
  isPending?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export function AlertDialog({
  open,
  title,
  description,
  confirmLabel = "Ya, lanjutkan",
  cancelLabel = "Batal",
  destructive = false,
  isPending = false,
  onConfirm,
  onCancel,
}: AlertDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onCancel() }}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {description && <DialogDescription>{description}</DialogDescription>}
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isPending}>
            {cancelLabel}
          </Button>
          <Button
            variant={destructive ? "destructive" : "default"}
            onClick={onConfirm}
            disabled={isPending}
          >
            {isPending ? "Memproses..." : confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
