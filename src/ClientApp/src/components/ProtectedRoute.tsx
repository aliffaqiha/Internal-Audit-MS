import { Navigate, Outlet, useLocation } from "react-router-dom"

import { useAuth } from "@/features/auth/auth-context"

export function ProtectedRoute() {
  const { isAuthenticated, user } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (user?.mustChangePassword && location.pathname !== "/change-password") {
    return <Navigate to="/change-password" replace />
  }

  return <Outlet />
}