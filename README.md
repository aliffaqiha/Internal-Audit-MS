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

Default admin (Development/Testing only): `admin` / `Admin@1234` — harus diganti saat login pertama.

## Konfigurasi (env)

API gagal berhenti saat startup jika variabel berikut tidak diisi:

| Variabel | Keterangan |
| --- | --- |
| `DATABASE_URL` | Connection string PostgreSQL |
| `JWT_SECRET_KEY` | Minimal 32 karakter |
| `MINIO_ENDPOINT` / `MINIO_ACCESS_KEY` / `MINIO_SECRET_KEY` | Object storage |

Di belakang reverse proxy, isi `ForwardedHeaders:KnownProxies` / `KnownNetworks` agar `X-Forwarded-For`/`-Proto` dipercaya (default: dinonaktifkan).