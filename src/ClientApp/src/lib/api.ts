import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios"

import { authApi } from "@/features/auth/auth-api"
import type { AuthResponse } from "@/features/auth/types"

const ACCESS_KEY = "iams.accessToken"
const REFRESH_KEY = "iams.refreshToken"
const USER_KEY = "iams.user"

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PaginationParams {
  page?: number
  pageSize?: number
}

export const tokenStore = {
  get accessToken() {
    return localStorage.getItem(ACCESS_KEY)
  },
  get refreshToken() {
    return localStorage.getItem(REFRESH_KEY)
  },
  set(auth: AuthResponse) {
    localStorage.setItem(ACCESS_KEY, auth.accessToken)
    if (auth.refreshToken) {
      localStorage.setItem(REFRESH_KEY, auth.refreshToken)
    } else {
      localStorage.removeItem(REFRESH_KEY)
    }
    localStorage.setItem(USER_KEY, JSON.stringify(auth.user))
  },
  clear() {
    localStorage.removeItem(ACCESS_KEY)
    localStorage.removeItem(REFRESH_KEY)
    localStorage.removeItem(USER_KEY)
  },
  getStoredUser(): AuthResponse["user"] | null {
    const raw = localStorage.getItem(USER_KEY)
    if (!raw) return null
    try {
      return JSON.parse(raw) as AuthResponse["user"]
    } catch {
      return null
    }
  },
}

export const api = axios.create({
  baseURL: "/api",
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
})

api.interceptors.request.use((config) => {
  const token = tokenStore.accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

interface RetryRequestConfig extends InternalAxiosRequestConfig {
  _retried?: boolean
}

let refreshPromise: Promise<string> | null = null

function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = authApi
      .refresh(tokenStore.refreshToken ?? "")
      .then((data) => {
        tokenStore.set(data)
        return data.accessToken
      })
      .finally(() => {
        refreshPromise = null
      })
  }
  return refreshPromise
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetryRequestConfig | undefined
    const isAuthRequest = config?.url?.startsWith("/auth/") ?? false
    const isUnauthorized = error.response?.status === 401
    const canRefresh = Boolean(tokenStore.accessToken) || Boolean(tokenStore.refreshToken)

    if (config && isUnauthorized && !config._retried && !isAuthRequest && canRefresh) {
      config._retried = true
      try {
        const newToken = await refreshAccessToken()
        config.headers.Authorization = `Bearer ${newToken}`
        return api(config)
      } catch (refreshError) {
        tokenStore.clear()
        if (typeof window !== "undefined") {
          window.location.href = "/login"
        }
        return Promise.reject(refreshError)
      }
    }

    if (isUnauthorized && isAuthRequest && config?.url === "/auth/refresh") {
      tokenStore.clear()
      if (typeof window !== "undefined") {
        window.location.href = "/login"
      }
    }

    return Promise.reject(error)
  },
)