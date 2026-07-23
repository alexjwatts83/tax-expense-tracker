# Work Location Rename Plan

## Status

Overall Status: Complete
Last Updated: 2026-07-23

## Execution Log

1. 2026-07-23: Created branch feature/work-location-rename-all-phases.
2. 2026-07-23: Started implementation by adding canonical API route /api/work-locations while keeping /api/work-from-home for backward compatibility.
3. 2026-07-23: Renamed backend WorkFromHome symbols/files/folders to WorkLocation across Domain, Application, Infrastructure, API, and unit tests.
4. 2026-07-23: Renamed frontend WorkFromHome models, service, component, and route references to WorkLocation.
5. 2026-07-23: Added compatibility mapping so WorkLocationEntry still maps to existing table WorkFromHomeEntries.
6. 2026-07-23: Validation checkpoint reached. Application, Infrastructure, Unit Tests, and Frontend builds pass; API project still hits existing staticwebassets copy error (MSB3030).
7. 2026-07-23: Resolved API staticwebassets build failure by removing recursively generated nested Backend output folders under project directories.
8. 2026-07-23: Full validation passed: dotnet build TaxExpenseTracker.sln, dotnet test TaxExpenseTracker.Tests.Unit, and npm --prefix Frontend run build.
9. 2026-07-23: Removed legacy compatibility routes (/api/work-from-home and /work-from-home) as Phase 5 cleanup.
10. 2026-07-23: Completed optional DB rename by mapping and migrating WorkFromHomeEntries table to WorkLocationEntries.

## Goal

Rename the WorkFromHome feature to WorkLocation across backend and frontend so the model reflects both WFH and Office entries while keeping a safe migration path.

## Scope

In Scope:
1. Backend symbol and namespace rename from WorkFromHome* to WorkLocation*.
2. API model and controller rename.
3. Frontend entity, service, component, and route rename.
4. Backward compatibility for legacy API and SPA routes during transition.
5. Build and test validation.

Out of Scope (for this phase):
1. Breaking removal of legacy routes in the same release.

## Progress Snapshot

1. Phase 1 - Backend Domain and Application Rename: Complete
2. Phase 2 - API Rename and Dual Route Support: Complete
3. Phase 3 - Frontend Entities, Services, and Route Rename: Complete
4. Phase 4 - Validation, Regression Testing, and Docs: Complete
5. Phase 5 - Legacy Cleanup (Optional Follow-up): Complete

## Implementation Plan

### Phase 1: Backend Domain and Application Rename (Complete)

1. Rename domain entity and references:
	- WorkFromHomeEntry -> WorkLocationEntry
2. Rename application folder and namespace:
	- TaxExpenseTracker.Application.WorkFromHome -> TaxExpenseTracker.Application.WorkLocation
3. Rename core contracts and DTOs:
	- IWorkFromHomeService -> IWorkLocationService
	- IWorkFromHomeRepository -> IWorkLocationRepository
	- WorkFromHomeReadDto -> WorkLocationReadDto
	- CreateWorkFromHomeCommand -> CreateWorkLocationCommand
	- UpdateWorkFromHomeCommand -> UpdateWorkLocationCommand
	- BatchCreateWorkFromHomeResult -> BatchCreateWorkLocationResult
4. Rename service implementation:
	- WorkFromHomeService -> WorkLocationService

### Phase 2: API Rename and Dual Route Support

1. Rename API controller and models:
	- WorkFromHomeController -> WorkLocationController
	- WorkFromHomeDto -> WorkLocationDto
	- CreateWorkFromHomeDto -> CreateWorkLocationDto
	- CreateWorkFromHomeBatchDto -> CreateWorkLocationBatchDto
	- WorkFromHomeBatchResultDto -> WorkLocationBatchResultDto
2. Introduce canonical route:
	- /api/work-locations
3. Keep legacy route during transition:
	- /api/work-from-home
4. Update DI registrations and imports in Program.cs.

### Phase 3: Frontend Entities, Services, and Route Rename

1. Update API model types:
	- WorkFromHomeEntry -> WorkLocationEntry
	- CreateWorkFromHomeRequest -> CreateWorkLocationRequest
	- WorkFromHomeBatchCreateRequest -> WorkLocationBatchCreateRequest
	- WorkFromHomeBatchCreateResult -> WorkLocationBatchCreateResult
2. Rename service and endpoint:
	- WorkFromHomeService -> WorkLocationService
	- Base API path -> /api/work-locations
3. Rename component feature:
	- work-from-home-management -> work-location-management
4. Update frontend route path and compatibility redirect:
	- Canonical: /work-locations
	- Legacy redirect: /work-from-home -> /work-locations

### Phase 4: Validation, Regression Testing, and Docs

1. Run backend build and tests.
2. Run frontend build and tests.
3. Validate key flows:
	- Create/update/delete/restore entry
	- Batch create
	- Weekly/monthly summary
	- Date range filtering
4. Update README, API samples, and plan references.

### Phase 5: Legacy Cleanup (Optional Follow-up)

1. Remove legacy API route after adoption period.
2. Remove frontend legacy redirect.
3. Rename DB table and constraints via dedicated migration:
	- WorkFromHomeEntries -> WorkLocationEntries

## Risks and Mitigations

1. Risk: Breaking existing clients.
	- Mitigation: Keep legacy API and SPA routes during transition.
2. Risk: Broad rename misses references.
	- Mitigation: Compile after each phase and run targeted tests.
3. Risk: Data migration complexity.
	- Mitigation: Defer table rename until post-rollout stabilization.

## Exit Criteria

1. All backend and frontend code uses WorkLocation naming as canonical.
2. Canonical routes are in place and legacy routes are removed after transition.
3. Backend and frontend builds pass.
4. Critical feature flows are validated without regression.
