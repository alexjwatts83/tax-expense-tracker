# Skill: Backend Core Delivery

## Objective

Deliver the ASP.NET Core + EF Core backend for the tax expense tracker with soft delete support and plan-aligned endpoints.

## Scope

- Runtime: .NET 10
- Framework: ASP.NET Core Web API
- Database: SQLite (initial)
- ORM: Entity Framework Core

## Required Domain

- `Tracker` with soft delete
- `Tag` with soft delete
- `TaxExpense` with source tracker and many-to-many tags
- `TaxExpenseTag` junction table

## Implementation Checklist

1. Create API project and add EF Core packages.
2. Add models and DTOs from the plan.
3. Build `AppDbContext` with:
   - global query filters for soft-deleted entities
   - relationships and constraints
   - decimal precision for `Price`
   - seed data for default trackers
4. Implement controllers:
   - `api/trackers`
   - `api/tags`
   - `api/expenses`
5. Implement soft delete behavior for delete endpoints.
6. Add summary and filter expense endpoints.
7. Create migrations and apply to SQLite.
8. Ensure app auto-migrates on startup in development workflows.

## Validation Steps

1. `dotnet build` completes successfully.
2. `dotnet ef migrations add <Name>` succeeds.
3. `dotnet ef database update` succeeds.
4. API endpoints return expected status codes for CRUD flows.

## Definition of Done

- Backend compiles and runs.
- SQLite schema is generated from migrations.
- Default trackers are seeded.
- Soft-deleted rows are excluded by default.
