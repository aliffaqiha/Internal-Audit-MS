import { LogOut } from "lucide-react"
import { useNavigate } from "react-router-dom"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { useAuth } from "@/features/auth/auth-context"

export function HomePage() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate("/login", { replace: true })
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 bg-background p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-xl">Selamat datang, {user?.fullName}</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 text-sm text-muted-foreground">
          <div className="grid gap-1">
            <span className="font-medium text-foreground">Email</span>
            <span>{user?.email}</span>
          </div>
          <div className="grid gap-1">
            <span className="font-medium text-foreground">Peran</span>
            <div className="flex flex-wrap gap-1">
              {user?.roles.map((role) => (
                <Badge key={role} variant="secondary">
                  {role}
                </Badge>
              ))}
            </div>
          </div>
        </CardContent>
        <CardFooter className="justify-end">
          <Button variant="outline" onClick={handleLogout}>
            <LogOut data-icon="inline-start" />
            Keluar
          </Button>
        </CardFooter>
      </Card>
    </main>
  )
}