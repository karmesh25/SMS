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

Client-ready USB package — see **[pendrive/CLIENT_SETUP_AND_START.txt](pendrive/CLIENT_SETUP_AND_START.txt)** for full instructions.

### Quick steps (developer)

1. Run `pendrive\build_pendrive.bat` — outputs to `pendrive\package\`
2. Copy PostgreSQL 15 portable binaries to `pendrive\package\db\bin\` (see [pendrive/db/README_POSTGRES.txt](pendrive/db/README_POSTGRES.txt))
3. Run `pendrive\validate_package.bat` — all checks must pass
4. Copy everything inside `pendrive\package\` to the USB drive root

### First run on client PC

1. `SETUP_FIRST_RUN.bat` — initialize database and create encrypted `config/secrets.enc` (choose master password)
2. `REGISTER_THIS_PC.bat` — authorize this computer (Admin → Devices)
3. `STOP.bat`, then `START.bat` for daily use (enter master password each start)

### Security (pendrive)

- **Master password** — required at each `START.bat` and `DAILY_BACKUP.bat`; unlocks `config/secrets.enc`
- **No secrets in appsettings** — connection string, JWT key, and license secret are encrypted on USB
- **PostgreSQL** — strong random password; `scram-sha-256` auth (no trust mode after setup)
- **Not protected** — copying `db/data/` offline can still expose data files; exports/backups remain plain text
- **Old USB** — run `UPGRADE_SECURITY.bat` to migrate to encrypted secrets

### Daily operation

- `START.bat` / `STOP.bat` — always stop before removing USB
- `DAILY_BACKUP.bat` — backup to `backup\` on the USB
- Reports export to `exports\` on the USB (not the PC Downloads folder)

### Demo / sandbox login

| User | Password | Sees |
|------|----------|------|
| `admin` | `Admin@123` | Real sites (e.g. Tapi) with full data |
| `demo` | `Demo@123` | Empty **Demo** sandbox site only (no wings/bookings/entries) |

Real customer data is never wiped when using the demo user.

## Phase 0 deliverables

- Monorepo scaffold (frontend + backend)
- EF Core schema with 14 entities and seed data
- Angular shell with lazy routes and core services
- Pendrive launcher scripts

## Next phase

Phase 1: Hardware fingerprint lock, JWT authentication, and RBAC.

## License

Confidential — ABR Society & Real Estate Management System, June 2026.
