# TodoApp

A multi-user to-do task management app built with .NET 9 Web API and Vue 3 + TypeScript.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9, EF Core 9 + SQLite, JWT auth |
| Frontend | Vue 3, TypeScript, Vite, vue-router |
| Infrastructure | Docker Compose, nginx |

## Prerequisites

**To run with Docker (recommended):**
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose)

**To run without Docker:**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (includes npm)

## Quick Start

```bash
docker-compose up --build
```

- Frontend: http://localhost:5173
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

**Demo account** — username: `demo`, password: `demo1234`

## Development (without Docker)

**Backend:**
```bash
cd TodoApp.Api
dotnet run
# API available at http://localhost:5000
```

**Frontend:**
```bash
cd todo-frontend
npm install
npm run dev
# Dev server at http://localhost:5173
```

**Tests:**
```bash
dotnet test
```

## Architecture

```
todo-app/
├── TodoApp.Api/              # .NET 9 Web API
│   ├── Controllers/          # HTTP layer (AuthController, TasksController)
│   ├── Services/             # Business logic (AuthService, TaskService)
│   ├── Repositories/         # Data access (UserRepository, TaskRepository)
│   ├── Models/               # EF Core entities (User, TaskItem)
│   ├── DTOs/                 # Request/response models
│   ├── Data/                 # DbContext + EF configurations + migrations
│   └── Exceptions/           # Custom exceptions + global error handler
├── TodoApp.Api.UnitTests/    # xUnit unit tests (service layer)
├── TodoApp.Api.IntegrationTests/ # xUnit integration tests (WebApplicationFactory)
├── todo-frontend/            # Vue 3 + TypeScript frontend
│   └── src/
│       ├── composables/      # useAuth, useTasks
│       ├── components/       # AppHeader, TaskFilters, TaskList, TaskItem, TaskFormModal
│       ├── views/            # LoginView, RegisterView, TaskListView
│       ├── lib/              # apiClient (fetch wrapper)
│       ├── router/           # vue-router with navigation guards
│       └── types/            # TypeScript interfaces for API types
└── docker-compose.yml
```

## API Overview

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | /api/auth/register | No | Create account |
| POST | /api/auth/login | No | Get JWT token |
| GET | /api/auth/me | Yes | Current user |
| GET | /api/tasks | Yes | List tasks (filter/sort) |
| POST | /api/tasks | Yes | Create task |
| GET | /api/tasks/{id} | Yes | Get task |
| PUT | /api/tasks/{id} | Yes | Update task |
| DELETE | /api/tasks/{id} | Yes | Soft-delete task |

All error responses follow the shape `{ "error": { "code", "message", "details", "traceId", "correlationId" } }`.

## Development Approach

This project followed a spec-driven development approach. Before writing any code, the full API contract, data model, layered architecture, testing strategy, and key trade-offs were documented in [`todo-app-spec.md`](todo-app-spec.md). Each non-obvious decision — why soft delete over hard delete, why a single .NET project over a multi-project solution, why hand-written fakes over a mocking library — was recorded with its rationale before implementation began.

The spec was treated as a living document: when implementation diverged from the original plan, the spec was updated to reflect what was actually built and why. The Assumptions, Trade-offs, and Known Limitations sections below are drawn directly from it.

Claude Code was used as an implementation assistant, guided by the spec above. Architecture decisions, trade-offs, and design choices were made upfront in the spec; Claude handled the implementation.

## Architecture Decisions

- **Controller → Service → Repository layering**: Controllers handle HTTP concerns only; Services own business logic and authorization checks; Repositories abstract data access. Ownership checks live in the Service layer so no controller action can accidentally bypass them.
- **Centralized error handling**: a single `IExceptionHandler` maps every exception type to a consistent error shape — no per-controller try/catch blocks.
- **User identity from JWT claims only**: the current user's ID is always read from the validated token server-side; client-supplied user IDs are never trusted.
- **Correlation ID flow**: the frontend generates a UUID per request (`X-Correlation-Id` header); the backend reads or mints it, attaches it to every log line, and echoes it in error responses — a single ID ties a user-reported error to its server-side trace.
- **Frontend state via composables**: `useAuth` and `useTasks` hold module-level `ref()` state, giving shared singleton state across all components without a dedicated state library.

