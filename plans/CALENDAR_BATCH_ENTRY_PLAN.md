# Calendar Batch Entry Plan (WFH and Leave)

## Goal

Add a new page that shows a month view focused on Monday to Friday, where each day is displayed as a row/tile. The user can select one or more days, choose whether each day is Work From Home (WFH) or Leave, and submit all selected entries in a single batch action.

## Problem Statement

Current WFH and Leave flows are entry-by-entry. Users need a faster way to plan or backfill multiple weekdays for a month without creating each record individually.

## Progress Tracking

### Status Legend

- [ ] Not Started
- [~] In Progress
- [x] Complete
- [!] Blocked

### Current Progress Snapshot

1. Phase 1 - UX Skeleton and Routing: [x] Complete
2. Phase 2 - Batch API Contract: [x] Complete
3. Phase 3 - End-to-End Submission: [x] Complete
4. Phase 4 - Quality and Hardening: [x] Complete

### Decision Tracker

1. Endpoint approach (combined vs split): [x] Decided - Separate endpoints for now.
2. Existing entry behavior (read-only vs editable): [x] Decided - Editable in place.
3. Public holiday behavior: [x] Decided - Public holidays are locked; users cannot add WFH or Leave.
4. Default entry type/hours behavior: [x] Decided - Default entry type is Full Day.

### Milestone Checklist

1. [x] Route added and reachable from navbar.
2. [x] Month weekday renderer implemented (Monday-Friday only).
3. [x] Per-day selector implemented (None/WFH/Leave).
4. [x] Per-day entry type and hours validation implemented.
5. [x] Batch payload contract finalized.
6. [x] Batch API implemented and wired.
7. [x] Result summary and retry UX implemented.
8. [x] Frontend unit tests excluded (not present in project).
9. [x] Backend unit tests added.

### Progress Notes

Use this section to log implementation updates in chronological order.

| Date | Owner | Update | Phase | Status |
| --- | --- | --- | --- | --- |
| 2026-07-23 | Team | Plan created and tracking scaffold added. | Planning | Complete |
| 2026-07-23 | Team | Product decisions confirmed for endpoint split, editable entries, public holiday lock, and Full Day default. | Planning | Complete |
| 2026-07-23 | Team | Phase 1 started and completed: route + navbar link added, weekday calendar rows implemented, editable per-day state with Full Day default and specific-hours validation, holiday-lock behavior wired via holiday lookup. | Phase 1 | Complete |
| 2026-07-23 | Team | Phase 2 completed: added separate WFH/Leave batch endpoints, batch DTO contracts, non-throwing mixed-result handling (created/skipped/failed), public holiday lock enforcement, and unit coverage for mixed batch outcomes. | Phase 2 | Complete |
| 2026-07-23 | Team | Phase 3 completed: frontend now submits only changed rows, applies create/update/delete deltas, shows row-level apply outcomes, and preserves failed rows for immediate retry. | Phase 3 | Complete |
| 2026-07-23 | Team | Integration tests removed from scope by product direction; hardening focuses on backend unit tests and UX/accessibility checks. | Phase 4 | In Progress |
| 2026-07-23 | Team | Frontend unit tests removed from scope because frontend test framework/tests are not present in this project. | Phase 4 | In Progress |
| 2026-07-23 | Team | Added backend leap-year/month-boundary unit tests for leave and WFH summaries and applied accessibility labels to calendar controls; build and unit tests pass. | Phase 4 | Complete |
| 2026-07-23 | Team | Continued post-first-pass UX/accessibility refinements: added keyboard shortcuts on focused day cells (N/W/L/F/H/S), improved focus-visible styling, and added live-region feedback messaging; frontend build passes. | Post First Pass | Complete |

## User Outcomes

The user will be able to:

1. Open a dedicated monthly calendar batch page.
2. View weekdays only (Monday to Friday) for the selected month.
3. Mark each day as one of:
   - None
   - WFH
   - Leave
