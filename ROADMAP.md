# Roadmap Internal Audit Management System (IAMS)

> Fase diurutkan berdasarkan nilai bisnis tertinggi dengan kompleksitas terkendali.
> Setiap fase memiliki **deliverable**, **acceptance criteria**, dan **estimasi**.
> Prinsip: bangun fondasi solid  (auth + DB), lalu inti bisnis (planning → finding → CAP → verification), dilanjutkan dengan report, dashboard, dan otomasi.

---

## Fase 0 — Setup & Fondasi

**Goal:** Repo, lingkungan dev, dan CI dasar berjalan.

- [ ] Scaffold repo sesuai struktur `src/{ClientApp, Backend/API, Backend/Application, Backend/Domain, Backend/Infrastructure, tests, docker, docs}`
- [ ] Backend: .NET 10 Web API + Clean Architecture layers
- [ ] Frontend: React 19 + Vite + TypeScript + Tailwind + shadcn/ui
- [ ] Docker Compose: `postgres`, `redis`, `minio`, `seq` (opsional `jaeger`), `hangfire`
- [ ] EF Core migration + seed data (roles, default admin)
- [ ] Serilog (console + Seq) tersambung
- [ ] GitHub Actions: build, test, lint (image build)
- [ ] Health check endpoint + logging

**DOD:** `docker compose up` menaikkan seluruh service; API merespons `/health`; `ClientApp` menampilkan halaman kosong.

---

## Fase 1 — Authentication & RBAC 

**Goal:** Akses aman; dasar semua fitur lain.

- [ ] JWT Access Token + Refresh Token (rotation)
- [ ] Login / Logout / Refresh interceptor di client (TanStack Query)
- [ ] Forgot Password (email reset token)
- [ ] Rate limiting (anti brute-force login)
- [ ] Audit Trail: login/logout/token actions dicatat

**DOD:** User bisa login/logout/reset password; token exp → refresh otomatis; role policy terbaca di client.

---

## Fase 2 — User, Department, Role Management

**Goal:** Master data & otorisasi berfungsi.

- [x] CRUD Department (Finance, HR, IT, Procurement, Warehouse, Production)
- [x] CRUD User (assign role, assign department, active/inactive)
- [x] Policy-Based Authorization per action (Admin-only untuk management)
- [x] UI admin (daftar user, form create/update)

**DOD:** Admin bisa buat/edit/hapus user & assignment; user non-admin terblokir dari endpoint admin.

---

## Fase 3 — Audit Planning

**Goal:** Rencana audit mengalir dari Draft → Completed.

- [x] Entity: AuditPlan (objective, scope, schedule, status)
- [x] Entity: AuditAssignment (anggota tim audit)
- [x] Workflow status: Draft → Submitted → Approved → In Progress → Completed
- [x] Command: CreateAuditPlan, Submit, Approve, Start, Complete (CQRS)
- [x] Audit Checklist (template per standar, contoh IT: backup, firewall, access control, patch)
- [x] API + UI form planning + daftar rencana audit

**DOD:** Auditor membuat rencana → submit → manager approve → start → complete; checklist bisa dijalankan per audit plan.

---

## Fase 4 — Finding & Evidence

**Goal:** Temuan terinci + bukti ber-versi.

- [x] CRUD Finding (title, description, department, risk level, category, recommendation, due date)
- [x] Risk: Low / Medium / High / Critical
- [x] Upload Evidence (PDF, Image, Excel, Word) → MinIO; versioning + timestamp + metadata
- [x] Validasi tipe/ukuran file + secure filename (no path traversal)
- [x] Domain Event `FindingCreated` → notifikasi + audit log

**DOD:** Auditor membuat temuan dan mengunggah bukti (version-aware); file tersimpan di MinIO dan tercatat.

---

## Fase 5 — Corrective Action Plan & Verification

**Goal:** Loop tindak lanjut lengkap (auditee isi → verify → close).

- [ ] CAP (action, PIC, target date, progress, attachment)
- [ ] Status: Open → In Progress → Pending Verification → Closed
- [ ] Verification flow: auditor review → approve / reject → reopen
- [ ] Relasi Finding ↔ CAP
- [ ] API + UI auditee dashboard untuk entri CAP

