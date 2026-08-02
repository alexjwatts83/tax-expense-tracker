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

Decision: Decided 2026-08-02 (`DEC-HARD-001`)

- Use a single-tenant Microsoft Entra tenant with separate SPA and API app registrations because Azure plans separate frontend and API hosts.
- Expose one delegated API scope. Angular obtains access tokens through authorization code with PKCE; no client secret is present in the browser.
- Configure App Service Authentication on the API to reject unauthenticated requests and accept only the API audience.
- Validate the bearer token again in ASP.NET Core and require the configured tenant, audience, and sole allowed user object ID.
- Treat the sole allowed identity as the DataTransfer administrator through a named policy; do not build application roles, user tables, registration, or password flows.
- Use an explicit test authentication scheme in automated tests. Any local convenience identity must be available only when the host environment is `Development`; Production has no bypass.
- Keep tokens in memory through MSAL and never place a reusable API key or access token in committed files or browser local storage.

Deferred alternative:

- If the frontend and API later move behind one same-origin authenticated host, reassess whether platform cookies can replace the SPA token flow.

Claims and authorization contract:

- `tid` must match the configured tenant ID.
- `aud` must match the API application ID URI/client ID.
- `oid` must match `Authentication:AllowedUserObjectId`.
- The API scope must be present for delegated browser calls.
- A missing/invalid token returns 401; a valid token for a different user returns 403.

### DG-001A: API Error Contract

Decision: Decided 2026-08-02 (`DEC-HARD-002`)

Use `application/problem+json` and RFC 9457 ProblemDetails for every API error. Responses contain:

- `type`: stable URI ending in the machine-readable code.
- `title`: stable short category, not raw exception text.
- `status`: HTTP status code.
- `detail`: user-safe contextual message.
- `instance`: request path.
- `code`: stable snake-case application code.
- `correlationId`: the same value returned in `X-Correlation-ID`.
- `errors`: optional dictionary of field names to string arrays for validation failures.

Initial status/code mapping:

| HTTP | Code | Use |
|---:|---|---|
| 400 | `validation_error` | Invalid command, body, query, or field values |
| 400 | `invalid_reference` | Requested source, bank, tag, or related ID does not exist |
| 401 | `authentication_required` | Missing, expired, or invalid credential |
| 403 | `access_denied` | Authenticated identity is not the configured owner or lacks the required scope |
| 404 | `not_found` | Route or requested entity does not exist |
| 408 | `request_timeout` | Server-side operation timeout |
| 409 | `conflict` | Duplicate active date, restore collision, uniqueness race, or incompatible state |
| 413 | `payload_too_large` | Request exceeds the supported API limit |
| 500 | `server_error` | Unexpected failure; detail remains generic |

Do not expose exception types, stack traces, SQL details, tokens, or secrets. Framework model validation, middleware failures, authorization results, controller not-found results, and DataTransfer errors must use this contract.

### DG-002: Date Storage Contract

Decision: Decided 2026-08-02 (`DEC-HARD-003`)

- Use `DateOnly` for expense purchase date, `WorkLocationEntry.WorkDate`, `LeaveEntry.LeaveDate`, and `PublicHoliday.HolidayDate`.
- Preserve API JSON as ISO date values (`yyyy-MM-dd`).
- Treat calendar dates as timezone-free values. Do not call `ToUniversalTime`, construct a UTC midnight, or otherwise shift a calendar date.
- Normalize existing `DateTime` values by retaining their stored year, month, and day, then add uniqueness constraints.
- Use UTC instants for `CreatedAt`, `UpdatedAt`, and other audit timestamps, represented consistently as `DateTimeOffset` or explicitly UTC `DateTime` according to the later audit decision.
- Make date-range endpoints inclusive at both ends and compare calendar values rather than timestamp boundaries.
- Preserve these meanings and formats in DataTransfer payloads.

Migration rule:

- A stored value such as `2026-08-02 23:30` normalizes to `2026-08-02`, regardless of server timezone.
- Duplicate-date preflight runs after normalization and before unique indexes are created.
- Migration rollback and duplicate-repair steps are documented before applying the migration to Azure.

### DG-003: Batch Atomicity

