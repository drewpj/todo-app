# TodoApp

A multi-user to-do task management app built with .NET 9 Web API and Vue 3 + TypeScript.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9, EF Core 9 + SQLite, JWT auth |
| Frontend | Vue 3, TypeScript, Vite, vue-router |
| Infrastructure | Docker Compose, nginx |

## Quick Start

```bash
# 1. Copy env file (defaults work for local dev)
cp .env.example .env

# 2. Start everything
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

## Key Design Decisions

- **Soft delete**: tasks set `IsDeleted=true` rather than being physically removed
- **JWT auth**: HMAC-SHA256, 1-hour expiry, user ID always read from validated token claims
- **No state library**: auth and task state managed via module-level `ref()` in composables
- **Real EF migrations**: committed to repo, auto-applied on startup
- **SQLite persistence**: mounted via Docker named volume (`db_data`)
