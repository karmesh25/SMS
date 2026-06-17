# Phase 0 Kickoff Prompt — ABR Society & Real Estate Management System

Copy and paste the prompt below into Cursor Composer/Agent to scaffold the full Phase 0 monorepo from scratch.

---

```
You are building the ABR Society & Real Estate Management System - an offline-first,
portable (USB pendrive) app. Tech: Angular 17+ standalone + ASP.NET Core 8 Web API
(Clean Architecture) + PostgreSQL 15 portable. Follow .cursorrules strictly:
UUID PKs, snake_case tables, DECIMAL(15,2) money, soft deletes, ApiResponse<T>,
async repositories, in-memory token (no localStorage).

Create ONE monorepo with this layout: frontend/ (Angular), backend/ (4-project .NET
solution: ABR.Api, ABR.Application, ABR.Infrastructure, ABR.Domain), pendrive/ (.bat
scripts), plus .cursorrules, .gitignore, README.md.

STEP 1 - Backend: scaffold the 4-project Clean Architecture solution with Npgsql EF
Core, JWT bearer, FluentValidation, Serilog (daily rolling to /logs), global exception
middleware returning ApiResponse<T>, CORS for http://localhost:4200, and Swagger with
JWT support. Show Program.cs and all project files.

STEP 2 - Domain + DB: create the 14 entities (Site, Wing, Flat, MainLedger, SubLedger,
BankAccount, Broker, Condition, ConditionItem, Booking, DailyEntry, User, DeviceLicense,
AuditLog) with Guid PKs, CreatedAt/UpdatedAt, snake_case mapping, FK cascade rules,
DECIMAL(15,2) money, soft-delete columns. Add AbrDbContext, initial EF migration, and a
DbInitializer that seeds a default admin user, default site, the 16 main ledgers, and
Construction sub-ledgers.

STEP 3 - Frontend: scaffold abr-frontend (Angular 17 standalone) with Angular Material,
lazy-loaded routes (auth, dashboard, admin, booking, accounting, reports), core services
(Auth, Api, Toast, Loader), shared components (PageHeader, ConfirmDialog, Loader),
AuthInterceptor (JWT bearer), GlobalErrorHandlerService, environment.ts
(apiUrl http://localhost:5050/api), and proxy.conf.json.

STEP 4 - Pendrive scripts: create START.bat, STOP.bat, SETUP_FIRST_RUN.bat,
DAILY_BACKUP.bat per the pendrive architecture (dynamic drive letter, Postgres :5433,
API :5050, db init, device.lic, 7-day backup retention).

Output complete file paths and full code for every file, grouped by step.
```

## Reference

- Scope: `Document/ABR_Project_Scope.docx`
- Phase prompts 1–4: `Document/ABR_Cursor_Estimate.docx`
- Estimated effort: ~20 hours (Phase 0)
