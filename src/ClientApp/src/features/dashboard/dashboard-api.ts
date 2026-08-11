import { api } from "@/lib/api"

import type { DashboardAnalyticsDto } from "./types"

export const dashboardApi = {
  analytics: () =>
    api.get<DashboardAnalyticsDto>("/dashboard/analytics").then((r) => r.data),
}
