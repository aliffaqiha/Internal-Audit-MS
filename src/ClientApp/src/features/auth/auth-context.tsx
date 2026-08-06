import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react"

import { authApi } from "@/features/auth/auth-api"
import type { AuthUser } from "@/features/auth/types"
import { tokenStore } from "@/lib/api"

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (emailOrUsername: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => tokenStore.getStoredUser())
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (!tokenStore.accessToken) {
      setUser(null)
    }
  }, [])

  const login = useCallback(async (emailOrUsername: string, password: string) => {
    setIsLoading(true)
    try {
      const data = await authApi.login(emailOrUsername, password)
      tokenStore.set(data)
      setUser(data.user)
    } finally {
      setIsLoading(false)
    }
  }, [])

  const logout = useCallback(async () => {
    const refresh = tokenStore.refreshToken
    tokenStore.clear()
    setUser(null)
    if (refresh) {
      try {
        await authApi.logout(refresh)
      } catch {
        // token may already be revoked; ignore
      }
    }
  }, [])

  return (
    <AuthContext.Provider
      value={{ user, isAuthenticated: Boolean(user), isLoading, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth must be used within AuthProvider")
  return ctx
}