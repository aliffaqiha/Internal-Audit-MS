import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Pencil, Plus, Trash2 } from "lucide-react"
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
import { Pagination } from "@/components/ui/pagination"
import { Skeleton } from "@/components/ui/skeleton"
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

const PAGE_SIZE = 15

export function DepartmentsPage() {
  const queryClient = useQueryClient()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingDepartment, setEditingDepartment] = useState<DepartmentDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<DepartmentDto | null>(null)
  const [page, setPage] = useState(1)

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
      setDeleteTarget(null)
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

  const handleSubmit = async (payload: { name: string; code: string }) => {
    await saveDepartment.mutateAsync({ id: editingDepartment?.id, payload })
  }

  const allDepts = departments.data ?? []
  const paged = allDepts.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

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
        <CardContent className="grid gap-3">
          <div className="overflow-x-auto">
            {departments.isLoading ? (
              <div className="grid gap-2 p-2">
                {Array.from({ length: 6 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full rounded-md" />
                ))}
              </div>
            ) : allDepts.length === 0 ? (
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
                  {paged.map((department) => (
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
                            onClick={() => setDeleteTarget(department)}
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
          </div>
          <Pagination
            page={page}
            total={allDepts.length}
            pageSize={PAGE_SIZE}
            onPageChange={setPage}
          />
        </CardContent>
      </Card>

      <DepartmentFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        department={editingDepartment}
        onSubmit={handleSubmit}
      />

      <AlertDialog
        open={!!deleteTarget}
        title="Hapus departemen?"
        description={
          deleteTarget
            ? `Hapus departemen "${deleteTarget.name}"? User terkait akan kehilangan asosiasi departemen.`
            : undefined
        }
        confirmLabel="Hapus"
        destructive
        isPending={deleteDepartment.isPending}
        onConfirm={() => { if (deleteTarget) deleteDepartment.mutate(deleteTarget.id) }}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}