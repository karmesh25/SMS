# ABR Society & Real Estate Management System

Portable pendrive edition — offline-first society and real estate management.

**Stack:** Angular 17+ | ASP.NET Core 8 Web API | PostgreSQL 15

## Repository structure

```
SMS/
├── frontend/          # Angular 17 standalone SPA (abr-frontend)
├── backend/           # ASP.NET Core 8 Clean Architecture solution
│   ├── ABR.Api/
│   ├── ABR.Application/
│   ├── ABR.Infrastructure/
│   └── ABR.Domain/
├── pendrive/          # Portable launcher scripts (START, STOP, SETUP, BACKUP)
├── prompts/           # Cursor development prompts
├── Document/          # Project scope documents
├── .cursorrules       # AI coding rules for Cursor
└── README.md
```

## Prerequisites (development)

- Node.js 18+ and npm
- .NET 8 SDK
- PostgreSQL 15 (local or portable on pendrive)

## Local development

### Backend

```bash
cd backend
dotnet restore
dotnet ef database update --project ABR.Infrastructure --startup-project ABR.Api
dotnet run --project ABR.Api
```

API runs at `http://localhost:5050` — Swagger at `/swagger`.

Default connection (see `backend/ABR.Api/appsettings.Development.json`):

```
Host=localhost;Port=5432;Database=abr_db;Username=postgres;Password=postgres
```

### Frontend

```bash
cd frontend
npm install
npm start
```

App runs at `http://localhost:4200` with API proxy to port 5050.

## Pendrive deployment

1. Run `pendrive/build_pendrive.bat` (after Phase 9) or copy build outputs manually.
2. Place portable PostgreSQL under `pendrive/db/`.
3. Run `SETUP_FIRST_RUN.bat` once on the authorized machine.
4. Use `START.bat` / `STOP.bat` for daily operation.

## Phase 0 deliverables

- Monorepo scaffold (frontend + backend)
- EF Core schema with 14 entities and seed data
- Angular shell with lazy routes and core services
- Pendrive launcher scripts

## Next phase

Phase 1: Hardware fingerprint lock, JWT authentication, and RBAC.

## License

Confidential — ABR Society & Real Estate Management System, June 2026.