4. Optionally set entry type for each marked day:
   - Full Day (7.6)
   - Half Day (3.8)
   - Specific Hours
5. Submit all marked days using one Batch Add button.
6. Receive a result summary showing created, skipped, and failed entries.

## Scope

### In Scope

1. New frontend page and route for monthly weekday batch entry.
2. Weekday-only month rendering (Mon-Fri).
3. Per-day state management for WFH/Leave/None.
4. Batch submit workflow with API support.
5. Validation and conflict handling (duplicates, invalid hours, conflicting day type).
6. Basic tests for UI state logic and backend batch use case.

### Out of Scope (First Pass)

1. Saturday/Sunday scheduling.
2. Recurring templates across multiple months.
3. Approval workflows.
4. Bulk edit/delete on existing entries.

## UX and Interaction Design

## Page Layout

1. Header:
   - Month navigation (Previous, Current Month label, Next)
   - Optional quick actions: Clear All, Mark Selected as WFH, Mark Selected as Leave
2. Calendar grid/list:
   - Show Monday to Friday dates only for the selected month
   - Each day row/tile includes:
     - Date label (e.g., Mon 05)
     - Status selector (None, WFH, Leave)
     - Entry type selector (Full Day, Half Day, Specific Hours)
     - Hours input visible only when Specific Hours is selected
     - Optional notes input (if required later)
3. Footer action bar:
   - Batch Add button
   - Pending count (e.g., 12 changes)
   - Validation summary area

## Interaction Rules

1. Selecting WFH/Leave marks the row as pending for batch add.
2. Selecting None removes that row from pending payload.
3. Specific Hours requires a decimal value greater than 0 and within configured max.
4. Weekends are not shown and cannot be selected.
5. Public holidays are shown as markers and are not selectable for WFH or Leave.
6. New day selections default to Full Day unless the user changes entry type.

## Functional Requirements

## Calendar Data Loading

1. Page defaults to current month.
2. Fetch existing WFH and Leave entries for visible month range.
3. Rows with existing entries display pre-filled and editable in place.
4. The page should distinguish between:
   - Existing persisted entries
   - New unsaved selections

## Batch Add Behavior

1. Batch Add sends only pending unsaved changes.
2. Backend validates each item independently.
3. Frontend groups pending rows by category and submits to separate WFH and Leave batch endpoints.
4. Batch response includes per-item result:
   - Created
   - SkippedDuplicate
   - FailedValidation
   - FailedConflict
5. UI renders summary and keeps failed rows editable for retry.

## Business Rules

1. One category per day: a day cannot be both WFH and Leave in the same batch item.
2. Full Day = 7.6 hours.
3. Half Day = 3.8 hours.
4. Specific Hours > 0 and <= daily maximum.
5. Duplicate rules follow existing domain constraints for WFH and Leave entries.
6. Public holidays are locked days; WFH and Leave cannot be added on those dates.

## API Plan

## Option A: New Combined Batch Endpoint (Future Consolidation)

1. POST /api/day-entries/batch
2. Request model:
   - month (YYYY-MM)
   - items[] with:
     - date (YYYY-MM-DD)
     - category (WFH | Leave)
     - entryType (FullDay | HalfDay | SpecificHours)
     - hours (required for SpecificHours)
3. Response model:
   - totalRequested
   - createdCount
   - skippedCount
   - failedCount
   - results[] per item with status and message

Benefits:
1. Single call from UI.
2. Unified validation and response.
3. Clear user feedback for mixed outcomes.

## Option B (Preferred for Now): Two Existing Batch Endpoints

1. POST /api/work-from-home/batch
2. POST /api/leave/batch

Trade-off:
1. Simpler domain isolation.
2. More frontend orchestration and split error handling.

## Frontend Architecture Plan

1. Add component:
   - Frontend/src/app/components/calendar-batch-entry/
