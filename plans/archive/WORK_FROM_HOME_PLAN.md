# Work Location and Leave Tracker Plan

Last Updated: 2026-07-23
Plan Status: Complete (Delivered)

## Goal

Add a work-location (WFH/Office) and leave tracking feature that lets users record time by location or leave, review entries by week or month, and import public holidays from a CSV file.

## Current Progress Snapshot

1. Phase 1 - Domain and Persistence Foundation: Complete
2. Phase 2 - Entry Management Use Cases: Complete
3. Phase 3 - Weekly and Monthly Reporting: Complete
4. Phase 4 - Public Holiday CSV Import: Complete
5. Phase 5 - API and Frontend Delivery: Complete
6. Phase 6 - Hardening and Polish: Complete

## Completion Notes

1. Canonical API and frontend route naming now uses Work Location terminology.
2. Legacy compatibility routes have been removed after transition.
3. Work-location persistence naming has been finalized in the database via table rename migration.
4. Detailed rename rollout and migration notes are tracked in `plans/archive/WORK_LOCATION_RENAME_PLAN.md`.

## User Outcomes

The user will be able to:

1. Enter a date worked as WFH or Office.
2. Enter a date for leave.
3. Select a full day, half day, or a specific number of hours for either entry type.
4. View work-location and leave entries grouped by week or month.
5. Import public holidays from a CSV file.

## Scope

### In Scope

1. Create and manage work-location entries (WFH/Office).
2. Create and manage leave entries.
3. Support three time entry modes:
   - Full day = 7.6 hours
   - Half day = 3.8 hours
   - Specific hours = user-defined decimal hours
4. Provide calendar-style or table-style views for weekly and monthly summaries.
5. Import public holidays from CSV and store them for reference during reporting.

### Out of Scope for First Pass

1. Payroll export.
2. Per-project or per-client time allocation.
3. Approval workflows.
4. Recurring work-location schedules.

## Functional Requirements

### Entry Capture

1. Users can create a WFH, Office, or leave entry for a selected date.
2. Users can choose one of three entry types:
   - Full day = 7.6 hours
   - Half day = 3.8 hours
   - Specific hours = editable numeric value
3. Duplicate entries for the same date and category are not allowed.
4. The entry form should default to the current date and full day unless otherwise configured.

### Views and Reporting

1. Weekly view uses a Monday-Sunday week boundary.
2. Monthly view should show entries grouped by month with totals.
3. Each view should display:
   - total work-location hours
   - total leave hours
   - number of work-location days
   - number of leave days
   - holiday markers within the selected period
4. Users should be able to move forward/backward by week or month.

### Public Holiday Import

1. Users can upload a CSV file containing public holiday dates.
2. The import should validate required columns before saving.
3. Imported holidays should be visible in the week/month views.
4. The system should handle duplicate holiday rows safely.

#### CSV Template (Current)

1. Required headers: `Date`, `Name`.
2. Supported alias headers:
   - Date: `HolidayDate`, `Holiday_Date`
   - Name: `HolidayName`, `Holiday_Name`
3. Supported date formats: `yyyy-MM-dd`, `dd/MM/yyyy`, `d/M/yyyy`.
4. Duplicate rows in the same file are skipped.
5. Rows already present in the database (same date + name) are skipped.
6. The public holiday screen loads all records by default until a date filter is applied.
7. Work-location and leave entry paging remains client-side unless real usage shows a server-side need.

## Proposed Data Model

### WorkLocationEntry

- Id (GUID/UUID) - Primary Key
- WorkDate (DateOnly or DateTime)
- WorkLocation (Wfh, Office)
- EntryType (FullDay, HalfDay, SpecificHours)
- HoursWorked (decimal)
- Notes (string, nullable)
- IsDeleted (bool, optional if soft delete is needed)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### LeaveEntry

- Id (GUID/UUID) - Primary Key
- LeaveDate (DateOnly or DateTime)
- EntryType (FullDay, HalfDay, SpecificHours)
- HoursWorked (decimal)
- Notes (string, nullable)
- IsDeleted (bool, optional if soft delete is needed)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### PublicHoliday

- Id (GUID/UUID) - Primary Key
- HolidayDate (DateOnly or DateTime)
- Name (string)
- Source (string, nullable)
- IsImported (bool)
- CreatedAt (DateTime)

#### Seed Data

Seed the initial public holiday records with the following known holidays:

