import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom"

import { AdminRoute } from "@/components/AdminRoute"
import { ProtectedRoute } from "@/components/ProtectedRoute"
import { AdminLayout } from "@/features/admin/AdminLayout"
import { DepartmentsPage } from "@/features/admin/DepartmentsPage"
import { UsersPage } from "@/features/admin/UsersPage"
import { AuditLogsPage } from "@/features/audit-logs/AuditLogsPage"
import { AuditPlanDetailPage } from "@/features/audit/AuditPlanDetailPage"
import { AuditPlansPage } from "@/features/audit/AuditPlansPage"
import { AuthProvider } from "@/features/auth/auth-context"
import { ForgotPasswordPage } from "@/features/auth/ForgotPasswordPage"
import { HomePage } from "@/features/auth/HomePage"
import { LoginPage } from "@/features/auth/LoginPage"
import { ResetPasswordPage } from "@/features/auth/ResetPasswordPage"
import { CapsPage } from "@/features/caps/CapsPage"
import { FindingDetailPage } from "@/features/findings/FindingDetailPage"
import { FindingsPage } from "@/features/findings/FindingsPage"

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
              <Route path="/" element={<HomePage />} />
              <Route path="/audits" element={<AuditPlansPage />} />
              <Route path="/audits/:id" element={<AuditPlanDetailPage />} />
              <Route path="/findings" element={<FindingsPage />} />
              <Route path="/findings/:id" element={<FindingDetailPage />} />
              <Route path="/caps" element={<CapsPage />} />
              <Route
                path="/admin/users"
                element={
                  <>
                    <AdminRoute />
                    <UsersPage />
                  </>
                }
              />
              <Route
                path="/admin/departments"
                element={
                  <>
                    <AdminRoute />
                    <DepartmentsPage />
                  </>
                }
              />
              <Route
                path="/admin/audit-logs"
                element={
                  <>
                    <AdminRoute />
                    <AuditLogsPage />
                  </>
                }
              />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App