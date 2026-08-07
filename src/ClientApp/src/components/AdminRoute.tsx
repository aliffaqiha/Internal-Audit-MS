import { Navigate, useLocation } from "react-router-dom"

import { useAuth } from "@/features/auth/auth-context"

export function AdminRoute() {
  const { user, isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (!user?.roles.includes("Administrator")) {
    return <Navigate to="/" replace />
  }

  return null
}