import { QueryClient, QueryClientProvider } from "@tanstack/react-query"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"

const queryClient = new QueryClient()

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <main className="flex min-h-screen flex-col items-center justify-center gap-8 bg-background p-6">
        <Card className="w-full max-w-md">
          <CardHeader>
            <Badge className="w-fit">Internal Audit Management System</Badge>
            <CardTitle className="mt-3">IAMS Foundation</CardTitle>
            <CardDescription>
              Platform audit internal terpusat — fondasi Fase 0 aktif.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              React 19 + Vite + TypeScript + Tailwind CSS v4 + shadcn/ui.
              Backend .NET 10 Clean Architecture berjalan di{" "}
              <code className="rounded bg-muted px-1 py-0.5">
                http://localhost:5000
              </code>
              .
            </p>
          </CardContent>
          <CardFooter className="justify-end">
            <Button>Mulai</Button>
          </CardFooter>
        </Card>
      </main>
    </QueryClientProvider>
  )
}

export default App