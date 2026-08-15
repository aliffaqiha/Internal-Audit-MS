import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import { AuditPlanStatusLabels, type AuditPlanStatus } from "@/features/audit/types"

const statusStyles: Record<AuditPlanStatus, { className: string }> = {
  Draft: {
    className: "border-slate-300 bg-slate-100 text-slate-700 dark:border-slate-700 dark:bg-slate-800/60 dark:text-slate-300",
  },
  Submitted: {
    className: "border-amber-300 bg-amber-50 text-amber-800 dark:border-amber-800 dark:bg-amber-950/60 dark:text-amber-300",
  },
  Approved: {
    className: "border-blue-300 bg-blue-50 text-blue-800 dark:border-blue-800 dark:bg-blue-950/60 dark:text-blue-300",
  },
  InProgress: {
    className: "border-indigo-300 bg-indigo-50 text-indigo-800 dark:border-indigo-800 dark:bg-indigo-950/60 dark:text-indigo-300 font-medium",
  },
  Completed: {
    className: "border-emerald-300 bg-emerald-50 text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300 font-medium",
  },
}

export function AuditPlanStatusBadge({
  status,
  className,
}: {
  status: AuditPlanStatus
  className?: string
}) {
  const style = statusStyles[status] ?? statusStyles.Draft
  return (
    <Badge variant="outline" className={cn(style.className, className)}>
      {AuditPlanStatusLabels[status] ?? status}
    </Badge>
  )
}
