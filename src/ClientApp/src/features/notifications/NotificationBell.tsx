import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Bell, BellOff, CheckCheck } from "lucide-react"
import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { connectNotificationsHub, disconnectNotificationsHub } from "@/lib/notifications-signalr"
import { cn } from "@/lib/utils"
import { notificationsApi } from "@/features/notifications/notifications-api"

export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const notifications = useQuery({
    queryKey: ["notifications"],
    queryFn: notificationsApi.list,
  })

  const unread = useQuery({
    queryKey: ["notifications-unread"],
    queryFn: notificationsApi.unreadCount,
  })

  useEffect(() => {
    const connection = connectNotificationsHub()

    const refresh = () => {
      void queryClient.invalidateQueries({ queryKey: ["notifications"] })
      void queryClient.invalidateQueries({ queryKey: ["notifications-unread"] })
    }

    connection.on("NotificationReceived", refresh)

    return () => {
      connection.off("NotificationReceived", refresh)
      disconnectNotificationsHub()
    }
  }, [queryClient])

  const markRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["notifications"] })
      void queryClient.invalidateQueries({ queryKey: ["notifications-unread"] })
    },
  })

  const markAllRead = useMutation({
    mutationFn: notificationsApi.markAllRead,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["notifications"] })
      void queryClient.invalidateQueries({ queryKey: ["notifications-unread"] })
    },
  })

  const handleOpen = (id: string, link: string | null) => {
    if (!notifications.data?.some((n) => n.id === id && n.isRead)) {
      markRead.mutate(id)
    }
    setOpen(false)
    if (link) navigate(link)
  }

  const unreadCount = unread.data ?? 0

  return (
    <div className="relative">
      <Button
        variant="ghost"
        size="icon-sm"
        onClick={() => setOpen((v) => !v)}
        aria-label="Notifikasi"
        className="relative"
      >
        <Bell />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 flex min-w-4 h-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-medium text-destructive-foreground">
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </Button>

      {open && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} />
          <div className="absolute right-0 z-50 mt-2 w-80 overflow-hidden rounded-lg border bg-background shadow-lg">
            <div className="flex items-center justify-between border-b px-3 py-2">
              <span className="text-sm font-medium">Notifikasi</span>
              {notifications.data?.some((n) => !n.isRead) && (
                <button
                  type="button"
                  className="inline-flex items-center gap-1 text-xs text-primary hover:underline"
                  onClick={() => markAllRead.mutate()}
                >
                  <CheckCheck />
                  Tandai semua dibaca
                </button>
              )}
            </div>

            <div className="max-h-96 overflow-auto">
              {notifications.isLoading ? (
                <p className="p-6 text-center text-sm text-muted-foreground">Memuat...</p>
              ) : (notifications.data ?? []).length === 0 ? (
                <p className="flex flex-col items-center gap-2 p-6 text-center text-sm text-muted-foreground">
                  <BellOff className="size-5" />
                  Belum ada notifikasi.
                </p>
              ) : (
                (notifications.data ?? []).map((notification) => (
                  <button
                    key={notification.id}
                    type="button"
                    className={cn(
                      "flex w-full items-start gap-2 border-b px-3 py-2 text-left transition-colors last:border-b-0 hover:bg-muted",
                      !notification.isRead && "bg-primary/5"
                    )}
                    onClick={() => handleOpen(notification.id, notification.link)}
                  >
                    <span
                      className={cn(
                        "mt-1.5 size-2 shrink-0 rounded-full",
                        notification.isRead ? "bg-transparent" : "bg-primary"
                      )}
                    />
                    <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                      <span className="text-sm leading-snug text-foreground">
                        {notification.title}
                      </span>
                      {notification.message && (
                        <span className="text-xs text-muted-foreground line-clamp-2">
                          {notification.message}
                        </span>
                      )}
                      <span className="text-xs text-muted-foreground">
                        {new Date(notification.createdAt).toLocaleString()}
                      </span>
                    </span>
                  </button>
                ))
              )}
            </div>
          </div>
        </>
      )}
    </div>
  )
}