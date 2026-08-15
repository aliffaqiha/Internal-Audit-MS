import { zodResolver } from "@hookform/resolvers/zod"
import { useState } from "react"
import { useForm } from "react-hook-form"
import { useNavigate } from "react-router-dom"
import { z } from "zod"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/features/auth/auth-context"

const schema = z
  .object({
    currentPassword: z.string().min(1, "Password saat ini wajib diisi"),
    newPassword: z
      .string()
      .min(8, "Minimal 8 karakter")
      .regex(/[A-Z]/, "Harus mengandung huruf besar")
      .regex(/[a-z]/, "Harus mengandung huruf kecil")
      .regex(/[0-9]/, "Harus mengandung angka"),
    confirmPassword: z.string().min(1, "Konfirmasi password wajib diisi"),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    path: ["confirmPassword"],
    message: "Password tidak sama",
  })

type ChangePasswordForm = z.infer<typeof schema>

export function ChangePasswordPage() {
  const { changePassword } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordForm>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: ChangePasswordForm) => {
    setError(null)
    try {
      await changePassword(values.currentPassword, values.newPassword)
      navigate("/login", { replace: true })
    } catch {
      setError("Gagal mengganti password. Periksa password saat ini dan coba lagi.")
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-background p-6">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle className="text-lg">Ganti Password</CardTitle>
          <CardDescription>
            Anda harus mengganti password default sebelum dapat melanjutkan.
          </CardDescription>
        </CardHeader>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <CardContent className="grid gap-4">
            <div className="grid gap-2">
              <Label htmlFor="currentPassword">Password Saat Ini</Label>
              <Input id="currentPassword" type="password" {...register("currentPassword")} autoFocus />
              {errors.currentPassword && (
                <p className="text-sm text-destructive">{errors.currentPassword.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="newPassword">Password Baru</Label>
              <Input id="newPassword" type="password" {...register("newPassword")} />
              {errors.newPassword && (
                <p className="text-sm text-destructive">{errors.newPassword.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="confirmPassword">Konfirmasi Password Baru</Label>
              <Input id="confirmPassword" type="password" {...register("confirmPassword")} />
              {errors.confirmPassword && (
                <p className="text-sm text-destructive">{errors.confirmPassword.message}</p>
              )}
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
          </CardContent>
          <CardFooter>
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Menyimpan..." : "Simpan Password Baru"}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </main>
  )
}
