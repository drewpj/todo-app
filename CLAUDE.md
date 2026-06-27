# CLAUDE.md

This file gives Claude Code the context it needs to work in this repo. Read it fully before making changes.

## What this repo is

A take-home implementation of a multi-user to-do task management app: .NET 9 Web API backend (Controller → Service → Repository, EF Core + SQLite, JWT auth) and a Vue 3 + TypeScript frontend, both run via Docker Compose.

## Source of truth: `todo-app-spec.md`

**`todo-app-spec.md` (repo root) is the authoritative specification for this project.** It defines the data model, full API contract (routes, request/response shapes, status codes), architecture, frontend component design, Docker setup, testing strategy, and the assumptions/trade-offs behind every non-obvious decision.

Before implementing anything, read the relevant section(s) of `todo-app-spec.md` rather than inventing a design from scratch. The spec was written specifically to remove ambiguity — if something seems underspecified, check there first before guessing.

**If you make an implementation decision that diverges from the spec** (a different default, a different folder name, different endpoint behavior), **update the spec to match what you actually built.** Don't let it go stale — it's relied on as an accurate description of the system, not just the original plan. This applies especially to Section 12 (Assumptions & Trade-offs) and Section 13 (Future Work), which should reflect reality.

## Always check progress first

`todo-app-spec.md` Section 14 ("Implementation Progress Checklist") contains:
- A checklist of every implementation task, grouped and ordered by dependency.
- A **Session Notes** block at the top of that section, summarizing current state, next step, and any blockers as of the last session.

**At the start of every session:**
1. Read Section 14's Session Notes block to understand where things stand.
2. Check which checklist items are already done vs. not.
3. Pick up from the next unchecked item unless told otherwise.

**At the end of every session** (or before running low on context):
1. Check off any checklist items completed.
2. Update the Session Notes block with 2–3 sentences: current state, next step, any blockers. Overwrite the previous note — it reflects the latest state, not a running log.
3. If anything diverged from the spec, update the relevant section(s) per the rule above.

## Key conventions (see spec for full detail)

- **Backend**: single ASP.NET Core Web API project, folder-based layering (`Controllers/`, `Services/`, `Repositories/`, `Models/`, `DTOs/`, `Data/`, `Exceptions/`). No multi-project split. EF Core uses Fluent API (`IEntityTypeConfiguration<T>`) for schema, Data Annotations for request DTO validation — these are different concerns, don't conflate them.
- **Auth**: JWT (HMAC-SHA256, 1-hour expiry, Issuer/Audience validated). Never trust a client-supplied user ID — the current user always comes from validated JWT claims server-side. Ownership checks live in the Service layer.
- **Errors**: every error response uses the standardized shape in spec Section 5 (`code`, `message`, `details`, `traceId`, `correlationId`), produced by one global `IExceptionHandler` — not per-controller try/catch.
- **Frontend**: Vue 3 + TypeScript, built with Vite, package-managed with `npm`. State/data access goes through the `useAuth()` and `useTasks()` composables (exact interfaces in spec Section 7) — don't introduce Pinia or another state library.
- **Soft delete**: deleting a task sets `IsDeleted = true`; it is never physically removed. All read queries must filter `IsDeleted = false`.
- **Migrations**: real EF Core Code-First migrations, committed to the repo, applied automatically on startup (`db.Database.Migrate()`) — not `EnsureCreated()`.
- **Seeding**: the demo user and sample tasks are seeded via a DI scope in `Program.cs` *after* migrations run — never via EF Core's `HasData()`, since seeding needs `PasswordHasher<T>` from DI (see spec Section 8, "Demo data seeding").
- **CORS**: backend explicitly allows `http://localhost:5173` (no wildcard — credentialed requests require an explicit origin).
- **Nginx**: the frontend's nginx config must include a SPA fallback (`try_files $uri $uri/ /index.html;`) or client-side routes will 404 on refresh.

## Commands

Once scaffolded, this section should be filled in with the actual commands (e.g., `dotnet test`, `npm run test`, `docker-compose up --build`). Update it as soon as the projects exist — don't leave it as a placeholder past the first session.

```
# Run everything locally:
docker-compose up --build

# Backend build:
cd TodoApp.Api && dotnet build

# Backend tests (once test projects exist):
dotnet test

# Frontend dev server (outside Docker, for faster iteration):
(TBD once frontend project exists)
```

## Testing expectations

Per spec Section 10: unit tests focus on the Service layer (`AuthService`, `TaskService`) covering both success and failure paths; integration tests use `WebApplicationFactory<Program>` against a real (file-based or held-open in-memory) SQLite test database — see Section 10 for why a naive in-memory connection string is a trap. Frontend tests are explicitly out of scope for this MVP.

## What not to do

- Don't add features marked out-of-scope in spec Section 2 (sharing/collaboration, tags, subtasks, attachments, pagination, roles, password reset, etc.) without being asked — they're deliberately deferred, not forgotten.
- Don't introduce new libraries/dependencies not already specified in the spec (e.g., no Moq, FluentAssertions, Pinia, BCrypt) without checking in first — the spec made deliberate minimal-dependency choices.
- Don't restructure the project layout (e.g., splitting into multiple .NET projects) without updating the spec first — that's a documented trade-off, not an oversight.