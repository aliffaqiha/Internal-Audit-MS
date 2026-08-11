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

export function AuditLogsPage() {
  const [search, setSearch] = useState("")
  const [entity, setEntity] = useState("")

  const logs = useQuery({
    queryKey: ["audit-logs", search, entity],
    queryFn: () =>
      auditLogsApi.list({
        search: search || undefined,
        entity: entity || undefined,
        take: 100,
      }),
  })

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
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Cari user, aksi, atau ID entitas..."
                className="pl-9"
              />
            </div>
            <select
              value={entity}
              onChange={(e) => setEntity(e.target.value)}
              className="h-9 rounded-md border bg-transparent px-3 text-sm"
            >
              <option value="">Semua entitas</option>
              {ENTITIES.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
          </div>

          {logs.isLoading ? (
            <p className="p-6 text-center text-muted-foreground">Memuat data...</p>
          ) : (logs.data ?? []).length === 0 ? (
            <p className="p-6 text-center text-muted-foreground">
              Belum ada catatan aktivitas.
            </p>
          ) : (
            <div className="overflow-x-auto">
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
                  {logs.data?.map((log) => (
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
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}