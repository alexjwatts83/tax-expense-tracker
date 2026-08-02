# Backend DDD and Clean Code Hardening Plan

Last Updated: 2026-08-02
Status: Planned

Cross-layer implementation order and progress are tracked in [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md). This document remains the detailed backend reference.

## Goal

Strengthen the existing backend so domain invariants are enforced consistently, API access is protected, persistence semantics match the domain, and architectural boundaries remain verifiable as the codebase grows.

This is a hardening plan, not another project restructure. The current dependency direction remains the target:

- API -> Application
- Infrastructure -> Application + Domain
- Application -> Domain
- Domain -> no outward project dependencies

## Review Baseline

The backend review confirmed that the project structure follows the intended Clean Architecture dependency direction. The main risks are within those boundaries:

1. API endpoints have no application-level authentication or authorization.
2. Work-location and leave restore flows can violate one-entry-per-day rules.
3. Date-only concepts are represented and queried with inconsistent `DateTime` semantics.
4. Public entity setters and mutable collections allow callers to bypass domain behavior.
5. Expense writes silently discard missing tag IDs.
6. Work-location and leave batch services duplicate logic and perform per-item reads/writes.
7. Unexpected API exceptions are not logged.
8. Entity-specific DataTransfer exports perform unnecessary reference-data queries.
9. Architecture, API, middleware, and real EF invariants need stronger automated coverage.

Current automated baseline:

- Unit tests: 96 passing
- Integration tests: 2 passing
- VS Code backend diagnostics: none

## Guiding Decisions

1. Preserve current routes and JSON contracts unless a contract change is explicitly documented.
2. Fix observable correctness and security risks before broad refactoring.
3. Add characterization tests before changing an existing rule.
4. Keep domain entities persistence-compatible without exposing unrestricted mutation.
5. Enforce cross-request uniqueness in the database as well as the application layer.
6. Preserve DataTransfer import atomicity and ID restoration behavior.
7. Preserve mixed-result batch behavior unless a product decision explicitly changes it.
8. Prefer focused domain methods over generic orchestration frameworks.
9. Deliver each phase as a small independently testable commit.

## Decision Gates

Resolve these decisions before the related phase begins.

### DG-001: Authentication Strategy

Recommended direction:

- Use Microsoft Entra ID / App Service Authentication for deployed browser-to-API access.
- Validate authenticated principals and authorization policies in ASP.NET Core.
- Use a development authentication scheme or explicit local bypass restricted to Development.
- Do not place a reusable API key in the Angular client.

Alternatives:

- ASP.NET Core JWT bearer validation with tokens issued by a trusted identity provider.
- API key authentication only for non-browser automation endpoints, stored outside source control.

Required outcome:

- Record the selected identity provider, local-development behavior, and admin policy for DataTransfer operations.
- Define the Angular login/session flow, claim mapping, deep-link behavior, and 401/403 handling before implementation.

### DG-002: Date Storage Contract

Recommended direction:

- Use `DateOnly` for `WorkLocationEntry.WorkDate`, `LeaveEntry.LeaveDate`, and `PublicHoliday.HolidayDate` where no time-of-day meaning exists.
- Preserve API JSON as ISO date values (`yyyy-MM-dd`).
- Normalize and migrate existing values before adding uniqueness constraints.

Required outcome:

- Confirm whether historical non-midnight values should be truncated to their local calendar date or UTC calendar date. Current frontend behavior indicates local calendar date is intended.
- Preserve the frontend calendar-date wire contract as ISO `yyyy-MM-dd` and distinguish it from UTC audit timestamps.

### DG-003: Batch Atomicity

Recommended direction:

- Preserve the current mixed-result contract: valid items may succeed while invalid/conflicting items are reported individually.
- Optimize database access without changing partial-success semantics.

Frontend constraint:

- Calendar batch currently combines create batches with individual update/delete requests and explicitly reconciles partial results. Decide whether to preserve that contract or replace it with one transactional calendar change-set endpoint before refactoring either layer.

Alternative:

- Make each batch all-or-nothing and change the API result contract accordingly.

## Phase 0: Characterization and Safety Net

Status: Not Started

### Tasks

- [ ] Capture current route, serialization, and result contracts for critical workflows.
- [ ] Add middleware characterization tests for existing validation and correlation-ID responses.
- [ ] Add API integration test infrastructure using `WebApplicationFactory` or an equivalent test host.
- [ ] Add architecture tests enforcing project dependency direction.
- [ ] Record representative database fixtures for migration and duplicate-date testing.

### Acceptance Criteria

