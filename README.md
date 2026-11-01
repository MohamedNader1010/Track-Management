# Takwene — Track Management

Full-stack take-home: a **.NET 10 Web API** for managing artists, tracks and their distribution to DSPs
(Spotify, Apple Music, YouTube Music, Anghami), plus an **Angular 22** single-page UI.

```
.
├── backend/    .NET 10 API — Clean Architecture, CQRS, tests
├── frontend/   Angular 22 standalone app (signals, zoneless)
├── README.md
└── DECISIONS.md
```

---

## 1. Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.100+ | `dotnet --version` |
| EF Core CLI | 10.x | pinned as a **local tool** in `backend/.config/dotnet-tools.json` — just run `dotnet tool restore` |
| SQL Server | Express / LocalDB / Docker | any of them works — see 2.1 for the matching connection string |
| Node.js | 22+ | for the Angular app |
| Docker | any | only for integration tests (Testcontainers) or `docker compose` |

---

## 2. Run the API

```bash
cd backend
dotnet restore
dotnet tool restore      # installs the pinned dotnet-ef locally
```

### 2.1 Database & migrations

Set the connection string in `src/TrackManagement.Api/appsettings.Development.json`
(or, better, in user secrets: `dotnet user-secrets set "ConnectionStrings:Default" "<value>" --project src/TrackManagement.Api`).
Pick the one that matches your SQL Server:

| You have | `ConnectionStrings:Default` |
|---|---|
| SQL Server Express | `Server=.\\SQLEXPRESS;Database=TakweneTrackManagement;Trusted_Connection=True;TrustServerCertificate=True` |
| Default SQL Server instance | `Server=.;Database=TakweneTrackManagement;Trusted_Connection=True;TrustServerCertificate=True` |
| LocalDB (Visual Studio) | `Server=(localdb)\\MSSQLLocalDB;Database=TakweneTrackManagement;Trusted_Connection=True;TrustServerCertificate=True` |
| Docker (`docker compose up sqlserver -d`) | `Server=localhost,1433;Database=TakweneTrackManagement;User Id=sa;Password=Takwene_Local_Passw0rd!;TrustServerCertificate=True` |

Not sure what's installed? In PowerShell: `Get-Service *SQL*` (an `MSSQL$SQLEXPRESS` service → Express).

The `InitialCreate` migration is committed in `src/TrackManagement.Infrastructure/Persistence/Migrations`.

**Apply migrations + seed data** — either:

```bash
dotnet ef database update \
  --project src/TrackManagement.Infrastructure \
  --startup-project src/TrackManagement.Api
```

or just run the API — in Development it applies pending migrations on startup
(`Database:ApplyMigrationsOnStartup = true`).

Seeding uses EF Core's `UseSeeding`/`UseAsyncSeeding` hooks, so it runs after both commands above and is idempotent.
Seed data: **4 artists, 10 tracks** (3 draft, 2 submitted, 5 distributed — across Arabic Pop, Mahraganat, Khaleeji,
Rai, Tarab, Hip-Hop, Jazz, Electronic) and **4 DSPs**.

Adding a later migration:

```bash
dotnet ef migrations add <Name> --project src/TrackManagement.Infrastructure --startup-project src/TrackManagement.Api --output-dir Persistence/Migrations
```

### 2.2 Start it

```bash
dotnet run --project src/TrackManagement.Api
```

- API: http://localhost:5080
- API reference (Scalar, Development only): http://localhost:5080/scalar/v1
- OpenAPI document: http://localhost:5080/openapi/v1.json
- Health: http://localhost:5080/health
- Ready-made requests: `src/TrackManagement.Api/TrackManagement.Api.http` (VS / Rider / VS Code REST Client)

### 2.3 Or with Docker (SQL Server + API)

```bash
cd backend
docker compose up --build
```

The migration must exist first (step 2.1) because the API applies it on startup.

---

## 3. Get a JWT token

All `GET` endpoints are public. Every write endpoint requires a JWT **with the `catalog-manager` role**.

Demo users (Development only, from `appsettings.Development.json`):

| Username | Password | Role | Can write? |
|---|---|---|---|
| `manager` | `Takwene@2026` | `catalog-manager` | ✅ |
| `viewer` | `Viewer@2026` | — | ❌ (gets 403) |

```bash
curl -s -X POST http://localhost:5080/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{ "username": "manager", "password": "Takwene@2026" }'
```

```json
{ "accessToken": "eyJhbGciOi...", "tokenType": "Bearer", "expiresAtUtc": "2026-11-01T12:00:00+00:00" }
```

