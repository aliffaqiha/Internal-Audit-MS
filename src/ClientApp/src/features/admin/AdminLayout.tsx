import { Building2, ClipboardList, FileWarning, LayoutDashboard, LogOut, ShieldCheck, Users } from "lucide-react"
import { NavLink, Outlet, useNavigate } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { useAuth } from "@/features/auth/auth-context"
import { cn } from "@/lib/utils"

const plannerRoles = ["Auditor", "AuditManager", "Administrator"]
const adminRoles = ["Administrator"]

const allNavItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, roles: null },
  { to: "/audits", label: "Audit", icon: ClipboardList, roles: plannerRoles },
  { to: "/findings", label: "Temuan", icon: FileWarning, roles: plannerRoles },
  { to: "/admin/users", label: "Pengguna", icon: Users, roles: adminRoles },
  { to: "/admin/departments", label: "Departemen", icon: Building2, roles: adminRoles },
]

export function AdminLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const navItems = allNavItems.filter(
    (item) => !item.roles || user?.roles.some((r) => item.roles!.includes(r))
  )

  const handleLogout = async () => {
    await logout()
    navigate("/login", { replace: true })
  }

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="sticky top-0 z-40 border-b bg-background">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-6 px-4">
          <div className="flex items-center gap-2 font-medium">
            <ShieldCheck className="size-5" />
            <span>IAMS</span>
          </div>
          <nav className="flex items-center gap-1">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) =>
                  cn(
                    "flex items-center gap-2 rounded-md px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:bg-muted hover:text-foreground",
                    isActive && "bg-muted font-medium text-foreground"
                  )
                }
              >
                <item.icon className="size-4" />
                {item.label}
              </NavLink>
            ))}
          </nav>
          <div className="ml-auto flex items-center gap-3">
            <div className="hidden text-right text-xs leading-tight sm:block">
              <div className="font-medium text-foreground">{user?.fullName}</div>
              <div className="text-muted-foreground">{user?.roles.join(", ")}</div>
            </div>
            <Button variant="outline" size="sm" onClick={handleLogout}>
              <LogOut data-icon="inline-start" />
              Keluar
            </Button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl p-4 md:p-6">
        <Outlet />
      </main>
    </div>
  )
}