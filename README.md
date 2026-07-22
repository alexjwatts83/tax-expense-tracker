# Tax Expense Tracker

Tax Expense Tracker is a full-stack app for managing tax-deductible expenses with soft-delete safety, filtering, and summary reporting.

## Implemented Highlights

- DDD/Clean architecture across Domain, Application, Infrastructure, and API layers
- Soft delete and restore flows for trackers, tags, banks, and expenses
- Expense filtering (single date, bank, price cap, tracker, tags)
- Inline expense creation on the Expenses page
- Manual tag entry with explicit Apply Tags flow (create missing tags, then attach)
- Expense table header filters using Angular Material controls for date, bank, tracker, and tags
- Clear-filters icon in the Actions header to reset filter inputs and reload full table data
- Filter request guard logic to avoid stale/in-flight filter responses overriding cleared results
- Dashboard summary totals grouped by bank and source
- TaxExpense.Item removed end-to-end from domain, API, frontend models, and DB schema
- Local run automation scripts with robust port handling

## Tech Stack

### Backend

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 + SQLite
- NLog logging
- Swagger in Development

### Frontend

- Angular 22
- Angular Material
- HttpClient with local proxy

## Data Model

Core entities:

- Tracker
- Tag
- Bank
- TaxExpense
- TaxExpenseTag (many-to-many join)

TaxExpense stores:

- Description, Date, Price
- BankId -> Bank
- SourceId -> Tracker
- Tags via TaxExpenseTag

Soft-delete query filters are applied for TaxExpense, Tracker, Tag, and Bank.

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

Current filter query params:

- date (single day)
- bankId
- price
- sourceId
- tagIds (comma-separated)

## Frontend Routes

- /dashboard
- /expenses
- /trackers
- /tags
- /banks

## Local Development

### Prerequisites

- .NET SDK 10
- Node.js LTS (Volta recommended)

### Install/Verify Prerequisites

```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Check-Prerequisites.ps1"
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Install-Prerequisites.ps1" -DryRun
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Install-Prerequisites.ps1"
```

### Start Everything (recommended)

```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Start-Local.ps1"
```

Force restart if needed:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Start-Local.ps1" -ForceRestart
```

Stop services:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Stop-Local.ps1"
```

Default local URLs:

- Frontend: http://localhost:4200
- Swagger: https://localhost:7152/swagger

### Manual Backend/Frontend

```bash
dotnet restore
dotnet run --project Backend/TaxExpenseTracker.Api --launch-profile https
```

```bash
cd Frontend
npm install
npm start
```

Frontend proxy targets https://localhost:7152.

## EF Migrations

Migrations are owned by Infrastructure:

```bash
dotnet tool restore
dotnet ef migrations add <MigrationName> --project Backend/TaxExpenseTracker.Infrastructure --startup-project Backend/TaxExpenseTracker.Infrastructure --context AppDbContext
dotnet ef database update --project Backend/TaxExpenseTracker.Infrastructure --startup-project Backend/TaxExpenseTracker.Api --context AppDbContext
```

Recent schema updates:

- `20260722041006_MakeBankEntity` (bank converted from string field to entity relationship)
- `20260722044559_RemoveExpenseItem` (removed Item column from TaxExpenses)

## Status

- Phase 1 complete
- Phase 2 complete
- Phase 3 complete
- DDD/Clean phases A-F complete
- Bank entity refactor completed end-to-end (backend, frontend, migration, tests)
- Expense Item field removed end-to-end (backend, frontend, migration, tests)

Open work remains in Phase 4 (deployment, coverage expansion, CSV export, additional trend analytics).