- Existing API contracts are captured before authentication and domain encapsulation changes.
- Architecture tests fail if Domain or Application gain forbidden outward references.
- The phase is green and mergeable; defect regression tests are added with their corresponding fixes.

### Suggested Commit

`test backend hardening baseline`

## Phase 1: Authentication and Authorization

Status: Not Started
Depends On: DG-001

### Tasks

- [ ] Add strongly typed authentication/security options with startup validation.
- [ ] Register the selected authentication handler and call `UseAuthentication()` before `UseAuthorization()`.
- [ ] Define a default authenticated-user policy for application endpoints.
- [ ] Define a separate administrator policy for DataTransfer import/export operations.
- [ ] Apply authorization consistently to all controllers.
- [ ] Keep health endpoints anonymous if introduced or already required by hosting.
- [ ] Configure local-development authentication without weakening Production defaults.
- [ ] Remove obsolete API key/JWT settings that are not part of the selected design.
- [ ] Add 401, 403, authenticated CRUD, and DataTransfer-admin integration tests.
- [ ] Update Angular authentication only as required by the selected provider.
- [ ] Update Azure and local setup documentation.

### Acceptance Criteria

- Anonymous mutation and export/import requests are rejected.
- Authenticated users can perform normal application workflows.
- Only the administrator policy can use DataTransfer endpoints.
- Production startup fails clearly when required identity settings are missing.
- No browser-delivered secret is used as an API credential.

### Suggested Commits

1. `add backend authentication policies`
2. `protect data transfer administration`
3. `document local and azure authentication`

## Phase 2: Date-Only Integrity and Restore Safety

Status: Not Started
Depends On: DG-002

### Tasks

- [ ] Add service regression tests proving restore is rejected when an active entry occupies the same work/leave date.
- [ ] Add real SQLite regression tests for non-midnight lookup and date-range behavior.
- [ ] Change date-only domain properties and application contracts to `DateOnly` where practical.
- [ ] Keep API JSON compatibility with ISO date serialization.
- [ ] Update summary-period, filtering, import/export, and CSV parsing code to use one date representation.
- [ ] Add restore conflict checks for work-location and leave entries.
- [ ] Add unique filtered indexes for active work-location and leave dates.
- [ ] Add an EF migration that normalizes existing values before changing column/index definitions.
- [ ] Add a migration preflight query or documented repair step for existing active duplicates.
- [ ] Translate unique-constraint races into a deterministic conflict response.
- [ ] Verify SQLite now and account for the planned Azure SQL provider.

### Database Constraint Direction

Target one active entry per date while allowing historical soft-deleted entries:

- Unique filtered index on `WorkDate` where `IsDeleted = 0`.
- Unique filtered index on `LeaveDate` where `IsDeleted = 0`.

The generated migration must be reviewed against both SQLite syntax and the planned Azure SQL migration path.

### Acceptance Criteria

- Create, update, restore, batch import, and DataTransfer import cannot produce two active entries for the same entity/date.
- Date-range queries include every entry on the requested final date.
- Same-day comparisons are independent of time components.
- Existing frontend date payloads continue to work unchanged.
- Migration rollback and duplicate-data handling are documented.

### Suggested Commits

1. `test date and restore invariants`
2. `use date-only work and leave semantics`
3. `enforce active date uniqueness`

## Phase 3: Aggregate Encapsulation

Status: Not Started

### Target Aggregate Behavior

- `TaxExpense` owns expense-tag links.
- `WorkLocationEntry` owns its date, location, entry type, hours, notes, and deletion state.
- `LeaveEntry` owns its date, leave type, entry type, hours, notes, and deletion state.
- Reference entities own rename, color, description, and deletion transitions.

### Tasks

- [ ] Replace public domain setters with private or protected setters supported by EF Core.
- [ ] Use private backing collections and expose read-only views for aggregate children.
- [ ] Add `TaxExpense.ReplaceTags`, `LinkTag`, and `UnlinkTag` behavior as needed.
- [ ] Move duplicate tag-link prevention into the aggregate.
- [ ] Stop Application and DataTransfer handlers from assigning aggregate collections directly.
- [ ] Introduce explicit import/rehydration factories for preserving IDs and deletion state.
- [ ] Remove `DateTime.UtcNow` property initializers and require explicit clock-driven creation paths.
- [ ] Decide and document whether Bank, Tag, and PublicHoliday require `UpdatedAt` auditing.
- [ ] Keep EF configuration in Infrastructure; do not add EF attributes to Domain.

### DataTransfer Compatibility

DataTransfer must continue to restore source IDs. Use explicit domain behavior such as `CreateForImport` or a narrowly scoped rehydration factory rather than reopening public setters.

