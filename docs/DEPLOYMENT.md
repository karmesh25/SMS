# ABR Cloud Deployment — Render + Supabase

Deploy the full stack (Angular UI + ASP.NET Core API + PostgreSQL) on a **single free URL** using [Render](https://render.com) and [Supabase](https://supabase.com).

## Architecture

- **Render Web Service** — Docker container runs API and serves the Angular build from `wwwroot`
- **Supabase** — Managed PostgreSQL (migrations run automatically on first startup)

Public URL example: `https://abr-sms.onrender.com`

## Prerequisites

1. GitHub repository with this codebase pushed to `main`
2. [Render](https://render.com) account (sign in with GitHub)
3. [Supabase](https://supabase.com) project with PostgreSQL enabled

## 1. Supabase database

1. Open your Supabase project → **Project Settings** → **Database**
2. Copy the database password (reset if needed — never commit it to git)
3. Use the **direct** connection on port **5432** (required for EF Core migrations)

Connection string format for Npgsql:

```
Host=db.twxowhyjqrxktnkmqson.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_SUPABASE_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

Replace `YOUR_SUPABASE_PASSWORD` with your actual password.

## 2. Render web service

### Option A — Blueprint (`render.yaml`)

1. Push this repo to GitHub
2. Render Dashboard → **New** → **Blueprint**
3. Connect the repository; Render reads [`render.yaml`](../render.yaml)
4. Set secret env vars when prompted:
   - `ConnectionStrings__DefaultConnection` — full Supabase string above
   - `Jwt__SecretKey` — random string, at least 32 characters
   - `Security__LicenseSecret` — random string for license encryption

### Option B — Manual

1. Render Dashboard → **New** → **Web Service**
2. Connect GitHub repo, branch `main`
3. **Environment:** Docker
4. **Dockerfile path:** `./Dockerfile`
5. **Health check path:** `/api/health`
6. **Instance type:** Free
7. Add environment variables:

| Key | Value |
|-----|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Supabase connection string (see above) |
| `Jwt__SecretKey` | Strong random secret (32+ chars) |
| `Jwt__Issuer` | `ABR.Api` |
| `Jwt__Audience` | `ABR.Frontend` |
| `Security__EnforceDeviceLock` | `false` |
| `Security__LicenseSecret` | Random secret |

8. Click **Create Web Service** and wait for the first deploy (~5–10 minutes)

## 3. First login

After a successful deploy:

1. Open your Render URL (e.g. `https://abr-sms.onrender.com`)
2. On first start, EF migrations create all tables and seed data:
   - **Username:** `admin`
   - **Password:** `Admin@123`
   - **Default site:** Tapi
3. Change the admin password after first login (Admin → Users)

## 4. Verify deployment

```text
GET https://<your-app>.onrender.com/api/health
```

Expected: JSON with `success: true` and database connectivity.

Manual checks:

- Login works without device authorization errors
- Dashboard loads site KPIs
- Vyaj Khata — add a party and confirm it persists after refresh
- Supabase **Table Editor** shows `users`, `sites`, `vyaj_parties`, etc.

## 5. Free tier limitations

| Limit | Impact |
|-------|--------|
| Render free spin-down | Service sleeps after ~15 min idle; first request may take 30–60s |
| Render free RAM | 512 MB — sufficient for light use |
| Supabase free | ~500 MB storage; shared resources |

For production or always-on use, upgrade Render and/or Supabase plans.

## 6. Local Docker test (optional)

From repository root:

```powershell
docker build -t abr-sms .
docker run -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e ConnectionStrings__DefaultConnection="Host=...;SSL Mode=Require;Trust Server Certificate=true" `
  -e Jwt__SecretKey="your-local-test-secret-at-least-32-chars" `
  -e Security__EnforceDeviceLock=false `
  -e Security__LicenseSecret="local-license-secret" `
  abr-sms
```

Open `http://localhost:8080`

## 7. Pendrive / offline mode

USB pendrive deployment is unchanged. See [`pendrive/build_pendrive.bat`](../pendrive/build_pendrive.bat). Cloud hosting disables device lock (`EnforceDeviceLock: false`).

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Build fails on Render | Check Docker build logs; ensure `package-lock.json` exists in `frontend/` |
| Database connection failed | Verify Supabase password, SSL params, and that port 5432 is used |
| 502 on cold start | Wait 60s and retry — free tier waking up |
| Login blocked (device) | Set `Security__EnforceDeviceLock=false` in Render env vars |
| Empty UI, API works | Confirm Angular files were copied to `wwwroot` in Docker image |
