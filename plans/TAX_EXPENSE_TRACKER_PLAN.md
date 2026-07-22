# Tax Expense Tracker - Project Plan

## Project Overview
A web application to track and manage tax-deductible expenses with a focus on financial organization and reporting.

---

## Technology Stack

### Backend
- Runtime: .NET 10 / C#
- Framework: ASP.NET Core Web API
- Database: SQLite (current), upgrade path to PostgreSQL or SQL Server
- ORM: Entity Framework Core
- Architecture: DDD + Clean Architecture

### Frontend
- Framework: Angular 22
- Styling: Angular Material
- HTTP Client: Angular HttpClient
- Node Version Management: Volta

### Infrastructure
- Hosting target: Azure App Service (API) + Azure Static Web Apps (frontend)
- Logging: NLog
- API docs: Swagger (Development)
- Version Control: Git

---

## Data Model

### Tracker Table (Reference Data)
- Id (GUID/UUID) - Primary Key
- Name (string)
- Description (string, nullable)
- IsDeleted (bool)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### Tag Table
- Id (GUID/UUID) - Primary Key
- Name (string)
- IsDeleted (bool)
- CreatedAt (DateTime)

### Bank Table (Reference Data)
- Id (GUID/UUID) - Primary Key
- Name (string)
- IsDeleted (bool)
- CreatedAt (DateTime)

### Tax Expense Table
- Id (GUID/UUID) - Primary Key
- Item (string)
- Description (string)
- Date (DateTime)
- BankId (GUID) - Foreign Key to Bank
- Price (decimal)
- SourceId (GUID) - Foreign Key to Tracker
- IsDeleted (bool)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### TaxExpenseTag Table (Junction)
- Id (GUID/UUID) - Primary Key
- TaxExpenseId (GUID) - Foreign Key to TaxExpense
- TagId (GUID) - Foreign Key to Tag

### Relationships
- TaxExpense -> Tracker: Many-to-One
- TaxExpense -> Bank: Many-to-One
- TaxExpense -> Tag: Many-to-Many via TaxExpenseTag
- Query filtering excludes soft-deleted records by default

### Seed Data
Default trackers:
- H&R Block
- Pluralsight
- Udemy
- JB Hifi
- Office Works

Default banks:
- ANZ
- CBA
- Westpac

---

## Project Structure

```text
tax-expense-tracker/
├── TaxExpenseTracker.sln
├── dotnet-tools.json
├── Backend/
│   ├── TaxExpenseTracker.Domain/
│   │   └── Entities/
│   ├── TaxExpenseTracker.Application/
│   │   ├── Trackers/
│   │   ├── Tags/
│   │   ├── Banks/
│   │   └── Expenses/
│   ├── TaxExpenseTracker.Infrastructure/
│   │   ├── Data/
│   │   └── Migrations/
│   ├── TaxExpenseTracker.Api/
│   │   ├── Controllers/
│   │   ├── Models/
│   │   └── Middleware/
│   ├── TaxExpenseTracker.Tests.Unit/
│   └── TaxExpenseTracker.Tests.Integration/
├── Frontend/
│   └── src/app/
│       ├── components/
│       │   ├── dashboard/
│       │   ├── expense-list/
│       │   ├── expense-form/
│       │   ├── expense-details/
│       │   ├── tracker-management/
│       │   ├── tag-management/
│       │   └── bank-management/
│       ├── services/
│       │   ├── expense.ts
│       │   ├── tracker.ts
│       │   ├── tag.ts
│       │   └── bank.ts
│       ├── models/
│       └── app.routes.ts
├── scripts/
│   ├── Check-Prerequisites.ps1
│   ├── Install-Prerequisites.ps1
│   ├── Start-Local.ps1
│   └── Stop-Local.ps1
├── plans/
│   ├── TAX_EXPENSE_TRACKER_PLAN.md
│   └── DDD_CLEAN_ARCHITECTURE_PLAN.md
└── README.md
```

---

## API Endpoints

### Trackers
- GET /api/trackers
- GET /api/trackers/{id}
- POST /api/trackers
- PUT /api/trackers/{id}
- DELETE /api/trackers/{id}
- POST /api/trackers/{id}/restore

### Tags
- GET /api/tags
- GET /api/tags/{id}
- POST /api/tags
- PUT /api/tags/{id}
- DELETE /api/tags/{id}
- POST /api/tags/{id}/restore

### Banks
- GET /api/banks
- GET /api/banks/{id}
- POST /api/banks
- PUT /api/banks/{id}
- DELETE /api/banks/{id}
- POST /api/banks/{id}/restore

### Expenses
- GET /api/expenses
- GET /api/expenses/{id}
- POST /api/expenses
- PUT /api/expenses/{id}
- DELETE /api/expenses/{id}
- POST /api/expenses/{id}/restore
- GET /api/expenses/summary
- GET /api/expenses/filter

Current expense filter query parameters:
- date (single day)
- bankId
- price (max price)
- sourceId
- tagIds (comma-separated GUIDs)

---

## Frontend Features

- Dashboard with summary cards
- Expense list with pagination and filters
- Expense creation form with bank/tracker/tag selectors
- Tracker management with soft delete + undo restore
- Tag management with soft delete + undo restore
- Bank management with soft delete + undo restore
- Snackbar/info/error state handling

---

## DDD/Clean Architecture Status

### Phase A - Solution and Project Restructure
- [x] Completed

### Phase B - Domain Extraction and Invariants
- [x] Completed

### Phase C - Application Use Cases by Feature Slice
- [x] Completed (Trackers, Tags, Expenses, Banks)

### Phase D - Infrastructure Ownership of Persistence
- [x] Completed (DbContext, repositories, migrations, design-time factory)

### Phase E - API Cleanup
- [x] Completed (thin controllers + centralized exception middleware)

### Phase F - Testing and Quality Gates
- [x] Baseline implemented
- [~] Coverage expansion ongoing

---

## Delivery Phases

### Phase 1: Setup & Core Backend
- [x] Completed

### Phase 2: Frontend Setup
- [x] Completed

### Phase 3: Integration & Polish
- [x] Completed
- [x] Single-date filter refactor completed
- [x] Bank-as-entity refactor completed end-to-end
- [x] Local start/stop automation scripts hardened

### Phase 4: Deployment & Enhancements
- [ ] Cloud deployment setup (no containers)
- [~] Expand test coverage (unit + integration)
- [ ] CSV export functionality
- [ ] Charts/graphs for trends
- [ ] Query/performance tuning

---

## Local Developer Workflow

### Prerequisites
- .NET SDK 10
- Node.js LTS (Volta recommended)

### Start all local services
```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Start-Local.ps1"
```

Force restart if ports are stuck:
```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Start-Local.ps1" -ForceRestart
```

Stop services:
```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Stop-Local.ps1"
```

### Local URLs
- Frontend: http://localhost:4200
- API/Swagger: https://localhost:7152/swagger

---

## Deployment & Security Direction (Planned)

- Azure App Service (API) + Azure Static Web Apps (frontend)
- Environment-based CORS allow list
- API key and/or JWT authentication layer
- Store production secrets in app settings/Key Vault

---

## Current Summary

- Core product flows are implemented and working locally.
- Bank is now a dedicated entity integrated through backend, frontend, and migrations.
- Phase 4 tasks are the primary remaining workstream.
