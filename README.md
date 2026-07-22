# Tax Expense Tracker

A web application to track and manage tax-deductible expenses with a focus on financial organization and reporting.

## Overview

Tax Expense Tracker helps you:

- Record business and tax-deductible expenses
- Organize expenses by tracker/source and tags
- Soft delete records safely instead of hard deletion
- Filter and summarize spending for reporting
- Prepare for future export and dashboard analytics

## Planned Technology Stack

### Backend

- Runtime: .NET 10 / C#
- Framework: ASP.NET Core Web API
- ORM: Entity Framework Core
- Database: SQLite (initial), upgrade path to PostgreSQL or SQL Server

### Frontend

- Framework: Angular (latest LTS)
- UI: Angular Material
- HTTP: Angular HttpClient

### Infrastructure

- Hosting: Azure App Service (API) and Azure Static Web Apps (frontend)
- Version Control: Git

### Tooling and Observability

- API Docs: Swagger UI (development)
- Logging: NLog (console + rolling file logs)

## Core Data Model

Main entities:

- Tracker: reference source for expenses (for example, H&R Block, Udemy)
- Tag: category labels (for example, Deductible, Equipment)
- TaxExpense: expense record with bank, date, amount, source tracker
- TaxExpenseTag: junction table for many-to-many TaxExpense and Tag

Soft delete is planned for Tracker, Tag, and TaxExpense using an `IsDeleted` flag and global query filters.

## Seed Data

Default trackers from the plan:

- H&R Block
- Pluralsight
- Udemy
- JB Hifi
- Office Works

## Current Project Structure

```
tax-expense-tracker/
├── TaxExpenseTracker.sln
├── dotnet-tools.json
├── Backend/
│   ├── TaxExpenseTracker.Domain/
│   │   └── Entities/
│   ├── TaxExpenseTracker.Application/
│   │   └── Trackers/
│   ├── TaxExpenseTracker.Infrastructure/
│   ├── TaxExpenseTracker.Api/
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── Models/
│   │   ├── Migrations/
│   │   ├── nlog.config
│   │   └── appsettings.Production.json
│   ├── TaxExpenseTracker.Tests.Unit/
│   └── TaxExpenseTracker.Tests.Integration/
├── Frontend/
│   ├── src/
│   ├── package.json
│   └── angular.json
├── plans/
│   └── TAX_EXPENSE_TRACKER_PLAN.md
│   └── DDD_CLEAN_ARCHITECTURE_PLAN.md
│   └── skills/
├── README.md
└── .gitignore
```

## API Surface (Planned)

### Trackers

- GET `/api/trackers`
- GET `/api/trackers/{id}`
- POST `/api/trackers`
- PUT `/api/trackers/{id}`
- DELETE `/api/trackers/{id}` (soft delete)
- POST `/api/trackers/{id}/restore`

### Tags

- GET `/api/tags`
- GET `/api/tags/{id}`
- POST `/api/tags`
- PUT `/api/tags/{id}`
- DELETE `/api/tags/{id}` (soft delete)
- POST `/api/tags/{id}/restore`

### Expenses

- GET `/api/expenses`
- GET `/api/expenses/{id}`
- POST `/api/expenses`
- PUT `/api/expenses/{id}`
- DELETE `/api/expenses/{id}` (soft delete)
- POST `/api/expenses/{id}/restore`
- GET `/api/expenses/summary`
- GET `/api/expenses/filter`

## Frontend Feature Plan

- Dashboard with summary cards and charts
- Tracker management (CRUD + soft delete)
- Tag management (CRUD + soft delete)
- Expense list with sorting, filtering, pagination
- Expense create/edit form with tracker dropdown and tag multi-select
- Expense details view

## Development Phases

### Phase 1: Setup and Core Backend

- Complete
- ASP.NET Core API scaffolded and running
- EF Core + SQLite configured with migrations applied
- Tracker, Tag, Expense, and TaxExpenseTag models implemented
- CRUD endpoints with soft delete implemented
- Summary and filter endpoints implemented
- Local and cloud appsettings configured
- Development secrets moved to User Secrets
- Swagger and NLog integrated

### Phase 2: Frontend Setup

- Initialize Angular app
- Build services and core components
- Add Angular Material styling and routing

### DDD/Clean Architecture Migration (Completed)

- Domain, Application, and Infrastructure layers established and wired
- Domain invariants and behavior methods implemented for Tracker, Tag, TaxExpense, and TaxExpenseTag
- Tracker, Tag, and Expense features moved to application use-case services with repository abstractions
- Persistence ownership moved to Infrastructure (DbContext, repositories, migrations)
- API controllers thinned and centralized exception middleware added
- Unit/integration coverage expanded and CI quality gates added

### Phase 3: Integration and Polish

- Connect frontend to backend
- Add filters, pagination, and dashboard summaries
- Improve UX with validation and error handling
- Add soft delete undo (restore) flows for trackers, tags, and expenses

### Phase 4: Deployment and Enhancements

- Deploy to Azure free-tier services
- Add tests
- Add CSV export and trend charts

## Getting Started

Backend and frontend integration are implemented through Phase 3, including local proxy-based API usage.

### Prerequisites

- .NET SDK 10
- Volta
- Node.js LTS (managed by Volta)
- Angular CLI

### Prerequisite Scripts

Use the repository scripts to verify and install required tooling.

1. Check prerequisites:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Check-Prerequisites.ps1"
```

2. Install missing prerequisites (dry run):

```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Install-Prerequisites.ps1" -DryRun
```

3. Install missing prerequisites (actual install):

```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Install-Prerequisites.ps1"
```

4. Re-run the checker to confirm setup:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\dev\github\tax-expense-tracker\scripts\Check-Prerequisites.ps1"
```

### Backend Setup

```bash
cd Backend/TaxExpenseTracker.Api
dotnet restore
dotnet user-secrets set "Security:ApiKey" "<your-local-dev-api-key>"
dotnet run
```

EF migrations are owned by Infrastructure. Use these commands from the repository root:

```bash
dotnet ef migrations add <MigrationName> --project Backend/TaxExpenseTracker.Infrastructure --startup-project Backend/TaxExpenseTracker.Api
dotnet ef database update --project Backend/TaxExpenseTracker.Infrastructure --startup-project Backend/TaxExpenseTracker.Api
```

Swagger UI is available in development at /swagger.

NLog writes rolling files to C:/logs/TaxExpenseTracker.Api.

### Frontend Setup

```bash
volta install node@lts
volta pin node@lts
volta install @angular/cli
cd Frontend
npm install
npm start
```

The `start` script uses `proxy.conf.json` so frontend API calls to `/api` are proxied to `https://localhost:5001` for local development.

Volta should be the standard Node version manager for this repository to keep team Node versions consistent.

## Security and Deployment Notes

Planned Azure deployment approach:

- Deploy API to Azure App Service free tier
- Host Angular app on Azure Static Web Apps
- Start with API key middleware, with JWT authentication as the recommended next step
- Store local development secrets in .NET User Secrets
- Store production secrets in Azure App Service configuration or Azure Key Vault

## Status

Current status:

- Phase 1 complete and validated
- Phase 2 complete (core Angular screens and API services implemented)
- Phase 3 complete (frontend-backend integration, filters, pagination, dashboard summary, restore flows)
- DDD/Clean migration complete across phases A-F

Source plan: `plans/TAX_EXPENSE_TRACKER_PLAN.md`