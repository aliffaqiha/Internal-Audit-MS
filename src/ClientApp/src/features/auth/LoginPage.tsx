import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { useLocation, useNavigate } from "react-router-dom"
import { z } from "zod"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/features/auth/auth-context"

const schema = z.object({
  emailOrUsername: z.string().min(1, "Username atau email wajib diisi"),
  password: z.string().min(1, "Password wajib diisi"),
})

type LoginForm = z.infer<typeof schema>

const APIErrorMessage = new Map<number, string>([
  [401, "Username/email atau password salah."],
  [429, "Terlalu banyak percobaan. Coba lagi beberapa menit lagi."],
])

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: { pathname: string } } | null)?.from?.pathname ?? "/"

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: LoginForm) => {
    try {
      const user = await login(values.emailOrUsername, values.password)
      // A user who already changed their password must not be sent back to the
      // change-password screen (the ProtectedRoute may have recorded it as the
      // original destination when the session was cleared).
      const target = from === "/change-password" && !user.mustChangePassword ? "/" : from
      navigate(target, { replace: true })
    } catch (err) {
      const status = (err as { response?: { status?: number } })?.response?.status
      setError("root", {
        message: APIErrorMessage.get(status ?? 0) ?? "Terjadi kesalahan. Silakan coba lagi.",
      })
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-background p-6">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle className="text-xl">Masuk ke IAMS</CardTitle>
        </CardHeader>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <CardContent className="grid gap-4">
            <div className="grid gap-2">
              <Label htmlFor="emailOrUsername">Username / Email</Label>
              <Input id="emailOrUsername" {...register("emailOrUsername")} autoFocus />
              {errors.emailOrUsername && (
                <p className="text-sm text-destructive">{errors.emailOrUsername.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="password">Password</Label>
              <Input id="password" type="password" {...register("password")} />
              {errors.password && (
                <p className="text-sm text-destructive">{errors.password.message}</p>
              )}
            </div>
            {errors.root && (
              <p className="text-sm text-destructive">{errors.root.message}</p>
            )}
          </CardContent>
          <CardFooter className="flex-col gap-3">
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Memproses..." : "Masuk"}
            </Button>
            <a
              href="/forgot-password"
              className="text-sm text-muted-foreground hover:text-foreground"
            >
              Lupa password?
            </a>
          </CardFooter>
        </form>
      </Card>
    </main>
  )
}