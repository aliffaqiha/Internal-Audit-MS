import { useQuery } from "@tanstack/react-query"
import { Search } from "lucide-react"
import { useState } from "react"

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
import { auditLogsApi } from "@/features/audit-logs/audit-logs-api"

const ENTITIES = [
  "User",
  "Department",
  "AuditPlan",
  "Finding",
  "CorrectiveAction",
  "AuditLog",
]

const PAGE_SIZE = 20

export function AuditLogsPage() {
  const [search, setSearch] = useState("")
  const [entity, setEntity] = useState("")
  const [page, setPage] = useState(1)

  const logs = useQuery({
    queryKey: ["audit-logs", search, entity, page],
    queryFn: () =>
      auditLogsApi.list({
        search: search || undefined,
        entity: entity || undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
  })

  const handleSearch = (v: string) => { setSearch(v); setPage(1) }
  const handleEntity = (v: string) => { setEntity(v); setPage(1) }

  const items = logs.data?.items ?? []
  const totalCount = logs.data?.totalCount ?? 0

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-medium">Jejak Audit</h1>
          <p className="text-sm text-muted-foreground">
            Riwayat aktivitas pada aplikasi (khususnya perubahan data penting).
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Riwayat Aktivitas</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4">
          <div className="flex flex-wrap items-center gap-2">
            <div className="relative min-w-64 flex-1">
              <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(e) => handleSearch(e.target.value)}
                placeholder="Cari user, aksi, atau ID entitas..."
                className="pl-9"
              />
            </div>
            <Select
              value={entity}
              onChange={(e) => handleEntity(e.target.value)}
              className="w-44"
            >
              <option value="">Semua entitas</option>
              {ENTITIES.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </Select>
          </div>

          <div className="overflow-x-auto">
            {logs.isLoading ? (
              <div className="grid gap-2 p-2">
                {Array.from({ length: 8 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full rounded-md" />
                ))}
              </div>
            ) : items.length === 0 ? (
              <p className="p-6 text-center text-muted-foreground">
                Belum ada catatan aktivitas.
              </p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Waktu</TableHead>
                    <TableHead>User</TableHead>
                    <TableHead>Aksi</TableHead>
                    <TableHead>Entitas</TableHead>
                    <TableHead>Detail</TableHead>
                    <TableHead>IP</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((log) => (
                    <TableRow key={log.id}>
                      <TableCell className="whitespace-nowrap text-muted-foreground">
                        {new Date(log.createdAt).toLocaleString()}
                      </TableCell>
                      <TableCell className="font-medium">{log.userName ?? "—"}</TableCell>
                      <TableCell className="whitespace-nowrap">{log.action}</TableCell>
                      <TableCell className="whitespace-nowrap">{log.entity}</TableCell>
                      <TableCell className="max-w-64 truncate text-muted-foreground">
                        {log.newValues ?? log.oldValues ?? log.entityId ?? "—"}
                      </TableCell>
                      <TableCell className="whitespace-nowrap text-muted-foreground">
                        {log.ipAddress ?? "—"}
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
    </div>
  )
}