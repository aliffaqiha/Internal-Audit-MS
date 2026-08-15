import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Pencil, Plus, Search, Trash2 } from "lucide-react"
import { useState } from "react"

import { AlertDialog } from "@/components/ui/alert-dialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Pagination } from "@/components/ui/pagination"
import { Select } from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { toast } from "@/components/ui/toast"
import { adminApi } from "@/features/admin/admin-api"
import { UserFormDialog } from "@/features/admin/UserFormDialog"
import type { CreateUserPayload, UpdateUserPayload, UserDto } from "@/features/admin/types"

const PAGE_SIZE = 15

export function UsersPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState("")
  const [departmentFilter, setDepartmentFilter] = useState("")
  const [roleFilter, setRoleFilter] = useState("")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<UserDto | null>(null)
  const [page, setPage] = useState(1)

  const users = useQuery({
    queryKey: ["users", search, departmentFilter, roleFilter, page],
    queryFn: () =>
      adminApi.users({
        search: search || undefined,
        departmentId: departmentFilter || undefined,
        roleId: roleFilter || undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
  })
  const roles = useQuery({ queryKey: ["roles"], queryFn: adminApi.roles })
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments })

  const saveUser = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id?: string
      payload: CreateUserPayload | UpdateUserPayload
    }) => {
      if (id) return adminApi.updateUser(id, payload as UpdateUserPayload)
      return adminApi.createUser(payload as CreateUserPayload)
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["users"] })
      toast.success(variables.id ? "Pengguna berhasil diperbarui" : "Pengguna baru berhasil ditambahkan")
    },
    onError: (err) => {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      toast.error(message ?? "Gagal menyimpan pengguna.")
    },
  })

  const deleteUser = useMutation({
    mutationFn: (id: string) => adminApi.deleteUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] })
      setDeleteTarget(null)
      toast.success("Akun pengguna berhasil dinonaktifkan")
    },
    onError: (err) => {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      toast.error(message ?? "Gagal menonaktifkan pengguna.")
    },
  })

  const items = users.data?.items ?? []
  const totalCount = users.data?.totalCount ?? 0

  const handleFilterChange = (setter: (v: string) => void) => (v: string) => {
    setter(v)
    setPage(1)
  }

  const openCreate = () => {
    setEditingUser(null)
    setDialogOpen(true)
  }

  const openEdit = (user: UserDto) => {
    setEditingUser(user)
    setDialogOpen(true)
  }

  const handleSubmit = async (payload: CreateUserPayload | UpdateUserPayload) => {
    await saveUser.mutateAsync({
      id: editingUser?.id,
      payload,
    })
  }

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-medium">Pengguna</h1>
          <p className="text-sm text-muted-foreground">Kelola akun, peran, dan departemen.</p>
        </div>
        <Button onClick={openCreate}>
          <Plus data-icon="inline-start" />
          Tambah Pengguna
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex flex-wrap items-center gap-2">
            <div className="relative min-w-52 flex-1">
              <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(e) => handleFilterChange(setSearch)(e.target.value)}
                placeholder="Cari username, nama, atau email..."
                className="pl-9"
              />
            </div>
            <Select
              value={departmentFilter}
              onChange={(e) => handleFilterChange(setDepartmentFilter)(e.target.value)}
              className="w-44"
            >
              <option value="">Semua Departemen</option>
              {(departments.data ?? []).map((d) => (
                <option key={d.id} value={d.id}>
                  {d.name}
                </option>
              ))}
            </Select>
            <Select
              value={roleFilter}
              onChange={(e) => handleFilterChange(setRoleFilter)(e.target.value)}
              className="w-40"
            >
              <option value="">Semua Peran</option>
              {(roles.data ?? []).map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </Select>
          </CardTitle>
        </CardHeader>
        <CardContent className="grid gap-3">
          <div className="overflow-x-auto">
            {users.isLoading ? (
              <div className="grid gap-2 p-2">
                {Array.from({ length: 6 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full rounded-md" />
                ))}
              </div>
            ) : items.length === 0 ? (
              <p className="p-6 text-center text-muted-foreground">Tidak ada pengguna ditemukan.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nama</TableHead>
                    <TableHead>Username</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>Departemen</TableHead>
                    <TableHead>Peran</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="w-24 text-right">Aksi</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium">{user.fullName}</TableCell>
                      <TableCell>{user.username}</TableCell>
                      <TableCell>{user.email}</TableCell>
                      <TableCell>{user.departmentName ?? "—"}</TableCell>
                      <TableCell>
                        <div className="flex max-w-64 flex-wrap gap-1">
                          {user.roles.map((role) => (
                            <Badge key={role.id} variant="secondary">
                              {role.name}
                            </Badge>
                          ))}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={user.isActive ? "default" : "outline"}>
                          {user.isActive ? "Aktif" : "Nonaktif"}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-1">
                          <Button variant="ghost" size="icon-sm" onClick={() => openEdit(user)}>
                            <Pencil />
                            <span className="sr-only">Edit</span>
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            onClick={() => setDeleteTarget(user)}
                            disabled={deleteUser.isPending}
                          >
                            <Trash2 />
                            <span className="sr-only">Hapus</span>
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>
          <Pagination
            page={page}
            total={totalCount}
            pageSize={PAGE_SIZE}
            onPageChange={setPage}
          />
        </CardContent>
      </Card>

      <UserFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        user={editingUser}
        roles={roles.data ?? []}
        departments={departments.data ?? []}
        onSubmit={handleSubmit}
      />

      <AlertDialog
        open={!!deleteTarget}
        title="Nonaktifkan akun pengguna?"
        description={
          deleteTarget
            ? `Akun "${deleteTarget.username}" akan dinonaktifkan. Tindakan ini tidak dapat dibatalkan.`
            : undefined
        }
        confirmLabel="Nonaktifkan"
        destructive
        isPending={deleteUser.isPending}
        onConfirm={() => { if (deleteTarget) deleteUser.mutate(deleteTarget.id) }}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}