2. Add service:
   - Frontend/src/app/services/day-entry-batch.ts (or extend existing leave/wfh services)
3. Add route in app.routes.ts.
4. Introduce view model:
   - CalendarDayRowVm
   - date, dayName, isWeekday, existingStatus, pendingStatus, entryType, hours, errors
5. Add helper utilities for:
   - month weekday generation
   - payload mapping
   - validation state

## Backend Architecture Plan

1. Application layer:
   - Add batch command/use case and validator(s)
   - Reuse existing Leave/WFH creation services where possible
2. API layer:
   - Add batch controller endpoint and DTOs
3. Infrastructure:
   - Ensure efficient duplicate checks for month batch (single query per category if possible)
4. Domain:
   - Keep invariants centralized; avoid duplicating hour/day rules in controller

## Validation and Error Handling

1. Frontend pre-validation:
   - Specific hours required when entry type = SpecificHours
   - Prevent submission when pending selection is empty
2. Backend authoritative validation:
   - Date format and range
   - Duplicate detection
   - Invalid category/type combinations
3. Partial failure handling:
   - Return HTTP 200 for mixed result batches with per-item status
   - Reserve 4xx/5xx for request-level failures

## Testing Strategy

1. Unit tests (frontend):
   - Out of scope for this plan because frontend unit test infrastructure/tests are not present in this project.
2. Unit tests (backend):
   - Batch validation rules
   - Duplicate skip behavior
   - Mixed success result mapping
3. Integration tests are not included in this plan.

## Delivery Phases

### Phase 1 - UX Skeleton and Routing

Status: [x] Complete

1. Add new page route and base component.
2. Render month navigation and weekday rows.
3. Implement local row selection model.

### Phase 2 - Batch API Contract

Status: [x] Complete

1. Implement separate batch endpoints for WFH and Leave.
2. Implement DTOs and application command.
3. Return detailed per-item result contract.

### Phase 3 - End-to-End Submission

Status: [x] Complete

1. Wire frontend payload to backend.
2. Add result summary and row-level error display.
3. Support retry for failed rows.

### Phase 4 - Quality and Hardening

Status: [x] Complete

1. Add backend unit tests only (no frontend unit tests, no integration tests).
2. Improve accessibility and keyboard interaction.
3. Validate behavior across month boundaries and leap years.

## Acceptance Criteria

1. A dedicated page exists and is reachable from the frontend navigation.
2. The page displays only Monday-Friday days for the selected month.
3. Users can mark days as WFH or Leave and choose entry type.
4. Batch Add submits all pending rows in one action.
5. User sees accurate result summary for created/skipped/failed rows.
6. Duplicate or invalid rows do not block valid rows from being created.
7. Tests cover core calendar logic and batch processing behavior.

## Risks and Mitigations

1. Risk: Duplicate detection performance for larger month payloads.
   - Mitigation: Preload existing entries per month/category in one query.
2. Risk: User confusion with mixed results.
   - Mitigation: Show row-level status chips and actionable retry path.
3. Risk: Date/time zone inconsistencies.
   - Mitigation: Use date-only types and ISO date strings end-to-end.

## Open Decisions

No open product decisions remain for the first pass.

Endpoint strategy is finalized as separate batch endpoints for WFH and Leave.

## Next Steps (Post First Pass)

No additional product decisions are required for the current roadmap.

Implementation follow-ups (non-decision):
1. Continue incremental UX polish and accessibility refinements on the calendar batch page.
2. Monitor batch usage and error trends for operational insight while retaining split endpoints.
3. Add backend unit tests for any newly discovered edge cases as they arise.

## Tracking Workflow

1. Update Current Progress Snapshot whenever a phase status changes.
2. Tick Milestone Checklist items as soon as they are verifiably complete.
3. Append a row to Progress Notes for each meaningful implementation step.
4. Record decisions in Decision Tracker once confirmed.
5. Keep status updates objective and test-backed where applicable.
