# Internal Audit Management System (IAMS)

Platform manajemen audit internal.

## Struktur
```
src/
  Backend/          # .NET 10 (Clean Architecture)
    API/            # ASP.NET Core Web API
    Application/    # CQRS (MediatR), validators, use cases
    Domain/         # Entities, domain events, result pattern
    Infrastructure/ # EF Core, persistence, services
  ClientApp/        # React 19 + Vite + TypeScript + Tailwind + shadcn/ui
tests/
docker/
docs/
```

## Prasyarat

- .NET SDK 10
- Node.js 24+
- Docker + Docker Compose

## Menjalankan

```bash
# 1. Boot infrastruktur (PostgreSQL, Redis, MinIO, Seq, Jaeger)
docker compose up -d

# 2. Jalankan API (membuat migrasi & seed otomatis saat startup)
dotnet run --project src/Backend/API

# 3. Jalankan frontend
cd src/ClientApp
npm install
npm run dev
```

- API: http://localhost:5000 (Swagger `/swagger`)
- Frontend: http://localhost:5173
- Seq: http://localhost:5341
- MinIO console: http://localhost:9001
- Jaeger: http://localhost:16686

Default admin: `admin` / `Admin@1234`