- New Year's Day - Thursday 1 January 2026
- New Year's Day - Friday 1 January 2027
- Australia Day - Monday 26 January 2026
- Australia Day - Tuesday 26 January 2027
- Good Friday - Friday 3 April 2026
- Good Friday - Friday 26 March 2027
- Easter Saturday - Saturday 4 April 2026
- Easter Saturday - Saturday 27 March 2027
- Easter Sunday - Sunday 5 April 2026
- Easter Sunday - Sunday 28 March 2027
- Easter Monday - Monday 6 April 2026
- Easter Monday - Monday 29 March 2027
- Anzac Day - Saturday 25 April 2026
- Anzac Day - Sunday 25 April 2027
- Additional Day - Monday 27 April 2026
- Additional Day - Monday 26 April 2027
- King's Birthday - Monday 8 June 2026
- King's Birthday - Monday 14 June 2027
- Bank Holiday - Monday 3 August 2026
- Bank Holiday - Monday 2 August 2027
- Labour Day - Monday 5 October 2026
- Labour Day - Monday 4 October 2027
- Christmas Day - Friday 25 December 2026
- Christmas Day - Saturday 25 December 2027
- Additional Day - Monday 27 December 2027
- Boxing Day - Saturday 26 December 2026
- Boxing Day - Sunday 26 December 2027
- Additional Day - Monday 28 December 2026
- Additional Day - Tuesday 28 December 2027

## Business Rules

1. Full day always resolves to 7.6 hours.
2. Half day always resolves to 3.8 hours.
3. Specific hours must be greater than 0 and should probably cap at a sensible daily maximum.
4. Public holiday dates are shown as display-only markers and do not change work-location or leave totals or day counts.
5. CSV import should reject invalid dates and missing mandatory columns.

## API Shape

### Work Location Entries

- GET /api/work-locations
- GET /api/work-locations?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD
- GET /api/work-locations/{id}
- POST /api/work-locations
- PUT /api/work-locations/{id}
- DELETE /api/work-locations/{id}
- POST /api/work-locations/{id}/restore
- GET /api/work-locations/summary?view=week|month&date=YYYY-MM-DD

### Leave Entries

- GET /api/leave
- GET /api/leave?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD
- GET /api/leave/{id}
- POST /api/leave
- PUT /api/leave/{id}
- DELETE /api/leave/{id}
- POST /api/leave/{id}/restore
- GET /api/leave/summary?view=week|month&date=YYYY-MM-DD

### Holiday Import

- GET /api/public-holidays
- GET /api/public-holidays?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD
- POST /api/public-holidays/import

## Frontend Features

1. Add a work-location entry form with date picker, location selector (WFH/Office), and entry type selector.
2. Add a leave entry form with the same day-part options.
3. Add a weekly/monthly summary page with navigation controls.
4. Highlight public holidays in the summary views.
5. Add a CSV import screen or dialog for holiday files.
6. Show validation feedback for duplicate dates, invalid hours, and bad CSV rows.

## Implementation Phases

### Phase 1 - Domain and Persistence

1. Add work-location entry and public holiday entities.
2. Add leave entry entity.
3. Add persistence and migrations.
4. Add validation rules for hours and dates.

### Phase 2 - Application Use Cases

1. Add create/update/delete/query services for work-location entries.
2. Add create/update/delete/query services for leave entries.
3. Add summary queries for week and month views.
4. Add holiday import use case with CSV parsing and validation.

### Phase 3 - API and UI

1. Add controllers and DTOs.
2. Add frontend screens and components.
3. Wire up weekly/monthly navigation and holiday highlighting.

### Phase 4 - Testing

1. Unit test hour conversion and validation rules.
2. Add UI-level coverage for entry creation and report views.

## Risks and Mitigations

1. Risk: ambiguous week boundaries.
   Mitigation: define and document the week start rule before implementation.
2. Risk: CSV files vary in column naming and date format.
   Mitigation: support one strict template first and document it clearly.
3. Risk: reporting logic becomes inconsistent with holiday handling.
   Mitigation: centralize summary calculations in the application layer.

## Acceptance Criteria

1. Users can record work-location entries (WFH/Office) as full day, half day, or specific hours.
2. Weekly and monthly views display totals correctly.
3. Public holidays can be imported from CSV and shown in reports.
4. Invalid dates, hours, and CSV rows are rejected with clear feedback.

## Next Actions

1. Monitor real usage before deciding whether server-side paging is needed later.

## Phased Implementation Backlog

Use this section as the delivery backlog for the same feature set described above.

### Backlog Rules

1. Keep full-day hours at 7.6.
2. Keep half-day hours at 3.8.
3. Treat specific-hours entries as user-entered decimal hours.
4. Treat public holiday import as a separate capability with its own validation and storage.
5. Build summaries on top of the saved entries and holiday data, not by duplicating logic in the UI.
6. Apply the same full-day, half-day, and specific-hours rules to both WFH and leave.

### Phase 1 - Domain and Persistence Foundation

#### Goal

