import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { useForm } from "react-hook-form"
import { z } from "zod"

import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select } from "@/components/ui/select"
import type {
  CreateUserPayload,
  DepartmentDto,
  RoleDto,
  UpdateUserPayload,
  UserDto,
} from "@/features/admin/types"

const USERNAME_RE = /^[a-zA-Z0-9._-]+$/
const PASSWORD_RE = /^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9]).{8,}$/

const formSchema = z
  .object({
    username: z.string().max(50),
    fullName: z.string().min(1, "Nama lengkap wajib diisi").max(150),
    email: z.string().min(1, "Email wajib diisi").email("Email tidak valid").max(256),
    departmentId: z.string().nullable(),
    roleIds: z.array(z.string()).min(1, "Pilih minimal satu peran"),
    isActive: z.boolean(),
    password: z.string(),
  })
  .superRefine((data, ctx) => {
    if (!data.username || !USERNAME_RE.test(data.username)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["username"],
        message: "Username wajib diisi, hanya huruf, angka, . _ -",
      })
    }
    if (data.password && !PASSWORD_RE.test(data.password)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["password"],
        message: "Password minimal 8 karakter, mengandung huruf besar, kecil, dan angka",
      })
    }
  })

type FormValues = z.infer<typeof formSchema>

interface UserFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: UserDto | null
  roles: RoleDto[]
  departments: DepartmentDto[]
  onSubmit: (payload: CreateUserPayload | UpdateUserPayload) => Promise<void>
}

export function UserFormDialog({
  open,
  onOpenChange,
  user,
  roles,
  departments,
  onSubmit,
}: UserFormDialogProps) {
  const isEdit = user !== null

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(formSchema) })

  useEffect(() => {
    if (open) {
      reset({
        username: user?.username ?? "",
        fullName: user?.fullName ?? "",
        email: user?.email ?? "",
        departmentId: user?.departmentId ?? null,
        roleIds: user?.roles.map((r) => r.id) ?? [],
        isActive: user?.isActive ?? true,
        password: "",
      })
    }
  }, [open, user, reset])

  const selectedRoles = watch("roleIds")
  const isActive = watch("isActive")

  const toggleRole = (roleId: string) => {
    const next = selectedRoles.includes(roleId)
      ? selectedRoles.filter((id) => id !== roleId)
      : [...selectedRoles, roleId]
    setValue("roleIds", next, { shouldDirty: true })
  }

  const onFormSubmit = async (values: FormValues) => {
    try {
      const payload: CreateUserPayload | UpdateUserPayload = isEdit
        ? {
            email: values.email,
            fullName: values.fullName,
            departmentId: values.departmentId,
            roleIds: values.roleIds,
            isActive: values.isActive,
            newPassword: values.password || null,
          }
        : {
            username: values.username,
            email: values.email,
            fullName: values.fullName,
            departmentId: values.departmentId,
            roleIds: values.roleIds,
            isActive: values.isActive,
            password: values.password,
          }
      await onSubmit(payload)
      onOpenChange(false)
    } catch (err) {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data
        ?.message
      setError("root", {
        message: message ?? "Terjadi kesalahan. Silakan coba lagi.",
      })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? "Edit Pengguna" : "Tambah Pengguna"}</DialogTitle>
          <DialogDescription>
            {isEdit
              ? `Perbarui detail ${user?.fullName}.`
              : "Buat akun pengguna baru dan tetapkan peran."}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onFormSubmit)} noValidate className="grid gap-4">
          <div className="grid max-h-[55vh] gap-4 overflow-y-auto pr-1">
            <div className="grid gap-2">
              <Label htmlFor="username">Username</Label>
              <Input
                id="username"
                {...register("username")}
                readOnly={isEdit}
                className={isEdit ? "opacity-60" : undefined}
                autoFocus={!isEdit}
              />
              {errors.username && (
                <p className="text-sm text-destructive">{errors.username.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="fullName">Nama Lengkap</Label>
              <Input id="fullName" {...register("fullName")} />
              {errors.fullName && (
                <p className="text-sm text-destructive">{errors.fullName.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" {...register("email")} />
              {errors.email && (
                <p className="text-sm text-destructive">{errors.email.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="departmentId">Departemen</Label>
              <Select id="departmentId" {...register("departmentId")}>
                <option value="">— Tidak ada —</option>
                {departments.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </Select>
            </div>
            <div className="grid gap-2">
              <Label>Peran</Label>
              <div className="grid gap-2 rounded-md border p-3">
                {roles.map((role) => (
                  <label
                    key={role.id}
                    className="flex cursor-pointer items-center gap-2 text-sm"
                  >
                    <Checkbox
                      checked={selectedRoles.includes(role.id)}
                      onChange={() => toggleRole(role.id)}
                    />
                    <span className="font-medium">{role.name}</span>
                    {role.description && (
                      <span className="text-xs text-muted-foreground">{role.description}</span>
                    )}
                  </label>
                ))}
              </div>
              {errors.roleIds && (
                <p className="text-sm text-destructive">{errors.roleIds.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="password">
                {isEdit ? "Password Baru (opsional)" : "Password"}
              </Label>
              <Input
                id="password"
                type="password"
                placeholder={isEdit ? "Kosongkan jika tidak diganti" : ""}
                {...register("password")}
              />
              {errors.password && (
                <p className="text-sm text-destructive">{errors.password.message}</p>
              )}
            </div>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox
                checked={isActive}
                onChange={(e) => setValue("isActive", e.target.checked)}
              />
              Akun aktif
            </label>
            {errors.root && <p className="text-sm text-destructive">{errors.root.message}</p>}
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Batal
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Menyimpan..." : isEdit ? "Simpan" : "Buat"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}