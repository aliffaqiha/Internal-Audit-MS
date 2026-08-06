import { zodResolver } from "@hookform/resolvers/zod"
import { useState } from "react"
import { useForm } from "react-hook-form"
import { Link } from "react-router-dom"
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
import { authApi } from "@/features/auth/auth-api"

const schema = z.object({
  email: z.string().min(1, "Email wajib diisi").email("Email tidak valid"),
})

type ForgotForm = z.infer<typeof schema>

export function ForgotPasswordPage() {
  const [sent, setSent] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotForm>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: ForgotForm) => {
    await authApi.forgotPassword(values.email)
    setSent(true)
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-background p-6">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle className="text-lg">Lupa Password</CardTitle>
          <CardDescription>
            Masukkan email Anda dan kami kirim tautan reset jika akun terdaftar.
          </CardDescription>
        </CardHeader>
        {sent ? (
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Jika email terdaftar, tautan reset telah dikirim. Periksa email Anda.
            </p>
          </CardContent>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <CardContent className="grid gap-4">
              <div className="grid gap-2">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" {...register("email")} autoFocus />
                {errors.email && (
                  <p className="text-sm text-destructive">{errors.email.message}</p>
                )}
              </div>
            </CardContent>
            <CardFooter className="justify-between">
              <Link to="/login" className="text-sm text-muted-foreground hover:text-foreground">
                Kembali
              </Link>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Mengirim..." : "Kirim tautan"}
              </Button>
            </CardFooter>
          </form>
        )}
      </Card>
    </main>
  )
}