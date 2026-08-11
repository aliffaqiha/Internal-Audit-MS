import { api } from "@/lib/api"

import type { NotificationDto } from "./types"

export const notificationsApi = {
  list: () => api.get<NotificationDto[]>("/notifications").then((r) => r.data),

  unreadCount: () => api.get<number>("/notifications/unread-count").then((r) => r.data),

  markRead: (id: string) => api.patch(`/notifications/${id}/read`).then(() => undefined),

  markAllRead: () => api.patch("/notifications/read-all").then(() => undefined),
}