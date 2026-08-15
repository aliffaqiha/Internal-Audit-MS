# Internal Audit Management System (IAMS)

[![CI Build & Test](https://github.com/aliffaqiha/Internal-Audit-MS/actions/workflows/ci.yml/badge.svg)](https://github.com/aliffaqiha/Internal-Audit-MS/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19.0-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0+-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![MinIO](https://img.shields.io/badge/MinIO-S3-C72C48?logo=minio&logoColor=white)](https://min.io/)

**IAMS (Internal Audit Management System)** adalah platform tingkat enterprise untuk mengelola seluruh siklus audit internal organisasi secara terintegrasi—mulai dari perencanaan berbasis risiko (*Audit Planning*), pelaksanaan checklist, pencatatan temuan (*Finding & Evidence*), tindak lanjut perbaikan (*Corrective Action Plan / CAP*), verifikasi auditor, hingga pelaporan otomatis (*PDF Generation*) dan analitik eksekutif real-time.

---

## Daftar Isi

- [Fitur Utama](#fitur-utama)
- [Arsitektur & Tech Stack](#arsitektur--tech-stack)
- [Struktur Direktori](#struktur-direktori)
- [Prasyarat Sistem](#prasyarat-sistem)
- [Panduan Instalasi & Menjalankan](#panduan-instalasi--menjalankan)
- [Konfigurasi Environment](#konfigurasi-environment)
- [Pengujian (Testing)](#pengujian-testing)
- [Backup & Disaster Recovery](#backup--disaster-recovery)
- [Observability & Monitoring](#observability--monitoring)
- [Akun Bawaan (Default Credentials)](#akun-bawaan-default-credentials)

---

## Fitur Utama

### 1. Keamanan & RBAC (Role-Based Access Control)
- **Autentikasi Modern:** JWT Access Token + HttpOnly Refresh Token Rotation dengan proteksi *token reuse detection* dan *family revocation*.
- **Hardening Keamanan:** Anti brute-force rate limiting (global IP + strict auth policy), timing-equalized credential check, login lockout otomatis, dan mandatory password change pada login pertama.
- **Data Scoping & Otorisasi:** Scoping data berbasis peran (`Admin`, `LeadAuditor`, `Auditor`, `Auditee`, `Management`) dan departemen (proteksi `403 Forbidden` untuk akses di luar wewenang).

### 2. Manajemen Master Data
- **Departemen:** Master departemen (Finance, HR, IT, Procurement, Warehouse, Production, dll).
- **Pengguna (Users):** Manajemen user, penugasan role dan departemen, status aktif/nonaktif.

### 3. Perencanaan Audit (Audit Planning)
- **Siklus Lengkap:** Alur status formal: `Draft` -> `Submitted` -> `Approved` -> `In Progress` -> `Completed`.
- **Assignment & Tim:** Penugasan Lead Auditor dan tim anggota audit per audit plan.
- **Checklist Standar:** Pelaksanaan checklist audit interaktif dengan urutan stabil berdasarkan kategori & standar (e.g. ISO, ITIL, COSO).

### 4. Pencatatan Temuan & Bukti (Finding & Evidence)
- **Matriks Risiko:** Klasifikasi tingkat risiko (`Low`, `Medium`, `High`, `Critical`) beserta target tanggal penyelesaian (*due date*).
- **Penyimpanan Bukti Aman:** Upload multi-berkas (PDF, Dokumen Office, Gambar) ke MinIO S3 dengan validasi tipe *magic-byte*, sanitasi path (*anti path-traversal*), versi berkas, dan *authenticated streaming download*.
- **Full-Text Search:** Pencarian cepat berbasis PostgreSQL `tsvector` dan indeks GIN untuk ribuan temuan dan CAP.

### 5. Tindak Lanjut & Verifikasi (CAP & Verification)
- **Closed-Loop CAP:** Workflow perbaikan: `Open` -> `In Progress` -> `Pending Verification` -> `Closed` / `Reopened`.
- **Auditee Portal:** Antarmuka khusus bagi auditee untuk menginput tindakan perbaikan, PIC, estimasi, dan bukti pendukung.
- **Auditor Verification:** Verifikasi hasil tindak lanjut oleh auditor dengan catatan persetujuan atau penolakan (*reopen*).

### 6. Dashboard & Analitik Eksekutif
- **Grafik & Metrik Realtime:** Visualisasi distribusi temuan berdasarkan risiko, departemen, kepatuhan jadwal CAP, dan beban kerja auditor (menggunakan Recharts).
- **Redis Query Cache:** Performa dashboard yang optimal dengan *intelligent invalidation caching*.

### 7. Pelaporan Otomatis (Audit Report PDF)
- **QuestPDF Server-Side Generator:** Pembuatan laporan audit resmi secara instan (mencakup Executive Summary, Detail Temuan, CAP, dan Kesimpulan).
- **Penyimpanan Terpusat:** Laporan tersimpan otomatis di MinIO untuk diunduh sewaktu-waktu.

### 8. Notifikasi & Audit Trail
- **Realtime In-App:** Notifikasi langsung di web menggunakan SignalR WebSocket.
- **Email Asinkron:** Pengiriman email otomatis saat penugasan temuan baru, CAP mendekati jatuh tempo, maupun persetujuan audit.
- **Audit Trail:** Pencatatan komprehensif seluruh mutasi data (aktor, IP address, waktu, nilai lama vs nilai baru).

### 9. Background Jobs & Otomasi (Hangfire)
- Pengingat otomatis CAP *near-due* dan *overdue*.
- Pembersihan berkas *temporary upload* secara periodik.
- Background report compilation.

---

## Arsitektur & Tech Stack

```
+-------------------------------------------------------------+
|                 Frontend: React 19 + Vite                   |
|        (TypeScript, Tailwind CSS v4, TanStack Query)        |
+------------------------------+------------------------------+
                               | HTTPS / JSON / SignalR WSS
+------------------------------v------------------------------+
|             Backend: ASP.NET Core 10 Web API                |
|  +-------------------------------------------------------+  |
|  │              API Layer (Controllers & Hubs)           │  │
|  +-------------------------------------------------------+  │
|  │      Application Layer (CQRS MediatR & Validators)    │  │
|  +-------------------------------------------------------+  │
|  │          Domain Layer (Entities & Domain Events)      │  │
|  +-------------------------------------------------------+  │
|  │     Infrastructure Layer (EF Core, Services, Jobs)    │  │
|  +-------------------------------------------------------+  │
+------┬──────────────┬──────────────┬──────────────┬---------+
       │              │              │              │
+------v------+  +----v------+  +----v------+  +----v------+
| PostgreSQL  |  |   Redis   |  |  MinIO S3 |  | Seq / OTLP|
| (DB + FTS)  |  |  (Cache)  |  | (Evidence)|  |(Telemetry)|
+-------------+  +-----------+  +-----------+  +-----------+
```

| Layer | Teknologi |
| --- | --- |
| **Backend Core** | .NET 10 (C# 13), ASP.NET Core Web API, Clean Architecture |
| **Pola Desain** | CQRS dengan MediatR, Result Pattern, Domain Events, Specification |
| **Database & ORM** | PostgreSQL 17, Entity Framework Core 10, Npgsql |
| **Object Storage** | MinIO (S3 API Compatible) |
| **Caching & Queue** | Redis 7, Hangfire (PostgreSQL Storage) |
| **Reporting** | QuestPDF |
| **Realtime** | ASP.NET Core SignalR |
| **Logging & Tracing** | Serilog, Seq Sink, OpenTelemetry / Jaeger |
| **Frontend Framework** | React 19, Vite, TypeScript |
| **Styling & UI** | Tailwind CSS v4, Base UI / Radix primitives, Lucide Icons |
| **State & API Client** | TanStack Query (React Query) v5, Axios Interceptor |
| **Forms & Validation** | React Hook Form, Zod |
| **Testing** | xUnit, FluentAssertions, Moq, Playwright (E2E) |

---

## Struktur Direktori

```
InternalAuditMS/
├── .github/
│   └── workflows/ci.yml       # CI/CD Pipeline (Build, Test, Lint)
├── docker-compose.yml         # Container definitions (Postgres, Redis, MinIO, Seq, Jaeger)
├── ROADMAP.md                 # Project roadmap & progress tracker
├── docs/
│   └── backup.md              # Disaster recovery & backup documentation
├── scripts/
│   └── backup/                # Automated shell scripts for DB & Object Storage backup
├── src/
│   ├── Backend/
│   │   ├── IAMS.slnx          # Solution file (.NET 10)
│   │   ├── API/               # Controllers, Middleware, SignalR Hubs, Program.cs
│   │   ├── Application/       # CQRS Commands & Queries, DTOs, Validators, Interfaces
│   │   ├── Domain/            # Entities, Enums, Value Objects, Domain Events
│   │   └── Infrastructure/    # EF Core DbContext, Migrations, Repositories, S3, Email, Jobs
│   └── ClientApp/             # React 19 Frontend
│       ├── src/
│       │   ├── components/    # Reusable UI components & layouts
│       │   ├── features/      # Feature modules (admin, audit, findings, caps, dashboard, auth)
│       │   └── lib/           # Axios instance, SignalR connection, QueryClient
│       └── e2e/               # Playwright End-to-End Test Suite
└── tests/
    ├── Domain.UnitTests/      # Domain entity & logic tests
    ├── Application.UnitTests/ # CQRS Handlers & FluentValidator tests
    └── API.IntegrationTests/  # Health checks, Auth & Rate Limiting tests
```

---

## Prasyarat Sistem

Sebelum menjalankan aplikasi, pastikan sistem telah terpasang:
- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- [Node.js 22+ / 24+ & npm](https://nodejs.org/)
- [Docker Engine & Docker Compose](https://www.docker.com/)
- [Git](https://git-scm.com/)

---

## Panduan Instalasi & Menjalankan

### 1. Clone Repository
```bash
git clone https://github.com/aliffaqiha/Internal-Audit-MS.git
cd Internal-Audit-MS
```

### 2. Jalankan Infrastruktur Pendukung (Docker)
Jalankan service database, cache, storage, dan logger:
```bash
docker compose up -d
```
*Pastikan container `iams-postgres`, `iams-redis`, `iams-minio`, `iams-seq`, dan `iams-jaeger` berstatus Healthy.*

### 3. Menjalankan Backend API
Migrasi database EF Core dan data seeding awal (Master Roles & Default Admin) akan dieksekusi secara otomatis saat API dijalankan:
```bash
dotnet run --project src/Backend/API
```
Backend akan aktif di:
- **API URL:** `http://localhost:5000`
- **Swagger Documentation:** `http://localhost:5000/swagger`
- **Hangfire Dashboard:** `http://localhost:5000/hangfire` (Khusus role Admin)

### 4. Menjalankan Frontend ClientApp
Buka terminal baru:
```bash
cd src/ClientApp
npm install
npm run dev
```
Frontend akan aktif di:
- **Web App:** `http://localhost:5173`

---

## Konfigurasi Environment

Aplikasi membaca konfigurasi dari file `appsettings.json` atau melalui Environment Variables di server:

| Variabel | Deskripsi | Default Dev |
| --- | --- | --- |
| `DATABASE_URL` | PostgreSQL Connection String | `Host=localhost;Port=5432;Database=iams;Username=iams;Password=iams_dev_password` |
| `JWT_SECRET_KEY` | Kunci rahasia enkripsi token JWT (min 32 char) | *(Development key)* |
| `REDIS_CONNECTION` | Connection string Redis | `localhost:6379` |
| `MINIO_ENDPOINT` | URL host MinIO S3 | `localhost:9000` |
| `MINIO_ACCESS_KEY` | Access Key MinIO | `iams_minio` |
| `MINIO_SECRET_KEY` | Secret Key MinIO | `iams_minio_secret` |
| `MINIO_BUCKET_NAME`| Nama bucket penyimpanan bukti/laporan | `iams-evidence` |
| `SMTP_HOST` | Host server SMTP email | `localhost` |
| `SMTP_PORT` | Port SMTP email | `1025` (e.g. Mailhog / Mailpit) |

---

## Pengujian (Testing)

### 1. Menjalankan Unit & Integration Tests (Backend)
```bash
# Menjalankan seluruh test suite backend
dotnet test src/Backend/IAMS.slnx

# Menjalankan spesifik unit test
dotnet test tests/Application.UnitTests/IAMS.Application.UnitTests.csproj
dotnet test tests/Domain.UnitTests/IAMS.Domain.UnitTests.csproj
```

### 2. Menjalankan Linter & E2E Tests (Frontend)
```bash
cd src/ClientApp

# Linter (Oxlint)
npm run lint

# Playwright E2E Tests (Headless)
npm run test:e2e

# Playwright Test UI Mode
npm run test:e2e:ui
```

---

## Backup & Disaster Recovery

Aplikasi dilengkapi dengan skrip otomatis untuk backup database PostgreSQL dan bucket MinIO yang terletak di folder `scripts/backup/`:

- **Backup Manual:**
  ```bash
  bash scripts/backup/backup.sh
  ```
- **Restore Manual:**
  ```bash
  bash scripts/backup/restore.sh /path/to/backup_directory
  ```

Panduan lengkap mengenai retensi, enkripsi, dan cron job backup dapat dibaca pada [docs/backup.md](docs/backup.md).

---

## Observability & Monitoring Dashboard

Setelah menjalankan `docker compose up -d`, Anda dapat mengakses dasbor monitoring melalui browser:

| Layanan | URL Akses | Fungsi |
| --- | --- | --- |
| **IAMS Web Client** | `http://localhost:5173` | Aplikasi Utama IAMS |
| **API Swagger OpenAPI** | `http://localhost:5000/swagger` | Dokumentasi & Interaksi REST API |
| **Hangfire Dashboard** | `http://localhost:5000/hangfire` | Monitoring Background Jobs & Reminders |
| **Seq Structured Logs** | `http://localhost:5341` | Log Query & Error Tracking |
| **MinIO Console** | `http://localhost:9001` | Manajemen Berkas & Object Storage |
| **Jaeger UI** | `http://localhost:16686` | Distributed Tracing & Performance Analysis |

---

## Akun Bawaan (Default Credentials)

Untuk keperluan *development* dan pengujian lokal, akun administrator awal telah disiapkan:

- **Username / Email:** `admin` (atau `admin@iams.local`)
- **Password:** `Admin@1234`
- **Role:** `Admin`

> [!IMPORTANT]
> Saat login pertama kali dengan akun default, sistem akan mewajibkan pergantian kata sandi (*Forced Password Change*) demi kepatuhan standar keamanan sebelum dapat mengakses menu aplikasi.

---

## Lisensi

Project ini merupakan project pribadi dan bertujuan untuk pembelajaran.