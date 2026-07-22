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

- Hosting: Azure App Service, Google Cloud Run, or AWS free-tier options
- Version Control: Git

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

## Planned Project Structure

```
tax-expense-tracker/
├── Backend/
│   └── TaxExpenseTracker.Api/
├── Frontend/
│   ├── src/
│   ├── package.json
│   └── angular.json
├── plans/
│   └── TAX_EXPENSE_TRACKER_PLAN.md
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

### Tags

- GET `/api/tags`
- GET `/api/tags/{id}`
- POST `/api/tags`
- PUT `/api/tags/{id}`
- DELETE `/api/tags/{id}` (soft delete)

### Expenses

- GET `/api/expenses`
- GET `/api/expenses/{id}`
- POST `/api/expenses`
- PUT `/api/expenses/{id}`
- DELETE `/api/expenses/{id}` (soft delete)
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

- Create ASP.NET Core API
- Configure EF Core and SQLite
- Implement Tracker, Tag, Expense models and relationships
- Implement CRUD endpoints with soft delete

### Phase 2: Frontend Setup

- Initialize Angular app
- Build services and core components
- Add Angular Material styling and routing

### Phase 3: Integration and Polish

- Connect frontend to backend
- Add filters, pagination, and dashboard summaries
- Improve UX with validation and error handling

### Phase 4: Deployment and Enhancements

- Deploy to cloud free-tier
- Add tests
- Add CSV export and trend charts

## Getting Started

This repository currently contains planning documents. Use the steps below when scaffolding begins.

### Prerequisites

- .NET SDK 10
- Node.js LTS
- Angular CLI

### Backend Setup (planned)

```bash
cd Backend/TaxExpenseTracker.Api
dotnet restore
dotnet ef database update
dotnet run
```

### Frontend Setup (planned)

```bash
cd Frontend
npm install
ng serve
```

## Security and Deployment Notes

Planned options from the project plan:

- Deploy API to Azure App Service free tier
- Host Angular app as a static site
- Start with API key middleware, with JWT authentication as the recommended next step

## Status

Current status: Planning phase.

Source plan: `plans/TAX_EXPENSE_TRACKER_PLAN.md`