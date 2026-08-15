import { CheckCircle2, AlertCircle, Info, AlertTriangle, X } from "lucide-react"
import { useEffect, useState } from "react"
import { cn } from "@/lib/utils"

export type ToastType = "success" | "error" | "info" | "warning"

export interface ToastItem {
  id: string
  type: ToastType
  message: string
  description?: string
  duration?: number
}

type ToastListener = (toasts: ToastItem[]) => void

let toasts: ToastItem[] = []
const listeners: Set<ToastListener> = new Set()

function notify() {
  listeners.forEach((listener) => listener([...toasts]))
}

export const toast = {
  success(message: string, description?: string, duration = 4000) {
    toast.show({ type: "success", message, description, duration })
  },
  error(message: string, description?: string, duration = 5000) {
    toast.show({ type: "error", message, description, duration })
  },
  info(message: string, description?: string, duration = 4000) {
    toast.show({ type: "info", message, description, duration })
  },
  warning(message: string, description?: string, duration = 4500) {
    toast.show({ type: "warning", message, description, duration })
  },
  show(item: Omit<ToastItem, "id">) {
    const id = Math.random().toString(36).substring(2, 9)
    const newToast: ToastItem = { ...item, id }
    toasts = [...toasts, newToast]
    notify()

    if (item.duration !== 0) {
      setTimeout(() => {
        toast.dismiss(id)
      }, item.duration ?? 4000)
    }
  },
  dismiss(id: string) {
    toasts = toasts.filter((t) => t.id !== id)
    notify()
  },
}

const icons: Record<ToastType, typeof CheckCircle2> = {
  success: CheckCircle2,
  error: AlertCircle,
  info: Info,
  warning: AlertTriangle,
}

const toastStyles: Record<ToastType, { border: string; iconColor: string }> = {
  success: {
    border: "border-emerald-500/30 bg-emerald-50/90 text-emerald-950 dark:bg-emerald-950/80 dark:text-emerald-100 dark:border-emerald-800",
    iconColor: "text-emerald-600 dark:text-emerald-400",
  },
  error: {
    border: "border-red-500/30 bg-red-50/90 text-red-950 dark:bg-red-950/80 dark:text-red-100 dark:border-red-800",
    iconColor: "text-red-600 dark:text-red-400",
  },
  warning: {
    border: "border-amber-500/30 bg-amber-50/90 text-amber-950 dark:bg-amber-950/80 dark:text-amber-100 dark:border-amber-800",
    iconColor: "text-amber-600 dark:text-amber-400",
  },
  info: {
    border: "border-blue-500/30 bg-blue-50/90 text-blue-950 dark:bg-blue-950/80 dark:text-blue-100 dark:border-blue-800",
    iconColor: "text-blue-600 dark:text-blue-400",
  },
}

export function Toaster() {
  const [items, setItems] = useState<ToastItem[]>([])

  useEffect(() => {
    listeners.add(setItems)
    return () => {
      listeners.delete(setItems)
    }
  }, [])

  if (items.length === 0) return null

  return (
    <div
      aria-live="polite"
      aria-atomic="true"
      className="pointer-events-none fixed bottom-4 right-4 z-50 flex max-w-md w-full flex-col gap-2 p-4 sm:p-0"
    >
      {items.map((item) => {
        const Icon = icons[item.type]
        const style = toastStyles[item.type]

        return (
          <div
            key={item.id}
            role="alert"
            className={cn(
              "pointer-events-auto flex items-start gap-3 rounded-lg border p-3.5 shadow-lg backdrop-blur-sm transition-all duration-200 animate-in fade-in slide-in-from-bottom-3",
              style.border
            )}
          >
            <Icon className={cn("size-5 shrink-0 mt-0.5", style.iconColor)} />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium leading-tight">{item.message}</p>
              {item.description && (
                <p className="mt-1 text-xs opacity-90 leading-snug">{item.description}</p>
              )}
            </div>
            <button
              type="button"
              onClick={() => toast.dismiss(item.id)}
              className="shrink-0 rounded p-1 opacity-70 transition-opacity hover:opacity-100 focus:outline-none"
              aria-label="Tutup notifikasi"
            >
              <X className="size-4" />
            </button>
          </div>
        )
      })}
    </div>
  )
}
