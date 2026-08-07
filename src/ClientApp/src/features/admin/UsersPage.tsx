import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Pencil, Plus, Search, Trash2 } from "lucide-react"
import { useState } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Select } from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { adminApi } from "@/features/admin/admin-api"
import { UserFormDialog } from "@/features/admin/UserFormDialog"
import type { CreateUserPayload, UpdateUserPayload, UserDto } from "@/features/admin/types"

export function UsersPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState("")
  const [departmentFilter, setDepartmentFilter] = useState("")
  const [roleFilter, setRoleFilter] = useState("")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserDto | null>(null)

  const users = useQuery({ queryKey: ["users"], queryFn: adminApi.users })
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
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] })
    },
  })

  const deleteUser = useMutation({
    mutationFn: (id: string) => adminApi.deleteUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] })
    },
  })

  const filtered = (users.data ?? []).filter((u) => {
    const q = search.trim().toLowerCase()
    const matchesSearch =
      q === "" ||
      u.username.toLowerCase().includes(q) ||
      u.fullName.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q)
    const matchesDepartment =
      departmentFilter === "" || u.departmentId === departmentFilter
    const matchesRole = roleFilter === "" || u.roles.some((r) => r.id === roleFilter)
    return matchesSearch && matchesDepartment && matchesRole
  })

  const openCreate = () => {
    setEditingUser(null)
    setDialogOpen(true)
  }

  const openEdit = (user: UserDto) => {
    setEditingUser(user)
    setDialogOpen(true)
  }

  const handleDelete = (user: UserDto) => {
    const confirmed = window.confirm(
      `Nonaktifkan akun "${user.username}"? Tindakan ini tidak dapat dibatalkan.`
    )
    if (confirmed) {
      deleteUser.mutate(user.id)
    }
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
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Cari username, nama, atau email..."
                className="pl-9"
              />
            </div>
            <Select
              value={departmentFilter}
              onChange={(e) => setDepartmentFilter(e.target.value)}
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
              onChange={(e) => setRoleFilter(e.target.value)}
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
        <CardContent className="overflow-x-auto">
          {users.isLoading ? (
            <p className="p-6 text-center text-muted-foreground">Memuat data...</p>
          ) : filtered.length === 0 ? (
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
                {filtered.map((user) => (
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
                          onClick={() => handleDelete(user)}
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
    </div>
  )
}