### Acceptance Criteria

- Invalid prices, empty foreign keys, invalid enums, inconsistent hours, and arbitrary deletion state cannot be assigned by normal callers.
- Expense tag links can only change through `TaxExpense` behavior.
- EF materialization and migrations continue to work without persistence constructors leaking into application code.
- DataTransfer roundtrip tests continue to pass with original IDs.

### Suggested Commits

1. `encapsulate expense aggregate state`
2. `encapsulate time entry state`
3. `encapsulate reference entity state`

## Phase 4: Strict Reference Validation

Status: Not Started

### Tasks

- [ ] Add create/update regression tests defining the expected response for missing tag IDs.
- [ ] Compare requested expense tag IDs with repository results during create and update.
- [ ] Reject missing tag IDs with a typed application exception or validation result.
- [ ] Deduplicate requested IDs before validation and aggregate mutation.
- [ ] Keep source, bank, and tag reference failures behaviorally consistent.
- [ ] Map missing references to a stable HTTP response and error code.
- [ ] Ensure DataTransfer uses the same reference-validation policy where contracts align.
- [ ] Coordinate the error contract with frontend stale-tag recovery so rejected saves preserve user input and can refresh lookups safely.

### Acceptance Criteria

- Expense writes never silently omit requested tags.
- Error responses identify all missing tag IDs without exposing persistence details.
- Create and update have identical reference-validation semantics.
- Existing valid expense workflows remain unchanged.

### Suggested Commit

`reject missing expense tag references`

## Phase 5: Batch Use-Case Cleanup

Status: Not Started
Depends On: DG-003

### Tasks

- [ ] Bulk-load occupied work-location/leave dates once per batch.
- [ ] Preserve same-payload duplicate detection with `HashSet<DateOnly>`.
- [ ] Stage accepted entities before persistence.
- [ ] Reduce `SaveChangesAsync` calls while preserving the selected mixed-result semantics.
- [ ] Extract shared day-entry calculations and validation only where domain behavior is genuinely identical.
- [ ] Extract a small shared batch policy only if it removes proven duplication without obscuring feature language.
- [ ] Preserve feature-specific result DTOs and messages.
- [ ] Add query-count and large-batch regression tests.

### Acceptance Criteria

- Database query count remains bounded as batch size grows.
- One invalid item does not change the agreed partial-success behavior.
- Work-location and leave rules remain independently readable.
- Batch result counts and statuses remain backward compatible.

### Suggested Commits

1. `batch work and leave date lookups`
2. `share day entry domain rules`

## Phase 6: API Resilience and Observability

Status: Not Started

### Tasks

- [ ] Add middleware tests for unexpected exceptions and correlated problem responses.
- [ ] Inject a logger into `ApiExceptionHandlingMiddleware`.
- [ ] Log unexpected exceptions with correlation ID, route, and method.
- [ ] Keep internal exception details out of 500 responses.
- [ ] Introduce typed application exceptions or problem mappings for validation, not-found, and conflict cases.
- [ ] Return `409 Conflict` for uniqueness and state conflicts rather than treating all invalid operations as `400`.
- [ ] Add middleware and controller integration tests for problem details.
- [ ] Publish one stable problem contract for frontend classification, including status, code/type, detail, correlation ID, and optional field errors.
- [ ] Move automatic database migration policy behind an explicit startup/deployment decision.
- [ ] Validate required Production configuration at startup.

### Acceptance Criteria

- Every unexpected 500 response has a searchable correlated server log.
- Validation, conflict, not-found, unauthorized, and forbidden responses are distinguishable.
- User-facing responses do not leak stack traces or secrets.
- Production configuration failures are immediate and actionable.

### Suggested Commits

1. `log and classify api failures`
2. `validate production startup options`

## Phase 7: Query and DataTransfer Cleanup

Status: Not Started

### Tasks

- [ ] Avoid loading reference exports for expense, work-location, leave, and expense-tag-only exports.
- [ ] Validate `DataTransferImportMode` values before handler execution.
- [ ] Keep bulk repository methods bounded and tracked only where mutation is required.
- [ ] Profile expense paging/filter queries before changing query shape.
- [ ] Complete large-payload write chunking only if measurements justify it.
- [ ] Keep full-import transaction rollback semantics intact across any chunks.
- [ ] Align API request limits, browser parse limits, progress behavior, and cancellation with the frontend DataTransfer plan.

### Acceptance Criteria

- Per-entity exports query only their required data.
- Undefined import modes return validation errors and perform no writes.
- Query-count tests protect bounded import/export behavior.
- Performance changes include before/after measurements.

