import { lazy, Suspense } from "react"
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom"

import { AdminRoute } from "@/components/AdminRoute"
import { ProtectedRoute } from "@/components/ProtectedRoute"
import { Toaster } from "@/components/ui/toast"
import { AdminLayout } from "@/features/admin/AdminLayout"
import { AuthProvider } from "@/features/auth/auth-context"
import { ForgotPasswordPage } from "@/features/auth/ForgotPasswordPage"
import { LoginPage } from "@/features/auth/LoginPage"
import { ResetPasswordPage } from "@/features/auth/ResetPasswordPage"

const DashboardPage = lazy(() =>
  import("@/features/dashboard/DashboardPage").then((m) => ({ default: m.DashboardPage }))
)
const AuditPlansPage = lazy(() =>
  import("@/features/audit/AuditPlansPage").then((m) => ({ default: m.AuditPlansPage }))
)
const AuditPlanDetailPage = lazy(() =>
  import("@/features/audit/AuditPlanDetailPage").then((m) => ({ default: m.AuditPlanDetailPage }))
)
const FindingsPage = lazy(() =>
  import("@/features/findings/FindingsPage").then((m) => ({ default: m.FindingsPage }))
)
const FindingDetailPage = lazy(() =>
  import("@/features/findings/FindingDetailPage").then((m) => ({ default: m.FindingDetailPage }))
)
const CapsPage = lazy(() =>
  import("@/features/caps/CapsPage").then((m) => ({ default: m.CapsPage }))
)
const UsersPage = lazy(() =>
  import("@/features/admin/UsersPage").then((m) => ({ default: m.UsersPage }))
)
const DepartmentsPage = lazy(() =>
  import("@/features/admin/DepartmentsPage").then((m) => ({ default: m.DepartmentsPage }))
)
const AuditLogsPage = lazy(() =>
  import("@/features/audit-logs/AuditLogsPage").then((m) => ({ default: m.AuditLogsPage }))
)

function PageLoader() {
  return <p className="p-10 text-center text-muted-foreground">Memuat...</p>
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route element={<ProtectedRoute />}>
            <Route element={<AdminLayout />}>
              <Route
                path="/"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <DashboardPage />
                  </Suspense>
                }
              />
              <Route
                path="/audits"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <AuditPlansPage />
                  </Suspense>
                }
              />
              <Route
                path="/audits/:id"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <AuditPlanDetailPage />
                  </Suspense>
                }
              />
              <Route
                path="/findings"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <FindingsPage />
                  </Suspense>
                }
              />
              <Route
                path="/findings/:id"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <FindingDetailPage />
                  </Suspense>
                }
              />
              <Route
                path="/caps"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <CapsPage />
                  </Suspense>
                }
              />
              <Route
                path="/admin/users"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <AdminRoute />
                    <UsersPage />
                  </Suspense>
                }
              />
              <Route
                path="/admin/departments"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <AdminRoute />
                    <DepartmentsPage />
                  </Suspense>
                }
              />
              <Route
                path="/admin/audit-logs"
                element={
                  <Suspense fallback={<PageLoader />}>
                    <AdminRoute />
                    <AuditLogsPage />
                  </Suspense>
                }
              />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
        <Toaster />
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App