Create the core data model and storage foundation for WFH entries, leave entries, and public holidays.

#### Backlog Items

- [x] Define `WorkLocationEntry` domain entity.
- [x] Define `LeaveEntry` domain entity.
- [x] Define `PublicHoliday` domain entity.
- [x] Add value rules for entry type and hours conversion.
- [x] Decide whether duplicate entries per day are allowed.
- [x] Add EF Core mappings and migrations for WFH entries.
- [x] Add EF Core mappings and migrations for leave entries.
- [x] Add EF Core mappings and migrations for public holidays.
- [x] Add repository abstractions or application ports for WFH entries.
- [x] Add repository abstractions or application ports for leave entries.
- [x] Add repository abstractions or application ports for public holidays.
- [x] Add seed/sample holiday data only if required for local development.

#### Exit Criteria

- Core entities persist successfully.
- A WFH entry can be created and retrieved from storage.
- A leave entry can be created and retrieved from storage.
- A public holiday can be saved and retrieved from storage.

### Phase 2 - Entry Management Use Cases

#### Goal

Expose application use cases for creating and managing WFH and leave entries.

#### Backlog Items

- [x] Add create WFH entry command and handler/service.
- [x] Add update WFH entry command and handler/service.
- [x] Add delete WFH entry command and handler/service.
- [x] Add get-by-id query for WFH entries.
- [x] Add list/query support for WFH entries within a date range.
- [x] Enforce one WFH entry per date.
- [x] Validate full day, half day, and specific-hours WFH inputs.
- [x] Add create leave command and handler/service.
- [x] Add update leave command and handler/service.
- [x] Add delete leave command and handler/service.
- [x] Add get-by-id query for leave entries.
- [x] Add list/query support for leave entries within a date range.
- [x] Enforce one leave entry per date.
- [x] Validate full day, half day, and specific-hours leave inputs.
- [x] Add unit tests for hour conversion and validation rules.

#### Exit Criteria

- Users can create, edit, delete, and retrieve WFH entries.
- Users can create, edit, delete, and retrieve leave entries.
- Invalid dates and invalid hours are rejected before persistence.

### Phase 3 - Weekly and Monthly Reporting

#### Goal

Add summary views that let users review WFH and leave time by week and month.

#### Backlog Items

- [x] Define week grouping convention and document it.
- [x] Add weekly summary query.
- [x] Add monthly summary query.
- [x] Add totals for WFH hours and WFH days recorded.
- [x] Add totals for leave hours and leave days recorded.
- [x] Add holiday markers in summary results.
- [x] Add previous/next period navigation inputs to summary queries.

#### Exit Criteria

- Weekly and monthly summaries return correct totals.
- Holiday dates are visible in summary output.
- Leave summaries return correct totals.

### Phase 4 - Public Holiday CSV Import

#### Goal

Allow users to import public holidays from CSV and reuse that data in reporting.

#### Backlog Items

- [x] Define the CSV column template.
- [x] Add import command or endpoint for holiday CSV upload.
- [x] Parse CSV rows and validate required columns.
- [x] Validate holiday date formats and reject malformed rows.
- [x] Deduplicate duplicate holiday rows safely.
- [x] Store imported holidays with a source label if useful.
- [x] Add tests for valid imports, invalid rows, and duplicates.

#### Exit Criteria

- A CSV file can be imported successfully.
- Bad input returns clear validation feedback.

### Phase 5 - API and Frontend Delivery

#### Goal

Expose the feature through the API and Angular UI.

#### Backlog Items

- [x] Add WFH entry DTOs and controller endpoints.
- [x] Add leave DTOs and controller endpoints.
- [x] Add holiday import DTOs and controller endpoints.
- [x] Add WFH entry form with date picker and entry type selector.
- [x] Add leave entry form with date picker and entry type selector.
- [x] Add weekly/monthly summary page or panel.
- [x] Add CSV import screen or dialog for holidays.
- [x] Highlight public holidays in the UI.
- [x] Add form validation and error messaging.

#### Exit Criteria

- Users can complete the full WFH and leave workflow from the UI.
- API contracts are stable and documented.

### Phase 6 - Hardening and Polish

#### Goal

Stabilize the feature with edge-case handling and usability improvements.

#### Backlog Items

- [x] Confirm how holidays affect totals and display logic.
- [x] Confirm whether leave and WFH share the same summary views or separate views.
- [x] Decide whether notes are required or optional.
- [x] Add empty-state handling for no entries and no holidays.
- [x] Add paging or filtering if the data volume grows.
- [x] Review date handling for time zone consistency.
- [x] Add documentation for the CSV template and week/month rules.

#### Exit Criteria

- Feature behavior is documented and predictable.
- Common edge cases are handled without ambiguity.