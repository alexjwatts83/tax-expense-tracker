# Leave Type Classification Plan

## Status

Overall Status: In Progress
Last Updated: 2026-07-23

## Goal

Extend Leave entries so each record has a Leave Type with two supported values:
1. Annual Leave
2. Sick Leave

This should mirror how Work Location evolved to support multiple types (WFH and Office), while preserving existing leave behavior and data.

## Progress Snapshot

1. Phase 1 - Domain Model and Contracts: Complete
2. Phase 2 - Application and API Integration: Complete
3. Phase 3 - Persistence and Migration: Complete
4. Phase 4 - Frontend UX and Models: In Progress
5. Phase 5 - Tests, Validation, and Documentation: In Progress

## Progress Tracking

| Date | Owner | Update | Phase | Status |
| --- | --- | --- | --- | --- |
| 2026-07-23 | Team | Plan created with end-to-end change scope and rollout steps. | Planning | Complete |
| 2026-07-23 | Copilot | Implemented LeaveType through domain, API, and migration layers; backend build and unit tests passed. | Backend | Complete |
| 2026-07-23 | Copilot | Added frontend LeaveType model, leave form selector, leave list column, and calendar batch leave type handling. | Frontend | In Progress |
| 2026-07-23 | Copilot | Added/updated backend leave service unit coverage for explicit LeaveType create, update, and batch flows; targeted unit tests and frontend build passed. | Validation | In Progress |

## Proposed Design

### New Domain Enum

Add a new enum in Domain:
1. LeaveType.Annual = 1
2. LeaveType.Sick = 2

### LeaveEntry Changes

Add LeaveType to LeaveEntry:
1. New property: LeaveType LeaveType
2. Add LeaveType parameter to Create and Update methods
3. Validate enum values in domain methods
4. Default strategy for old callers: Annual (temporary during migration)

## Impacted Areas

### Backend Domain

1. Backend/TaxExpenseTracker.Domain/Entities/LeaveEntry.cs
2. New file: Backend/TaxExpenseTracker.Domain/Entities/LeaveType.cs

### Application Layer

1. Backend/TaxExpenseTracker.Application/Leave/LeaveDtos.cs
2. Backend/TaxExpenseTracker.Application/Leave/LeaveService.cs
3. Backend/TaxExpenseTracker.Application/Leave/ILeaveService.cs (if signatures need update)
4. Backend/TaxExpenseTracker.Application/Leave/ILeaveRepository.cs (only if filters/queries are extended)

### API Layer

1. Backend/TaxExpenseTracker.Api/Models/CreateLeaveDto.cs
2. Backend/TaxExpenseTracker.Api/Models/LeaveDto.cs
3. Backend/TaxExpenseTracker.Api/Models/LeaveBatchResultDto.cs
4. Backend/TaxExpenseTracker.Api/Controllers/LeaveController.cs

### Infrastructure

1. Backend/TaxExpenseTracker.Infrastructure/Data/AppDbContext.cs (if explicit mapping changes needed)
2. Backend/TaxExpenseTracker.Infrastructure/Data/EfLeaveRepository.cs (if leave-type filtering is added)
3. New EF migration adding LeaveType column to LeaveEntries
4. Backend/TaxExpenseTracker.Infrastructure/Migrations/AppDbContextModelSnapshot.cs

### Frontend

1. Frontend/src/app/models/api.models.ts
2. Frontend/src/app/services/leave.ts
3. Frontend/src/app/components/leave-management/leave-management.ts
4. Frontend/src/app/components/leave-management/leave-management.html
5. Frontend/src/app/components/calendar-batch-entry/calendar-batch-entry.ts
6. Frontend/src/app/components/calendar-batch-entry/calendar-batch-entry.html

### Tests

1. Backend/TaxExpenseTracker.Tests.Unit/LeaveServiceTests.cs
2. Any related integration tests if leave payload contracts are validated
3. Frontend behavior validation via build and manual flow checks

## Implementation Phases

### Phase 1 - Domain Model and Contracts

Status: [x] Complete

1. Add LeaveType enum (Annual, Sick).
2. Add LeaveType to LeaveEntry entity.
3. Update Create/Update signatures and validation.
4. Ensure existing behavior for hours and dates remains unchanged.

Exit Criteria:
1. Domain compiles.
2. LeaveEntry fully supports LeaveType.

### Phase 2 - Application and API Integration

Status: [x] Complete

1. Add LeaveType to leave DTOs and commands.
2. Pass LeaveType through service create/update/batch flows.
3. Update API request/response models with LeaveType.
4. Update LeaveController mappings.

Exit Criteria:
1. API contracts include LeaveType.
2. Endpoints serialize/deserialize LeaveType correctly.

### Phase 3 - Persistence and Migration

Status: [x] Complete

1. Add LeaveType column to LeaveEntries via migration.
2. Set default value for existing rows to Annual (1).
3. Confirm migration upgrades existing databases safely.

Exit Criteria:
1. Migration applies successfully.
2. Existing leave rows remain readable and valid.

### Phase 4 - Frontend UX and Models

Status: [~] In Progress

1. Add LeaveType enum/type to frontend models.
2. Add Leave Type selector to leave-management form.
3. Display Leave Type in leave list/table.
4. Include LeaveType in calendar batch leave payloads.

Progress:
1. Frontend shared models now include LeaveType.
2. Leave management exposes Leave Type in the form and table.
3. Calendar batch leave rows carry LeaveType through create/update payloads.

Exit Criteria:
1. User can create/edit Annual and Sick leave entries.
2. Existing UI flows continue to work.

### Phase 5 - Tests, Validation, and Documentation

Status: [~] In Progress

1. Add/update unit tests for domain and service behavior with both leave types.
2. Run backend build + unit tests + integration tests.
3. Run frontend build and manual smoke checks.
4. Update README and relevant plans after implementation.

Progress:
1. Backend unit tests now cover explicit LeaveType create, update, and batch cases.
2. Frontend build passes.
3. Manual smoke checks and README updates remain.

Exit Criteria:
1. All tests and builds pass.
2. Docs reflect new Leave Type behavior.

## Risks and Mitigations

1. Risk: Breaking API clients that do not send LeaveType.
- Mitigation: Use API-side default (Annual) for transition window if needed.

2. Risk: Existing rows have null/unknown type during migration.
- Mitigation: Non-nullable column with default value of Annual.

3. Risk: Batch flows miss LeaveType mapping.
- Mitigation: Add explicit tests for batch create and result mapping.

## Validation Checklist

1. Create Annual leave entry works.
2. Create Sick leave entry works.
3. Update existing leave entry type works.
4. Batch create supports both leave types.
5. Summary endpoints remain correct.
6. Soft delete and restore remain unchanged.

## Rollout Notes

1. Apply migration before or during API startup migration window.
2. Verify leave-management and calendar-batch-entry flows in UI.
3. Monitor API errors for missing or invalid LeaveType payloads after rollout.
