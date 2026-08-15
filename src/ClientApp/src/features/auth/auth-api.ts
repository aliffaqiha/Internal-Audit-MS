import { api } from "@/lib/api"

import type { AuthResponse } from "./types"

export const authApi = {
  login: (emailOrUsername: string, password: string) =>
    api.post<AuthResponse>("/auth/login", { emailOrUsername, password }).then((r) => r.data),

  refresh: (refreshToken: string | null) =>
    api.post<AuthResponse>("/auth/refresh", { refreshToken }).then((r) => r.data),

  logout: (refreshToken: string | null) =>
    api.post("/auth/logout", { refreshToken }).then(() => undefined),

  forgotPassword: (email: string) =>
    api.post("/auth/forgot-password", { email }).then(() => undefined),

  resetPassword: (token: string, newPassword: string) =>
    api.post("/auth/reset-password", { token, newPassword }).then(() => undefined),

  changePassword: (currentPassword: string, newPassword: string) =>
    api.post("/auth/change-password", { currentPassword, newPassword }).then(() => undefined),
}