Use it as `Authorization: Bearer <accessToken>`. In Scalar, paste it into the **Authentication** box.
`GET /api/auth/me` echoes back who the token belongs to.

The token endpoint is rate-limited (10 requests / minute / IP) against brute force.

---

## 4. Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/token` | — | Exchange credentials for a JWT |
| GET | `/api/auth/me` | JWT | Current user |
| POST | `/api/artists` | JWT + role | Create an artist |
| GET | `/api/artists` | — | List all artists |
| GET | `/api/dsps` | — | List DSPs |
| POST | `/api/tracks` | JWT + role | Create a track (starts as `draft`) |
| GET | `/api/tracks?artistId=&genre=&status=&page=&pageSize=` | — | List tracks, filtered + paged |
| GET | `/api/tracks/{id}` | — | Track details incl. per-DSP distribution status |
| POST | `/api/tracks/{id}/distribute` | JWT + role | Submit a track to one or more DSPs |
| PATCH | `/api/tracks/{id}/status` | JWT + role | Change status (`draft` / `submitted` / `distributed`) |

**Track lifecycle** (enforced in the domain): `draft → submitted → distributed`, and `submitted → draft` (withdraw).
Distributing a draft track moves it to `submitted` automatically. `distributed` requires at least one
pending/live distribution and is final. Re-submitting to a DSP that *rejected* the track is allowed;
re-submitting to a pending/live one returns `409`.

**Errors** are RFC 9457 problem details:

```json
// 400 — every invalid field at once
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "Isrc": ["ISRC must follow the CC-XXX-YY-NNNNN format, e.g. 'EG-TKW-24-00001'."] },
  "traceId": "00-…", "requestId": "…" }

// 409 — business rule, with a stable machine-readable code
{ "title": "Cannot move a track from 'draft' to 'distributed'. Allowed: submitted.", "status": 409,
  "errorCode": "Track.InvalidStatusTransition", "traceId": "00-…" }
```

---

## 5. Tests

```bash
cd backend
dotnet test
```

| Project | What | Needs |
|---|---|---|
| `Domain.UnitTests` | ISRC value object, status lifecycle, distribution rules | nothing |
| `Application.UnitTests` | validators, validation pipeline behavior, handler (EF in-memory) | nothing |
| `Api.IntegrationTests` | full HTTP flows against real SQL Server: auth 401/403, validation 400, lifecycle, conflicts | Docker running + the migration from 2.1 |

Run without Docker: `dotnet test --filter "FullyQualifiedName!~IntegrationTests"`.

Frontend: `cd frontend && npm test`.

---

## 6. Run the UI

```bash
cd frontend
npm install
npm start          # http://localhost:4200 — /api is proxied to http://localhost:5080
```

| Page | Route | Endpoint(s) | Who |
|---|---|---|---|
| Track list — filter by status / artist, paging, click-to-copy ISRC | `/tracks` | `GET /api/tracks`, `GET /api/artists` | everyone |
| Track detail — full info + per-DSP distribution status | `/tracks/:id` | `GET /api/tracks/{id}` | everyone |
| ↳ change status / distribute to DSPs | same page | `PATCH …/status`, `POST …/distribute`, `GET /api/dsps` | catalog-manager |
| New track | `/tracks/new` | `POST /api/tracks` | catalog-manager |
| Artists | `/artists` | `GET /api/artists` | everyone |
| New artist | `/artists/new` | `POST /api/artists` | catalog-manager |
| Sign in | `/login` | `POST /api/auth/token` | — |

Buttons for write actions only appear for `catalog-manager` users, and the `/…/new` routes are guarded
(signed-out users are sent to sign in and brought back). The API enforces the same rule regardless.

---

## 7. Architecture at a glance

```
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure (EF Core, JWT, interceptors)
```

- **Domain** — aggregates (`Track` owns `TrackDistribution`s), value object `Isrc`, smart enums
  (`TrackStatus`, `DistributionStatus`, `Genre`), domain events, `ErrorOr` domain errors. No EF, no ASP.NET.
- **Application** — one folder per use case (command/query + validator + handler), MediatR pipeline
  (logging → validation), domain-event handlers.
- **Infrastructure** — `AppDbContext`, Fluent configurations, SaveChanges interceptors (audit timestamps,
  domain-event dispatch), a slow-query command interceptor, seeding, JWT issuing/validation.
- **Api** — thin controllers on a shared `ApiController` that maps `ErrorOr` errors to problem details;
  global `IExceptionHandler`, Serilog, OpenAPI + Scalar, CORS, rate limiting, health checks.