## Assumptions

Decisions made where the original brief left things unspecified:

- **Task fields** — `Title`, `Description`, `Status`, `Priority`, `DueDate`, `CreatedAt`, `UpdatedAt` were chosen as a reasonable starter set; no external spec dictated these fields
- **Field length limits** — `Title` max 200 chars, `Description` max 2000 chars, `Username` max 50 chars; reasonable defaults, not handed-down requirements
- **Password minimum length** — 6 characters; spec said "minimum-length only" without specifying a number
- **Default list sort** — `createdAt desc`; most natural default for a task list
- **JWT expiration** — 1 hour; reasonable session length absent any stated requirement
- **Enum names** — `Todo/InProgress/Done` and `Low/Medium/High`; common, self-explanatory names

## Trade-offs

Deliberate choices of a simpler approach over a more complex one, made knowingly:

- **Single .NET project with folder-based layering** — avoids multi-project complexity (`Api`/`Core`/`Infrastructure`) for an app of this size; would revisit if the codebase grew significantly
- **Soft delete** — tasks set `IsDeleted=true` rather than being removed; preserves data and leaves room for a future trash/restore feature at the cost of every read query needing the filter
- **No refresh tokens** — simplifies auth at the cost of forcing re-login every hour; would revisit if hourly re-login proved disruptive
- **JWT in `localStorage`** — simplest SPA approach, accepting the known XSS-exposure trade-off over an httpOnly cookie; would revisit for higher-sensitivity data
- **`PUT` for all task updates (no dedicated `PATCH /status`)** — keeps the API surface smaller; a dedicated status endpoint would be a cleaner "toggle complete" interaction
- **No rate limiting on auth endpoints** — acceptable for this exercise; a production deployment would add brute-force login protection before going live
- **HTTP-only Docker setup** — no TLS termination is configured; a production deployment would terminate TLS at a reverse proxy in front of the services
- **xUnit with hand-written fakes (no Moq)** — minimal dependencies; slightly more boilerplate per test but keeps the test suite easy to read and maintain
- **No frontend tests** — backend logic prioritised as the higher-value target within a take-home time box
- **No pagination** — filtering and sorting are included; pagination deferred until task counts grow large enough to make a full-list response slow
- **No API versioning** — acceptable with a single first-party consumer; would add before any external dependency on the API

## Known Limitations

Things to be aware of while using the app right now:

- **Sessions expire after 1 hour** — there is no refresh token mechanism; you will need to log in again when the token expires
- **No password reset or email verification** — if you forget your password for a non-demo account there is no recovery path
- **HTTP only** — the local Docker setup does not include TLS; passwords and tokens are unencrypted in transit (fine for local review, not for production)
- **SQLite** — adequate for local use and review; not suitable for production concurrent write load (see Scalability below)
- **Logout is client-side only** — JWTs are stateless; signing out clears the local token but the token remains technically valid until its 1-hour expiry

## Scalability & Future Work

What would change if this continued into a real product:

- **Database** — replace SQLite with PostgreSQL or SQL Server; SQLite's single-writer model means concurrent write throughput is capped at one transaction at a time — adequate for light use, but a hard ceiling under real production write load. The EF Core abstraction makes this largely a configuration change.
- **Pagination** — the `{ "tasks": [...] }` response wrapper was designed to add `totalCount` and cursor/page fields later without a breaking change
- **Horizontal scaling** — the backend is already stateless (JWT auth, no server-side sessions), so running multiple instances behind a load balancer works out of the box
- **Refresh tokens** — short-lived access tokens paired with long-lived refresh tokens to avoid hourly re-login
- **Rate limiting and brute-force protection** — required on auth endpoints before any public deployment
- **TLS termination** — a reverse proxy (nginx, Caddy, or a cloud load balancer) in front of the services
- **Structured log aggregation** — `traceId` and `correlationId` are already present in every log line and error response; shipping to a structured log platform requires no redesign
- **CI/CD pipeline** — automated build, test, and deploy on push
- **Managed secrets** — move the JWT signing key from `docker-compose.yml` into Azure Key Vault, AWS Secrets Manager, or equivalent
- **httpOnly cookie + CSRF protection** — stronger token storage alternative to `localStorage` if the security bar rises