### Suggested Commit

`tighten data transfer query boundaries`

## Phase 8: Architecture and Quality Gates

Status: Not Started

### Tasks

- [ ] Enforce Domain has no references to Application, Infrastructure, or API.
- [ ] Enforce Application has no references to Infrastructure or API.
- [ ] Enforce Infrastructure contains no controller/use-case orchestration.
- [ ] Add API route/serialization smoke coverage for every controller.
- [ ] Add real EF tests for query filters, filtered unique indexes, and foreign keys.
- [ ] Add BankService coverage and complete reference-service update/delete coverage.
- [ ] Keep unit tests focused on domain/application behavior and integration tests focused on boundaries.
- [ ] Update CI to run architecture, unit, integration, and migration checks.

### Acceptance Criteria

- Forbidden project dependencies fail CI.
- Every persistence invariant has at least one real-database test.
- Every public API area has route, authorization, and representative contract coverage.
- CI can apply migrations to an empty database and run the backend suites.

### Suggested Commits

1. `enforce clean architecture dependencies`
2. `expand backend boundary tests`

## Recommended Delivery Order

1. Phase 0: characterization tests
2. Phase 1: authentication and authorization
3. Phase 2: date-only integrity and restore safety
4. Phase 4: strict reference validation
5. Phase 3: aggregate encapsulation
6. Phase 5: batch cleanup
7. Phase 6: API resilience and observability
8. Phase 7: query and DataTransfer cleanup
9. Phase 8: architecture and quality gates

Phase 4 precedes the larger aggregate refactor because it fixes user-visible silent data loss with a small behavioral change. Phase 3 then establishes the long-term domain ownership model.

## Cross-Layer Delivery Sequence

1. Agree identity/session, error, date, calendar change-set, and DataTransfer size contracts.
2. Land backend enforcement and frontend handling behind compatible configuration where needed.
3. Deploy authentication enforcement and authentication UX together.
4. Preserve ISO calendar-date payloads while backend storage/domain types change.
5. Land typed backend problem responses before replacing frontend error extraction.
6. Decide calendar partial-versus-atomic semantics before batch service/component refactors.
7. Test strict tag-reference rejection with stale frontend lookup state.

## Validation Matrix

Run as applicable after every phase:

```powershell
dotnet build TaxExpenseTracker.sln --no-restore
dotnet test Backend/TaxExpenseTracker.Tests.Unit/TaxExpenseTracker.Tests.Unit.csproj --no-restore
dotnet test Backend/TaxExpenseTracker.Tests.Integration/TaxExpenseTracker.Tests.Integration.csproj --no-restore
dotnet ef migrations script --project Backend/TaxExpenseTracker.Infrastructure --startup-project Backend/TaxExpenseTracker.Api
```

Additional phase-specific validation:

- Authentication: anonymous/authenticated/admin API matrix.
- Date migration: upgrade a copy of representative data and inspect duplicate handling.
- Encapsulation: DataTransfer export/import roundtrip with preserved IDs.
- Batch optimization: query-count and representative large-payload tests.
- Azure readiness: deployed frontend/API login and authorization smoke test.

## Risks and Mitigations

### Risk: Authentication blocks local development

Mitigation: implement an explicit Development-only identity path and test Production startup separately.

### Risk: Date migration changes historical calendar days

Mitigation: decide local-versus-UTC truncation first, back up the database, and report affected rows before migration.

### Risk: Unique index creation fails on existing duplicates

Mitigation: include a preflight report and deterministic repair procedure before applying the index.

### Risk: Private setters break EF or DataTransfer

Mitigation: refactor one aggregate at a time with real EF materialization and roundtrip tests.

### Risk: Shared batch abstraction hides domain language

Mitigation: extract calculations/policies first; keep feature services separate unless duplication remains substantial.

### Risk: Security configuration diverges between local and Azure

Mitigation: use strongly typed validated options and document environment-specific values without committing secrets.

## Definition of Done

- API access is authenticated and DataTransfer operations require explicit administrator authorization.
- Work-location and leave date invariants hold across create, update, restore, batch, import, and concurrent requests.
- Date-only concepts use one consistent representation from API through persistence.
- Domain aggregates prevent ordinary callers from bypassing invariants.
- Expense writes reject missing references instead of silently changing intent.
- Batch query counts remain bounded without accidental contract changes.
- Unexpected API failures are logged and correlated while responses remain sanitized.
- DataTransfer queries load only required data and retain atomic imports.
- Architecture and persistence invariants are enforced in CI.
