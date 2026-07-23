# Tax Expense Tracker

Tax Expense Tracker is a full-stack app for managing tax-deductible expenses with soft-delete safety, filtering, and summary reporting.

It also has documented planning for work-location (WFH/Office), leave, and public-holiday tracking in [plans/WORK_FROM_HOME_PLAN.md](plans/WORK_FROM_HOME_PLAN.md), with rename delivery tracked in [plans/WORK_LOCATION_RENAME_PLAN.md](plans/WORK_LOCATION_RENAME_PLAN.md).

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
- Work-location (WFH/Office), leave, and public-holiday entities added to the domain and persistence model
- Work-location/leave repositories and application services implemented and wired in DI
- Work-location and leave API endpoints added for CRUD, restore, and optional date-range querying
- Work-location and leave weekly/monthly summary endpoints added (`view=week|month`, `date=YYYY-MM-DD`) using Monday-Sunday week boundaries
- Work-location canonicalization completed end-to-end (legacy `/api/work-from-home` and `/work-from-home` routes removed)
- Work-location persistence table renamed to canonical `WorkLocationEntries` via EF migration
- Holiday markers are display-only and do not alter work-location/leave totals or day counts
- Public holiday API endpoints added for list and CSV import with validation and duplicate handling
- Public holiday seed data for 2026/2027 added via EF migration
- Shared entity base abstractions introduced (`IEntity`, `Entity`, `SoftDeletableEntity`, `AuditableEntity`, `AuditableSoftDeletableEntity`)
- Shared generic repository abstractions introduced (`IRepository<T>`, `ISoftDeleteRepository<T>`)
- Domain guard clauses standardized with `ThrowIfNullOrWhiteSpace` and `ThrowIfEqual`
- Domain/application clock handling standardized on required `TimeProvider`
- One-entry-per-date validation enforced for work-location and leave records
- Unit tests use a shared `FakeTimeProvider` with fixed deterministic dates
- Angular service/model layer added for Work Location (WFH/Office), Leave, and Public Holidays
- Angular management screens added for Work Location, Leave, and Public Holidays, with Work Location and Leave combined under a shared time-tracking page
- Time-tracking screens use local date input formatting to avoid UTC day drift in browser date fields
- Time-tracking screens support date-range filtering and lightweight client-side paging
- Public holiday screen loads all holiday records by default and applies filters only when requested
- Work-location/Leave paging remains client-side by design for current personal-use scale

## Planned Enhancements

- Delivery notes and backlog tracked in [plans/WORK_FROM_HOME_PLAN.md](plans/WORK_FROM_HOME_PLAN.md)
- Work-location rename delivery and rollout checklist tracked in [plans/WORK_LOCATION_RENAME_PLAN.md](plans/WORK_LOCATION_RENAME_PLAN.md)

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
- WorkLocationEntry
- LeaveEntry
- PublicHoliday

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

### Work Location

- GET /api/work-locations
- GET /api/work-locations?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD
- GET /api/work-locations/{id}
- POST /api/work-locations
- PUT /api/work-locations/{id}
- DELETE /api/work-locations/{id}
- POST /api/work-locations/{id}/restore
- GET /api/work-locations/summary?view=week|month&date=YYYY-MM-DD

`/api/work-locations` payloads include a `workLocation` field:

- `1` = WFH
- `2` = Office

### Leave

- GET /api/leave
- GET /api/leave?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD
- GET /api/leave/{id}
- POST /api/leave
- PUT /api/leave/{id}
- DELETE /api/leave/{id}
- POST /api/leave/{id}/restore
- GET /api/leave/summary?view=week|month&date=YYYY-MM-DD

`/api/leave` payloads include a `leaveType` field:

- `1` = Annual Leave
- `2` = Sick Leave

### Public Holidays

- GET /api/public-holidays
- GET /api/public-holidays?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD
- POST /api/public-holidays/import (multipart/form-data file upload)

Current public holiday CSV rules:

- Required headers: `Date`, `Name`
- Accepted header aliases: `HolidayDate`, `Holiday_Date`, `HolidayName`, `Holiday_Name`
- Accepted date formats: `yyyy-MM-dd`, `dd/MM/yyyy`, `d/M/yyyy`, `yyyy/M/d`
- Duplicate rows in the same file are skipped
- Existing rows with the same date and name are skipped

Current filter query params:

- date (single day)
- bankId
- price
- sourceId
- tagIds (comma-separated)

## Frontend Routes

- /dashboard
- /expenses
- /time-tracking
- /public-holidays
- /trackers
- /tags
- /banks

## Local Development

### Prerequisites

- .NET SDK 10
- Node.js LTS (Volta recommended)
- Terraform >= 1.6 (for Azure infrastructure)
- Terragrunt >= 0.60 (for stack orchestration)
- Azure CLI (for Azure auth and backend bootstrap)

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
dotnet ef database update --project Backend/TaxExpenseTracker.Infrastructure --startup-project Backend/TaxExpenseTracker.Infrastructure --context AppDbContext
```

Notes:

- API startup also applies pending migrations at runtime via `Database.Migrate()`.
- For local CLI migration execution in this repo, use Infrastructure as the startup project (API does not reference EF Design package).

Recent schema updates:

- `20260722041006_MakeBankEntity` (bank converted from string field to entity relationship)
- `20260722044559_RemoveExpenseItem` (removed Item column from TaxExpenses)
- `20260723003927_AddWfhLeaveAndPublicHolidays` (added WFH/leave/public-holiday tables and holiday seed data)
- `20260723044046_AddWorkLocationToWorkFromHome` (added `WorkLocation` column to work-location entries for WFH/Office support)
- `20260723050557_RenameWorkFromHomeEntriesToWorkLocationEntries` (renamed work-location table and primary key to canonical naming)

## Status

- Work-location rename plan complete across backend, frontend, routing, and persistence naming
- Canonical work-location routes active and legacy compatibility routes removed
- DB migration chain includes work-location column addition and table rename to `WorkLocationEntries`
- DDD/Clean phases A-F complete
- Backend and frontend builds passing
- Unit tests passing: 61/61
- Integration tests passing: 1/1

Current roadmap and backlog remain tracked in [plans/WORK_FROM_HOME_PLAN.md](plans/WORK_FROM_HOME_PLAN.md) and [plans/WORK_LOCATION_RENAME_PLAN.md](plans/WORK_LOCATION_RENAME_PLAN.md).