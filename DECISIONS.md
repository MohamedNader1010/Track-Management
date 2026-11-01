# DECISIONS.md — Vibe Coding Challenge

**Tool used:** Claude (Anthropic), in a chat workspace, as a pair programmer.

**How I worked:** I didn't start from a blank prompt. I gave the AI the blueprint of **my own backend template**: the
architecture and conventions I designed from scratch and have used to build several applications at my current
company. Claude then generated this project's code to that blueprint. I ran everything on my own machine (Rider,
Windows), tested it as a user, **found the problems listed below, and sent the AI back to fix each one**.

---

## 1. What did AI generate, and what did I write or modify myself?

### My backend template: the blueprint the AI had to follow

Over several production applications at my company I've built and refined a .NET backend template. For this task I
handed its structure and rules to the AI as non-negotiable requirements instead of letting it choose:

- **Clean Architecture** (Domain / Application / Infrastructure / Api) with **CQRS** via MediatR.
- A **MediatR pipeline** (logging + FluentValidation) so handlers never validate by hand.
- **`ErrorOr`** for expected failures (no exceptions for business errors), and my **`ApiController` base class**,
  which maps `ErrorOr` errors to RFC 9457 problem details. I gave the AI my implementation of it, and it adapted it
  rather than inventing its own (one change I accepted is described in section 3).
- **Domain events** dispatched by an EF Core **SaveChanges interceptor**, plus an auditing interceptor.
- **Smart enums**, **central package management** (`Directory.Packages.props`), and **unit + integration tests**.

For the frontend I set the same kind of ground rules: Angular standalone components only (no NgModules),
functional interceptors, a global loader, centralized error handling, logging, custom directives and routing.

### What the AI generated

Being honest: **the code in this repository was written by Claude**, following that blueprint:

- **Backend (.NET 10):** the domain model (`Track` aggregate, `Isrc` value object, smart enums, domain events),
  the CQRS commands/queries/validators/handlers, the pipeline behaviors, EF Core configurations and interceptors,
  seed data, JWT issuing and validation, controllers, Dockerfile / docker-compose.
- **Tests:** domain unit tests, application unit tests, and Testcontainers-based API integration tests.
- **Frontend (Angular 22):** interceptors, auth service and guards, logger, loader, toasts, global error handler,
  directives, all pages and forms, and their unit tests.
- First drafts of `README.md` and this file, and the `make-commits.ps1` script that split the finished work into one
  commit per layer. The history is grouped by layer, not a record of the exact order I worked in.

### Issues I found, and had the AI fix

The AI's output looked complete, but running it and using it turned up real gaps. For each one I identified the
problem, told the AI what was wrong and what I expected, and checked the fix:

| What I found | How I found it | What I asked for / did |
|---|---|---|
| **No way to create an artist or a track from the UI**, although `POST /api/artists` and `POST /api/tracks` are in the brief. | Clicking through the app as a catalog manager. | An Artists page with a **New artist** form and a **New track** form, using the existing endpoints only. (Section 3.) |
| **Scope creep:** when I asked about CRUD, the AI started adding update/delete endpoints and new business rules to every controller. | Reviewing what it was changing against the brief's endpoint table. | Stopped it and **narrowed the scope to the endpoints the brief lists**; it reverted the extra domain code. |
| **The API crashed on first run** with a SQL connection error. The generated config assumed a SQL Server that didn't exist on my machine. | Running it in Rider and reading the stack trace. | Diagnosed it (`Get-Service *SQL*` returned nothing), **installed SQL Server 2025 Express**, set the connection string to `.\SQLEXPRESS`. |
| **`dotnet ef` not found.** The setup steps assumed a globally installed tool. | Trying to create the migration. | Added `dotnet-ef` as a **local tool** (`dotnet tool restore` for anyone cloning), generated and applied `InitialCreate`, and had the README rewritten to match. |
| **The new UI work was missing from my history.** The commit script reported "nothing to commit" for the Artists/forms step. | Reading the script output before pushing. | Traced it to files that never reached my project folder, restored them, verified the app, and committed them as two separate commits. |
| **Manual test data was slow to enter.** Typing valid ISRCs by hand for every new track. | Using the New Track form repeatedly. | Asked for a **Generate** button that builds a valid ISRC from the artist's country and release year. |
| **The UI didn't look like Takwene.** | Comparing it with the brand. | Supplied the official logo and had it added to the header and favicon (the white logo is inverted on light backgrounds). |

### Done by me on my machine

- The AI's environment couldn't compile .NET, so the **first build, run and test happened on my machine in Rider**.
- Installed and configured SQL Server Express, the local `dotnet-ef` tool, the `InitialCreate` migration, and the
  GitHub repository and push.


---

## 2. Security issues found (or introduced) in the AI-generated code, and how they were handled

To be clear about credit: most of the protections below were **put in by the AI** while generating the code, not found
by me afterwards. I've listed where each one lives so it can be checked. The second table is the part that matters
most: trade-offs the generated code **introduced**, which I chose to accept for a demo and document.

### Handled