**DOD:** Auditee bisa mengisi CAP; auditor review/approve/reject; status berubah sesuai alur.

---

## Fase 6 — Audit Trail & Notifications

**Goal:** Traceability & notifikasi otomatis.

- [ ] AuditTrail: user, action, date, IP, old value, new value (untuk entity penting)
- [ ] Notification entity + SignalR (realtime in-app)
- [ ] Email notification via SMTP (async background)
- [ ] Trigger: Finding Assigned, CAP Due Tomorrow, CAP Overdue, Audit Approved

**DOD:** Setiap perubahan penting tercatat; user menerima notifikasi in-app realtime + email.

---

## Fase 7 — Audit Report 

**Goal:** Generate laporan PDF.

- [ ] Struktur laporan: Executive Summary, Finding, Recommendation, Conclusion
- [ ] PDF generation server-side
- [ ] Download / simpan ke MinIO
- [ ] Template & layout konsisten

**DOD:** Laporan PDF lengkap bisa di-generate dari data audit dalam satu klik.

---

## Fase 8 — Dashboard & Analytics 

**Goal:** Nilai jual utama — insight manajemen.

- [ ] Query analytics (efisien, cacheable via Redis):
  - Audit Progress (% vs total)
  - Finding by Risk
  - Finding by Department
  - Finding by Category
  - CAP near-due
  - CAP overdue
  - Rata-rata waktu penyelesaian finding
  - Jumlah audit per auditor
  - Distribusi status audit
- [ ] Visualisasi grafik (recharts / chart library)
- [ ] Redis cache untuk query dashboard

**DOD:** Dashboard manajemen realtime, informatif, dan cepat.

---

## Fase 9 — Background Job & Otomasi 

**Goal:** Pengingat dan maintenance otomatis.

- [ ] Hangfire: reminder CAP due/overdue, cleanup temp file, report generation
- [ ] Job cron yang idempotent
- [ ] Outbox Pattern (opsional) untuk future messaging

**DOD:** Reminder terkirim otomatis tanpa intervensi manual.

---

## Fase 10 — Security Hardening & Non-Functional

**Goal:** Siap production.

- [ ] JWT refresh, rate limiting global
- [ ] Secure file upload (type/size validation), file encryption (opsional)
- [ ] Full-text search (PostgreSQL) untuk finding/CAP
- [ ] OpenTelemetry → Jaeger (tracing), Serilog → Seq
- [ ] Backup strategy (PostgreSQL + MinIO)
- [ ] Responsive UI semua halaman

---

## Fase 11 — Testing & QA (berjalan sepanjang proyek)

- [ ] Unit test (Domain/Application) + MediatR handler
- [ ] Integration test (API + DB)
- [ ] E2E (Playwright) untuk flow utama: login, create audit, finding, CAP, report
- [ ] Load test baseline (opsional)
- [ ] 80%+ test coverage pada core logic

---

## Fase 12 — Deployment & Go-Live

- [ ] Docker image production build (multi-stage)
- [ ] Reverse proxy (Traefik/NGINX) + HTTPS
- [ ] Database migration strategy (EF Core pada deployment)
- [ ] Monitoring + alerting (healthcheck, logs)
- [ ] Staging → Production smoke test
- [ ] Rollout plan & dokumentasi admin

---


**Bisa diparalelkan:**
- Fase 9 (Hangfire) bisa dimulai saat Fase 5 (CAP) berjalan.
- Fase 11 (Testing) berjalan sepanjang fase pengembangan.
- Fase 3 & 4 saling terkait (Audit Plan & Finding) — kerjakan berurutan.

## Catatan Kunci

- Mulai selalu dengan **data model & migrasi** sebelum fitur UI.
- Terapkan **Result Pattern** + **Domain Events** sejak awal (sulit di-retrofit).
- Dashboard pakai **query cache** agar tidak membebani database.
- MFA, File Encryption, ElasticSearch, Outbox + RabbitMQ bersifat **opsional** — tambahkan hanya jika waktu memungkinkan.

