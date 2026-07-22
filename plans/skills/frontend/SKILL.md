# Skill: Frontend Delivery (Angular)

## Objective

Build the Angular frontend that consumes the backend API and delivers expense tracking workflows defined in the plan.

## Scope

- Framework: Angular (LTS)
- UI: Angular Material
- HTTP: Angular HttpClient

## Component Targets

- Dashboard and summary widgets
- Tracker management
- Tag management
- Expense list with sorting/filtering/pagination
- Expense form (create/edit)
- Expense details view

## Implementation Checklist

1. Initialize Angular app in `Frontend/`.
2. Add Angular Material and configure theme/layout basics.
3. Create API services:
   - `expense.service.ts`
   - `tracker.service.ts`
   - `tag.service.ts`
4. Implement pages/components for tracker and tag CRUD.
5. Implement expense table with source and tag rendering.
6. Add create/edit modal form with validation.
7. Add route-level views and app navigation.
8. Connect filtering UI to `/api/expenses/filter`.
9. Connect dashboard cards/charts to `/api/expenses/summary`.

## Validation Steps

1. `npm install` succeeds.
2. `ng serve` runs and app loads.
3. Tracker, tag, and expense operations succeed against backend API.
4. Pagination and filters return expected results.

## Definition of Done

- Frontend communicates with backend APIs.
- CRUD flows are functional end-to-end.
- Core screens are responsive and usable on desktop/tablet.