| Issue | How it's handled |
|---|---|
| **Secrets in config.** JWT signing key and demo passwords in `appsettings` are the classic AI default. | The base `appsettings.json` has an **empty** signing key and no users; values exist only in `appsettings.Development.json`. `JwtOptions` is validated at startup (`ValidateOnStart`, min 32 chars), so a production deploy without a real key **fails to start** instead of running with a known key. Real environments should use user-secrets / env vars / Key Vault. |
| **Weak token validation.** | Issuer, audience, lifetime and signing key are all validated; algorithms pinned to HS256 (`ValidAlgorithms`) to block algorithm-confusion; clock skew reduced from the 5-minute default to 30 s. |
| **Authenticated ≠ authorized.** Plain `[Authorize]` on writes lets *any* valid token change the catalogue. | Writes require the `ManageCatalog` policy (`catalog-manager` role). The base controller is `[Authorize]` by default, so a new endpoint is protected unless someone deliberately adds `[AllowAnonymous]`. An integration test checks that a token **without** the role gets **403**. |
| **Brute force / user enumeration** on the token endpoint. | Fixed-window rate limit per IP; one generic "Invalid username or password" for unknown user *and* wrong password; constant-time comparison that also runs for unknown usernames, so response timing doesn't reveal which usernames exist. |
| **Passwords in logs.** A logging behavior that serializes the request would write `LoginCommand.Password` to the logs. | `LoggingBehavior` logs only the request name, outcome, error codes and duration. The slow-query interceptor logs SQL text, never parameter values. |
| **Leaking internals in errors.** | Expected failures are `ErrorOr` results with curated messages. Unexpected exceptions go through a global `IExceptionHandler` that logs details server-side and returns a generic problem-details body with a `traceId`. |
| **Over-permissive CORS.** | Explicit origin list from config, only `GET/POST/PATCH`, only `Authorization`/`Content-Type` headers. |
| **Token sent to other hosts (frontend).** | The auth interceptor attaches the JWT **only** to `/api/…` requests. That's also why the UI calls the API through the dev-server proxy with relative URLs instead of a hard-coded base URL. |
| **Open redirect (frontend).** `/login?returnUrl=https://evil.example` | Only in-app paths (`/…`, not `//…`) are accepted as return URLs. |
| **Race conditions on unique data** (two requests creating the same ISRC/email at the same time both pass the "exists?" check). | Unique indexes on ISRC, artist email, DSP name and (TrackId, DspId) are the real guarantee; the resulting DB error becomes a 409. `Track` has a `rowversion` concurrency token, so concurrent status changes can't silently overwrite each other. |

### Introduced and accepted (documented trade-offs)

| Trade-off | Why it's acceptable here / what production would do |
|---|---|
| **Demo users with plain-text passwords in `appsettings.Development.json`.** | The brief only needs "at least one JWT-protected endpoint". There's no user store or registration on purpose. In production this API should only *validate* tokens issued by an identity provider (Keycloak / Entra ID). |
| **JWT stored in `sessionStorage`.** Any XSS could read it. | Chosen over `localStorage` so it's cleared when the tab closes. The proper fix is an HttpOnly cookie / backend-for-frontend. Roles read from the token are used only to show/hide buttons — the API re-checks every request. |
| **Every `DbUpdateException` is returned as 409.** | Right for unique-index races, but it would also hide a genuine foreign-key bug behind a "conflict" message. The full exception is logged server-side; a production version should inspect the SQL error number. |

---

## 3. One thing the AI got wrong that I had to fix

### The UI didn't cover the brief, and the first fix overshot it

**What happened.** The first version of the Angular app had a track list, a track detail page and a login page.
The AI presented it as done. When I tested it myself as a user, I couldn't create an artist or a track **at all** — the only way was Scalar or the `.http`
file. But `POST /api/artists` and `POST /api/tracks` are explicitly listed in the brief, and the whole point of a
full-stack task is that the UI exercises the API.

When I raised it, the AI asked whether I wanted UI for the existing endpoints or "full CRUD". I picked full CRUD,
and it started adding **update and delete endpoints for artists, tracks and DSPs** — new domain methods, new error
types, new business rules (e.g. "only draft tracks can be deleted"). Halfway through I realised that was my mistake
as much as the AI's: none of those endpoints are in the brief. I stopped it and changed the instruction.

**Why it was wrong.**

- **The AI's mistake — requirement coverage.** It built the screens that were easiest to show (lists and details)
  and skipped the write flows the brief asks for, then described the UI as done. It looked finished but wasn't.
- **The correction I almost got wrong — scope creep.** Adding PUT/DELETE across every controller would have added
  untested surface area, business rules nobody asked for, and more write endpoints to secure — in a time-boxed task
  that's reviewed against a specific endpoint list.

**How I fixed it.** I told it to implement only what the document specifies. It reverted the update/delete domain
changes so the backend stayed exactly as specified, and added:

- an **Artists** page (`GET /api/artists`) with a **New artist** form (`POST /api/artists`),
- a **New track** form (`POST /api/tracks`), reachable from the track list and from each artist's row,
- role-based guards on both create routes, and API validation errors shown under the matching form field.

**Lesson:** AI output needs to be checked against the requirements, not just against "does it run". I now go through
the brief's endpoint table and tick each one off in the UI and in the tests.

### Also worth mentioning

- **Issues the AI caught in its own first draft** while generating — not mine to take credit for, but they show
  why AI code needs review:
  - Filtering with `t.Status == status` on a smart enum compiles to a call to the overloaded `==` operator, which
    EF Core can't reliably translate to SQL → replaced with a reference-equality expression EF translates through
    the value converter.
  - A design-time `DbContext` factory would have made `dotnet ef database update` **skip the seeding hooks** and
    use a hard-coded connection string → removed.
  - Paging (`Skip/Take`) was applied before joining artists, so page order wasn't guaranteed → moved after the join.
  - My template's `ApiController` mapped `ErrorType.Unauthorized` to **403**. Wrong credentials are **401** (not
    authenticated); 403 is for an authenticated user without permission → mapped `Unauthorized` → 401 and added
    `Forbidden` → 403.
