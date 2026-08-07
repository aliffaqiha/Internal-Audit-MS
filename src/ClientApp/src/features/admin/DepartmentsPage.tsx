import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Pencil, Plus, Trash2 } from "lucide-react"
import { useState } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { adminApi } from "@/features/admin/admin-api"
import { DepartmentFormDialog } from "@/features/admin/DepartmentFormDialog"
import type { DepartmentDto } from "@/features/admin/types"

export function DepartmentsPage() {
  const queryClient = useQueryClient()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingDepartment, setEditingDepartment] = useState<DepartmentDto | null>(null)

  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments })

  const saveDepartment = useMutation({
    mutationFn: ({ id, payload }: { id?: string; payload: { name: string; code: string } }) =>
      id ? adminApi.updateDepartment(id, payload) : adminApi.createDepartment(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["departments"] })
    },
  })

  const deleteDepartment = useMutation({
    mutationFn: (id: string) => adminApi.deleteDepartment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["departments"] })
    },
  })

  const openCreate = () => {
    setEditingDepartment(null)
    setDialogOpen(true)
  }

  const openEdit = (department: DepartmentDto) => {
    setEditingDepartment(department)
    setDialogOpen(true)
  }

  const handleDelete = (department: DepartmentDto) => {
    const confirmed = window.confirm(
      `Hapus departemen "${department.name}"? User terkait akan kehilangan asosiasi.`
    )
    if (confirmed) {
      deleteDepartment.mutate(department.id)
    }
  }

  const handleSubmit = async (payload: { name: string; code: string }) => {
    await saveDepartment.mutateAsync({ id: editingDepartment?.id, payload })
  }

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-medium">Departemen</h1>
          <p className="text-sm text-muted-foreground">
            Kelola departemen untuk asosiasi user dan auditee.
          </p>
        </div>
        <Button onClick={openCreate}>
          <Plus data-icon="inline-start" />
          Tambah Departemen
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Daftar Departemen</CardTitle>
        </CardHeader>
        <CardContent>
          {departments.isLoading ? (
            <p className="p-6 text-center text-muted-foreground">Memuat data...</p>
          ) : (departments.data ?? []).length === 0 ? (
            <p className="p-6 text-center text-muted-foreground">Belum ada departemen.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nama</TableHead>
                  <TableHead>Kode</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-24 text-right">Aksi</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(departments.data ?? []).map((department) => (
                  <TableRow key={department.id}>
                    <TableCell className="font-medium">{department.name}</TableCell>
                    <TableCell>{department.code}</TableCell>
                    <TableCell>
                      <Badge variant={department.isActive ? "default" : "outline"}>
                        {department.isActive ? "Aktif" : "Nonaktif"}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-1">
                        <Button variant="ghost" size="icon-sm" onClick={() => openEdit(department)}>
                          <Pencil />
                          <span className="sr-only">Edit</span>
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          onClick={() => handleDelete(department)}
                          disabled={deleteDepartment.isPending}
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

      <DepartmentFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        department={editingDepartment}
        onSubmit={handleSubmit}
      />
    </div>
  )
}