Decision: Decided 2026-08-02 (`DEC-HARD-004`)

- Preserve the current mixed-result contract: valid items may succeed while invalid/conflicting items are reported individually.
- Optimize database access without changing partial-success semantics.
- Return HTTP 200 for a structurally valid batch with aggregate requested/created/skipped/failed counts and one ordered result per requested item.
- Keep stable item statuses such as `Created`, `SkippedDuplicate`, `FailedValidation`, and `FailedConflict` until a versioned contract intentionally changes them.
- Reject a malformed request body or invalid top-level contract before processing any item using the shared ProblemDetails response.
- Do not roll back successful items because a neighboring item is skipped or fails.
- Preserve request order in the item-result collection so the frontend can reconcile rows deterministically.

Frontend constraint:

- Calendar batch currently combines create batches with individual update/delete requests and explicitly reconciles partial results. Keep that orchestration truthful: update/delete requests can also succeed independently, and the UI must retain failed rows for correction.

Deferred alternative:

- A single atomic calendar change-set endpoint requires a new product decision, versioned contract, and corresponding frontend simplification. Do not introduce it as part of performance refactoring.

### DG-004: DataTransfer Limits and Progress

Decision: Decided 2026-08-02 (`DEC-HARD-005`)

- Set the maximum DataTransfer HTTP request body to 12 MiB at both ASP.NET Core and Azure hosting/proxy boundaries.
- Keep the browser-selected file limit at 10 MiB so request encoding and transport overhead stay below the server ceiling.
- Return the shared `payload_too_large` ProblemDetails response for API limit failures where the request reaches ASP.NET Core.
- Preserve the existing five-minute operation timeout initially and return `request_timeout` on expiry.
- Keep imports atomic. Cancellation before commit rolls back the operation; do not report cancellation after the transaction has committed.
- Do not expose fabricated percentage progress from a synchronous request. The API may report only real transport or server-job progress if a later design introduces it.
- Revisit limits only from measured representative payload size, browser memory, Azure proxy behavior, and import duration.

## Phase 0: Characterization and Safety Net

Status: In Progress

### Tasks

- [x] Capture representative route, JSON serialization, correlation-ID, mixed batch-result, and DataTransfer stream contracts.
- [ ] Add middleware characterization tests for existing validation and correlation-ID responses.
- [x] Add API integration test infrastructure using `WebApplicationFactory` with isolated in-memory SQLite databases.
- [x] Add architecture tests enforcing project dependency direction.
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

Execution order: begin immediately after the shared API error contract is complete, deploy to Azure dev, and pass the real-account smoke test before date or aggregate refactors.

### Tasks

- [ ] Add strongly typed Entra options for tenant, audience, API scope, and allowed user object ID with Production startup validation.
- [ ] Register JWT bearer validation and call `UseAuthentication()` before `UseAuthorization()`.
- [ ] Define a default allowed-user policy validating tenant, audience, scope, and object ID.
- [ ] Define a named DataTransfer policy over the same sole allowed identity, preserving an explicit security boundary without adding roles.
- [ ] Apply authorization consistently to all controllers.
- [ ] Keep health endpoints anonymous if introduced or already required by hosting.
- [ ] Add integration-test authentication and an explicit Development-only local identity mode without weakening Production defaults.
- [ ] Remove obsolete API key/JWT settings that are not part of the selected design.
- [ ] Add 401, wrong-user 403, allowed-user CRUD, missing-scope, wrong-audience, and DataTransfer integration tests.
- [ ] Configure API App Service Easy Auth, allowed audiences, and the frontend CORS origin through Terraform.
- [ ] Update Azure and local setup documentation, including Entra app registration and owner assignment.

### Acceptance Criteria

- Anonymous mutation and export/import requests are rejected.
- The configured owner identity can perform normal application and DataTransfer workflows.
- Other identities are forbidden even if they belong to the tenant.
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
- [x] Inject a logger into `ApiExceptionHandlingMiddleware`.
- [x] Log unexpected exceptions with correlation ID, route, and method.
- [x] Keep internal exception details out of 500 responses.
- [x] Introduce typed application exceptions for validation, conflict, not-found, and missing-reference cases.
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
