# Full-Stack Hardening Implementation Plan

## Plan Status Snapshot

- **Status:** In Progress - 92% Complete
- **Assessed:** 2026-08-23
- **Evidence:** 68 of 74 tasks are complete. Stages 0, 1, 3, 4, 5, 6, and 7 are complete; Stage 2 is 8 of 10 (blocked on Azure), and Stage 8 is 3 of 7.
- **Remaining gate:** Stage 2's Azure verification (S2-09/S2-10) is externally blocked. Stage 8 continues with Slice B (measured performance work).
- **Authority:** This snapshot supersedes older progress text below until the next dated assessment.

Last Updated: 2026-08-23
Status: In Progress
Overall Progress: 68/74 tasks (92%)
Current Stage: Stage 8 - Quality Gates and Operational Readiness
Next Task: S8-04/S8-05 / Slice B measured performance work

## Purpose

Deliver backend DDD, API security and resilience, frontend clean-code, workflow, and contract hardening in the safest implementation order. Work is organized as vertical increments: agree the shared contract, implement backend authority, implement frontend behavior, then validate the complete workflow before moving on.

This is the sole hardening plan and progress tracker. It consolidates the former backend, frontend, and full-stack plans. Its stages control delivery order, its decision log controls shared contracts, and its implementation guidance controls layer-specific work.

## Target Architecture

Preserve the existing Clean Architecture dependency direction:

- API -> Application
- Infrastructure -> Application + Domain
- Application -> Domain
- Domain -> no outward project dependencies

Backend code remains the authority for security, persistence constraints, and domain invariants. Angular route guards and validation improve the experience but never replace API authorization or server-side validation. Preserve current routes and JSON contracts unless a recorded decision explicitly approves a breaking change.

Frontend components should own interaction and presentation rather than multi-entity workflow policy. Keep HTTP services small, move reusable orchestration into focused facades or pure modules, and introduce shared state only after measuring a concrete need. Do not add NgRx or another global state library solely for reference lookups.

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete and validated
- `[!]` Blocked; add an entry to the blocker log
- `[-]` Removed from scope by a recorded decision

Progress counts only `[x]` tasks. A stage is complete only when its validation gate is complete.

## Progress Dashboard

| Stage | Scope | Done | Total | Progress | Status | Depends On |
|---|---|---:|---:|---:|---|---|
| 0 | Baseline and contract decisions | 9 | 9 | 100% | Complete | None |
| 1 | Error contract and immediate defects | 8 | 8 | 100% | Complete | Stage 0 |
| 2 | Authentication and authorization | 8 | 10 | 80% | Blocked | Stages 0-1 |
| 3 | Date integrity and restore safety | 10 | 10 | 100% | Complete | Stages 0-1 |
| 4 | Expense references and aggregate ownership | 9 | 9 | 100% | Complete | Stages 1, 3 |
| 5 | Calendar and day-entry workflow | 8 | 8 | 100% | Complete | Stages 1, 3 |
| 6 | Remaining domain encapsulation | 6 | 6 | 100% | Complete | Stages 3-5 |
| 7 | DataTransfer hardening | 7 | 7 | 100% | Complete | Stages 1-4 |
| 8 | Quality gates and operational readiness | 3 | 7 | 43% | In Progress | Stages 2-7 |
| **Total** |  | **68** | **74** | **92%** | **In Progress** |  |

## Delivery Rules

1. Keep every commit buildable and every completed stage deployable.
2. Backend is the security and invariant authority; frontend guards and validation improve UX only.
3. Preserve wire compatibility while internals change unless a contract decision explicitly approves a breaking change.
4. Add a regression test with each defect fix; do not commit intentionally failing tests alone.
5. Update this dashboard, task checkboxes, validation log, and `Last Updated` after each work session.
6. Do not start a dependent stage while its decision or validation gate is incomplete.
7. Do not combine authentication rollout, date migration, and aggregate encapsulation in one release.
8. Keep generated migrations and contract changes in separate reviewable commits when practical.
9. Add characterization tests before changing established behavior.
10. Keep domain entities persistence-compatible without exposing unrestricted mutation; EF configuration remains in Infrastructure.
11. Use runtime validation at untrusted boundaries rather than duplicating it throughout the frontend.
12. Preserve DataTransfer import atomicity and source-ID restoration while internals are hardened.

## Stage 0: Baseline and Contract Decisions

Goal: create reliable test boundaries and decide the contracts that later stages depend on.

### Tasks

- [x] **S0-01** Add backend API test hosting with an isolated test database.
- [x] **S0-02** Add frontend HTTP service test infrastructure and a non-watch CI command.
- [x] **S0-03** Add backend architecture tests for project dependency direction.
- [x] **S0-04** Capture representative API route, JSON, correlation-ID, batch-result, and DataTransfer contracts.
- [x] **S0-05** Decide identity provider, credential flow, claims, admin policy, and Development behavior (`DEC-HARD-001`).
- [x] **S0-06** Decide the common API problem/error shape (`DEC-HARD-002`).
- [x] **S0-07** Decide calendar-date versus instant contracts and expense-date semantics (`DEC-HARD-003`).
- [x] **S0-08** Decide calendar partial-success versus atomic change-set behavior (`DEC-HARD-004`).
- [x] **S0-09** Decide API and browser DataTransfer size/progress limits (`DEC-HARD-005`); stage validation gate tracked separately below.

### Recommended Decisions

- Identity: single-tenant Microsoft Entra ID with separate SPA and API registrations; Angular MSAL authorization-code flow with PKCE; bearer-token validation in App Service and ASP.NET Core; only the configured Entra object ID is authorized; no browser secret or app-owned user store.
- Errors: RFC 9457 ProblemDetails with stable `code`/`type`, title, status, safe detail, request instance, matching correlation ID, and optional field-error dictionary.
- Dates: `yyyy-MM-dd`/`DateOnly` for expense purchase, work, leave, and holiday dates; UTC ISO timestamps for audit instants; no timezone conversion of calendar dates.
- Calendar: preserve mixed partial-success results with deterministic per-item statuses; do not add an atomic change-set endpoint without a new product decision.
- DataTransfer: 10 MiB browser file limit and 12 MiB API request limit; reject before reading, parse supported files in a worker, and show stage-based status without invented percentages.

### Validation Gate

- Backend unit and integration suites pass.
- Frontend tests and production build pass.
- Five decisions are recorded in the decision log.
- Test infrastructure is reusable by later stages.

### Suggested Commits

1. `test backend api boundaries`
2. `test frontend service boundaries`
3. `document hardening contract decisions`

## Stage 1: Error Contract and Immediate Defects

Goal: establish predictable errors before authentication and invariant changes, while fixing independent frontend defects.

### Tasks

- [x] **S1-01** Add typed backend application exceptions or result types for validation, conflict, not found, and missing references.
- [x] **S1-02** Log unexpected exceptions with route, method, exception, and correlation ID.
- [x] **S1-03** Map backend errors to the agreed ProblemDetails contract and status codes.
- [x] **S1-04** Add middleware/API integration tests for 400, 404, 409, and 500 responses.
- [x] **S1-05** Add typed frontend `ApiProblem` parsing/classification for JSON, text, and Blob responses.
- [x] **S1-06** Replace duplicated component error extraction with the shared frontend utility.
- [x] **S1-07** Fix the frontend zero-price expense filter and add a request-parameter regression test.
- [x] **S1-08** Validate normal and Blob errors end to end and pass the stage validation gate.

### Backend First

Land the backend contract before converting every frontend component, but deploy both sides together if status/code behavior changes.

### Acceptance Criteria

- Every unexpected 500 is logged and correlated without leaking internals.
- Frontend messages are always strings and can decode export errors returned as Blob.
- Validation and conflict errors are distinguishable.
- A price cap of `0` reaches the expense filter endpoint.

### Suggested Commits

1. `classify and log api failures`
2. `centralize frontend api errors`
3. `fix zero price expense filtering`

## Stage 2: Authentication and Authorization

Goal: protect API data and complete a usable browser session flow in one release capability.

Start this stage immediately after the Stage 1 shared error contract is complete. Validate it in Azure dev before beginning date, aggregate, or calendar refactors.

### Tasks

- [x] **S2-01** Add validated single-tenant Entra options, including tenant, API audience, and allowed user object ID.
- [x] **S2-02** Register JWT bearer validation and place `UseAuthentication()` before authorization.
- [x] **S2-03** Add an allowed-user policy and a named DataTransfer policy; both authorize the sole configured identity without introducing application roles.
- [x] **S2-04** Apply policies to every controller and document intentionally anonymous endpoints. No API endpoints are intentionally anonymous.
- [x] **S2-05** Add backend 401/403/authenticated/admin integration tests.
- [x] **S2-06** Add MSAL Angular using authorization code with PKCE, in-memory token caching, and an API-scope interceptor.
- [x] **S2-07** Add authenticated and administrator route guards plus role-aware navigation.
- [x] **S2-08** Add login, logout, loading, expired-session, access-denied, and deep-link behavior.
- [!] **S2-09** Provision separate SPA/API Entra registrations, expose and consent the API scope, assign only the owner, configure API Easy Auth, CORS, redirect URIs, and a Development-only test scheme. Local scheme and configuration are complete; Azure resources are not provisioned.
- [!] **S2-10** Run the anonymous/user/admin end-to-end matrix and pass the stage validation gate. Local matrix passes; Azure dev matrix awaits S2-09.

### Rollout Constraint

Backend enforcement and frontend authentication UX must be deployed together or behind coordinated configuration. Route guards do not replace server policies.

### Acceptance Criteria

- Anonymous API writes and exports/imports are rejected.
- The configured Entra identity can use normal workflows and DataTransfer.
- Other tenant users are rejected even if they can authenticate with Entra.
- 401 recovers the session as designed; 403 does not cause redirect loops.
- Production startup fails clearly for invalid identity configuration.
- Azure dev authentication is verified before Stage 3 begins.

### Suggested Commits

1. `add api authentication policies`
2. `add frontend authentication experience`
3. `protect data transfer administration`

## Stage 3: Date Integrity and Restore Safety

Goal: make date-only rules consistent from Angular through the domain and database.

### Tasks

- [x] **S3-01** Add backend regression tests for restore conflicts and non-midnight date behavior.
- [x] **S3-02** Add frontend date utility/contract tests including invalid dates, leap days, and timezones.
- [x] **S3-03** Introduce frontend calendar-date and UTC-timestamp types and strict utilities.
- [x] **S3-04** Preserve `yyyy-MM-dd` calendar-date JSON across forms, filters, summaries, and DataTransfer.
- [x] **S3-05** Convert backend date-only domain/application contracts to `DateOnly` where agreed.
- [x] **S3-06** Add restore conflict checks for work-location and leave entries.
- [x] **S3-07** Add filtered unique indexes for active work-location and leave dates.
- [x] **S3-08** Create migration normalization, duplicate preflight, repair, and rollback steps.
- [x] **S3-09** Translate database uniqueness races to the agreed conflict response and frontend UX.
- [x] **S3-10** Validate upgrade, roundtrip, timezone, restore, batch, and import behavior and pass the stage gate.
### Backend First

Frontend date types/utilities can land before the migration because the wire format remains stable. Apply the backend migration only after frontend compatibility tests are green.

### Acceptance Criteria

- No create, update, restore, batch, or import path can produce duplicate active dates.
- Date-range queries include the requested final day.
- Calendar dates do not shift by timezone.
- Migration handling for existing duplicates is deterministic and documented.

### Suggested Commits

1. `separate calendar dates from timestamps`
2. `enforce work and leave restore conflicts`
3. `migrate active date uniqueness`

## Stage 4: Expense References and Aggregate Ownership

Goal: stop silent tag loss and make `TaxExpense` own its tag links and state transitions.

### Tasks

- [x] **S4-01** Add backend tests for missing, duplicate, and stale expense tag IDs on create/update.
- [x] **S4-02** Reject missing source, bank, and tag references consistently with stable codes.
- [x] **S4-03** Add frontend stale-tag recovery while preserving unsaved form state.
- [x] **S4-04** Extract shared frontend manual-tag parsing and resolution workflow.
- [x] **S4-05** Define and test partial tag-creation failure/retry behavior.
- [x] **S4-06** Add `TaxExpense` tag-link behavior and private backing collection.
- [x] **S4-07** Replace direct tag collection assignment/mutation in Application and DataTransfer.
- [x] **S4-08** Restrict `TaxExpense` mutation and add explicit import/rehydration behavior preserving IDs.
- [x] **S4-09** Run expense UI, API, EF, and DataTransfer roundtrip tests and pass the stage gate.

### Acceptance Criteria

- Expense writes never silently omit requested references.
- Both frontend expense entry points resolve tags identically.
- Only `TaxExpense` behavior changes expense-tag links.
- DataTransfer preserves IDs and relationships after encapsulation.

### Suggested Commits

1. `reject missing expense references`
2. `extract frontend tag resolution workflow`
3. `encapsulate expense aggregate state`

## Stage 5: Calendar and Day-Entry Workflow

Goal: bound backend batch queries and move complex frontend workflow policy out of components.

### Tasks

- [x] **S5-01** Implement the recorded partial-success or atomic change-set contract.
- [x] **S5-02** Bulk-load occupied work/leave dates and remove per-item existence queries.
- [x] **S5-03** Reduce save calls while preserving the selected transaction semantics.
- [x] **S5-04** Add backend query-count and mixed-result regression tests.
- [x] **S5-05** Extract frontend calendar change-set planning into a pure tested module.
- [x] **S5-06** Extract result reconciliation into a pure reducer/facade with mixed-outcome tests.
- [x] **S5-07** Share work/leave form and paging policies only where domain language stays clear.
- [x] **S5-08** Validate create/update/delete mixtures, retries, conflicts, and partial failures and pass the stage gate.

### Delivery Slices

- [x] **Slice A (S5-01-S5-04): Backend batch efficiency.** Preserve deterministic partial-success results, bulk-load occupied dates, save at most once, and enforce bounded repository operations with mixed-result and large-batch tests.
- [x] **Slice B (S5-05-S5-06): Pure calendar workflow.** Extract create/update/delete change-set planning and mixed-result reconciliation together, including retention of failed or skipped rows for correction and retry. Validate with focused pure-module tests and existing component tests before integration work.
- [x] **Slice C (S5-07-S5-08): Integration and stage gate.** Adopt shared work/leave form or paging policy only where behavior substantially matches, keep domain terminology explicit, cover mixed operations, retries, conflicts, and partial failures at component level, then run the full backend/frontend suites and production build.

Implement Slice B before Slice C so planning and reconciliation have a stable tested contract before component integration and any policy sharing decisions.

### Acceptance Criteria

- Backend query count is bounded as batch size grows.
- Frontend does not claim atomicity unless the API provides it.
- Calendar planning/reconciliation is testable without component rendering.
- Existing per-row status feedback remains available.

### Suggested Commits

1. `batch work and leave persistence`
2. `extract calendar change planning`
3. `extract day entry interaction policies`

## Stage 6: Remaining Domain Encapsulation

Goal: enforce domain behavior across time-entry and reference entities without breaking EF or APIs.

### Tasks

- [x] **S6-01** Replace public time-entry mutation with private/protected setters and domain methods.
- [x] **S6-02** Centralize shared day-entry hours rules without hiding feature terminology.
- [x] **S6-03** Encapsulate Tracker, Tag, Bank, PublicHoliday, and soft-delete transitions.
- [x] **S6-04** Remove hardcoded `DateTime.UtcNow` initializers and use explicit clock-driven creation.
- [x] **S6-05** Decide and implement consistent audit timestamps for mutable reference entities.
- [x] **S6-06** Validate EF materialization, imports, migrations, APIs, and frontend contracts and pass the stage gate.

### Delivery Slices

- [x] **Slice A (S6-01 + time-entry part of S6-04): Time-entry aggregates.** Replace public work-location and leave mutation with private/protected setters and domain methods, and move their creation to explicit clock-driven factories. Validate with focused domain/service tests plus EF materialization and DataTransfer roundtrip coverage before touching shared rules.
- [x] **Slice B (S6-02): Shared day-entry rules.** Centralize only genuinely shared day-entry hours rules while keeping work-location and leave terminology explicit. Depends on Slice A so both aggregates are encapsulated first.
- [x] **Slice C (S6-03 + reference-entity part of S6-04 + S6-05): Reference entities and audit policy.** Encapsulate Tracker, Tag, Bank, PublicHoliday, and soft-delete transitions, finish removing hardcoded `DateTime.UtcNow` initializers, and implement consistent audit timestamps. Reference imports preserve optional source timestamps while remaining compatible with payloads that omit them.
- [x] **Slice D (S6-06): Stage gate.** Validate EF materialization, imports, migrations, APIs, and frontend contracts, then run the full backend/frontend suites and production build.

Implement Slice A before Slice B, resolve `DEC-HARD-006` before starting Slice C, and run Slice D only after the encapsulation slices are green.

### Acceptance Criteria

- Normal callers cannot assign invalid entity state directly.
- EF remains configuration-owned by Infrastructure.
- Import factories are explicit and narrowly scoped.
- Existing HTTP contracts remain stable.

### Suggested Commits

1. `encapsulate time entry domain state`
2. `encapsulate reference entity state`

## Stage 7: DataTransfer Hardening

Goal: make DataTransfer efficient, role-restricted, and safe for supported browser payloads.

### Tasks

- [x] **S7-01** Route per-entity exports to only the owning repository. `PublicHoliday` is a hard-delete entity, so its export does not take `includeSoftDeleted`; trackers, tags, and banks continue to honor it.
- [x] **S7-02** Verify and test that a single import wraps all its entity handlers in one transaction so any failure rolls back the whole payload; confirm mode behavior (Upsert/InsertOnly/Replace) and dry-run non-persistence.
- [x] **S7-03** Enforce the `DEC-HARD-005` limits (10 MiB selected file, 12 MiB API request) by replacing the current 100 MiB `RequestSizeLimit` on the DataTransfer endpoints.
- [x] **S7-04** Reject oversized or non-JSON files in the frontend before `file.text()`/`JSON.parse()`, using the same 10 MiB ceiling from `DEC-HARD-005`.
- [x] **S7-05** Move supported large parsing off the UI thread only if measurements against the 10 MiB ceiling justify it. Measurement was 7.35 ms median / 8.29 ms p95 for a near-10 MiB payload, so a worker was not justified.
- [x] **S7-06** Complete the DataTransfer UX parts not already implemented: authorization states, cancellation only where the request is truly abortable, and truthful stage-based progress. Existing route/navbar/401-403 handling remains authoritative; active HTTP requests now expose real cancellation and Downloading/Validating/Importing states.
- [x] **S7-07** Run malformed, oversized, unauthorized, dry-run, rollback, and large-roundtrip tests and pass the stage gate.

### Delivery Slices

- [x] **Slice A (S7-01 + S7-02): Backend DataTransfer boundaries.** Route per-entity exports to their owning repository (public holidays remain hard-delete and export unconditionally), and verify/test single-transaction import atomicity and mode/dry-run behavior. Backend-only and lowest risk; validate with service/repository and query-count tests.
- [x] **Slice B (S7-03 + S7-04): File-limit contract.** Enforce the `DEC-HARD-005` limits on both sides of the boundary by lowering the API `RequestSizeLimit` from 100 MiB to 12 MiB and rejecting oversized or non-JSON files in the browser at the 10 MiB ceiling before reading. Ship together so API and browser cannot drift; validate with one oversized-payload roundtrip plus focused frontend rejection tests.
- [x] **Slice C (S7-05 + S7-06 + S7-07): Frontend UX and stage gate.** Complete authorization states, real cancellation, and truthful stage-based progress, reusing existing correlation IDs and error parsing; add a worker-based parse only if measurements against the 10 MiB ceiling justify it. Then run the full malformed, oversized, unauthorized, dry-run, rollback, and large-roundtrip matrix and pass the stage gate.

Run Slice A before Slice B, and Slice C last because its test matrix and progress behavior depend on the enforced limit. Slice B is the highest-value, lowest-effort security fix and may be pulled first if prioritizing by risk over layer.

### Acceptance Criteria

- Per-entity exports query only required data; reference exports honor `includeSoftDeleted` for the three soft-deletable entities (trackers, tags, banks), while hard-delete public holidays export unconditionally.
- Unsupported or oversized files are rejected before reading and do not freeze the browser.
- API and browser limits both match `DEC-HARD-005` (10 MiB file, 12 MiB API request).
- DataTransfer remains dry-run-first and administrator-only.
- Import atomicity and ID/relationship restoration remain intact under single-transaction rollback.

### Suggested Commits

1. `tighten data transfer query boundaries`
2. `harden data transfer file handling`

## Stage 8: Quality Gates and Operational Readiness

Goal: make the hardened architecture and workflows enforceable in CI and diagnosable in deployment.

### Tasks

- [x] **S8-01** Complete backend architecture, API route, repository, query-filter, FK, and uniqueness coverage.
- [x] **S8-02** Complete frontend service, critical component, guard, accessibility, and error-state coverage.
- [x] **S8-03** Add CI checks for backend build/tests, migration script, frontend tests, and production build.
- [ ] **S8-04** Measure reference-data requests and add lightweight frontend caching only if justified.
- [ ] **S8-05** Profile expense paging/filtering and DataTransfer chunking before further optimization.
- [ ] **S8-06** Finalize Production startup validation, migration execution policy, monitoring, and correlated logs, and confirm whether Azure SQL provider parity is in scope or SQLite remains the sole provider.
- [ ] **S8-07** Run local and Azure smoke matrices, update all plans/docs, and pass final sign-off.

### Delivery Slices

- [x] **Slice A (S8-01 + S8-02 + S8-03): Coverage and CI enforcement.** Complete backend architecture/route/repository/query-filter/FK/uniqueness coverage and frontend service/component/guard/accessibility/error-state coverage, then add CI checks that run backend build/tests, migration-script generation, frontend tests, and the production build. Group these because CI enforces the very suites completed here; use plain (non-idempotent) SQLite migration scripts in CI. Maps to the `enforce full stack quality gates` commit.
- [ ] **Slice B (S8-04 + S8-05): Measured performance work.** Measure reference-data request patterns and profile expense paging/filtering and DataTransfer chunking, adding caching or optimization only where a measurement justifies it and a cache-invalidation strategy exists. Independent of Slice A; likely near no-op and must not add speculative optimization.
- [ ] **Slice C (S8-06 + S8-07): Operational readiness and final sign-off.** Finalize Production startup validation, migration execution policy, monitoring, correlated logs, and the Azure SQL provider-scope decision, then run the local and Azure smoke matrices, update all plans/docs, and pass final sign-off.

Run Slice A first because CI must enforce the completed suites before sign-off, and run Slice C last because `S8-07` is the final gate. Slice B is measurement-gated and can run any time before Slice C. Slice C's Azure smoke matrix shares the external Azure/Entra provisioning blocker with `S2-09`/`S2-10`; the local matrix and all non-Azure items can complete independently.

### Acceptance Criteria

- Forbidden dependencies and persistence invariant regressions fail CI.
- Critical frontend workflows have success and failure coverage.
- Production configuration failures are immediate and actionable.
- No optimization is added without a measurement and invalidation strategy.
- Deployment smoke tests cover anonymous, user, and administrator roles.

### Suggested Commits

1. `enforce full stack quality gates`
2. `finalize hardening operations`

## Layer-Specific Implementation Guidance

### Backend Domain and Persistence

- `TaxExpense` owns expense-tag links through explicit behavior and a private backing collection.
- Work-location and leave entries own their date, type, hours, notes, and deletion transitions.
- Reference entities own rename, metadata, and soft-delete transitions.
- Import and rehydration paths preserve source IDs through explicit, narrowly scoped factories rather than public setters.
- Calendar-date fields use `DateOnly`; audit fields remain UTC instants. Existing non-midnight values normalize by retaining their stored year, month, and day.
- Enforce one active work-location or leave entry per date with application checks and filtered unique indexes. SQLite is the current provider; treat Azure SQL provider parity as out of scope unless confirmed in Stage 8 (`S8-06`).
- Keep unit tests focused on domain/application behavior and integration tests focused on HTTP, middleware, architecture, EF constraints, migrations, and serialization boundaries.

### Frontend Components and Workflows

- Error handling accepts `unknown` and produces a string from JSON, text, or Blob responses. Components provide contextual fallback copy while shared classification handles status, code, field errors, and correlation IDs.
- Calendar dates and UTC timestamps use distinct types and utilities. Never format a calendar date through `new Date('yyyy-MM-dd')`, `toISOString()`, or timezone conversion.
- Expense tag tokenization and resolution are shared by both expense entry points, including deterministic recovery after partial tag-creation failures.
- Calendar change-set planning and result reconciliation live in pure tested modules; the component retains rendering and interaction responsibilities.
- Share work-location and leave mechanics only where domain terminology remains explicit; avoid deeply generic base components.
- DataTransfer rejects unsupported or oversized files before reading, keeps parsing on the UI thread while near-ceiling measurements remain below the worker threshold, exposes truthful stages, supports cancellation where real, and invalidates dry-run approval when payload or options change.
- Frontend tests assert service contracts and user-visible behavior without depending on Angular Material internal markup.

### Operational Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Authentication blocks local development | Provide an explicit Development-only identity path and test Production startup separately. |
| Security configuration diverges between local and Azure | Use strongly typed validated options and document environment values without committing secrets. |
| Date migration changes historical calendar days | Back up data, normalize by stored calendar components, and report affected rows before migration. |
| Unique index creation fails on existing duplicates | Run a duplicate preflight and document deterministic repair before creating indexes. |
| Private setters break EF or DataTransfer | Refactor one aggregate at a time with real EF materialization and roundtrip tests. |
| Shared batch abstractions hide domain language | Extract calculations and policies first; keep feature services separate unless substantial duplication remains. |
| Browser import parsing freezes the UI | Enforce the 10 MiB pre-read limit and parse accepted files in a Web Worker. |

## Cross-Stage Validation Matrix

Run the relevant commands after every backend/frontend increment:

```powershell
dotnet build TaxExpenseTracker.sln --no-restore
dotnet test Backend/TaxExpenseTracker.Tests.Unit/TaxExpenseTracker.Tests.Unit.csproj --no-restore
dotnet test Backend/TaxExpenseTracker.Tests.Integration/TaxExpenseTracker.Tests.Integration.csproj --no-restore
dotnet ef migrations script --project Backend/TaxExpenseTracker.Infrastructure --startup-project Backend/TaxExpenseTracker.Api

Set-Location Frontend
npm run test:ci
npm run build
```

If a running API locks build outputs, stop it through the repository script before full build validation; do not treat a file lock as a code failure.

## Release Checkpoints

| Checkpoint | Included Stages | Deployable Outcome | Status |
|---|---|---|---|
| R1 | 0-1 | Tested contracts, correlated errors, immediate frontend fixes | Not Started |
| R2 | 2 | End-to-end authenticated application and admin authorization | Not Started |
| R3 | 3 | Date-safe work/leave workflows and database uniqueness | Complete |
| R4 | 4-5 | Strict expense references and maintainable calendar workflow | Complete |
| R5 | 6-7 | Encapsulated domain and hardened DataTransfer | Complete |
| R6 | 8 | CI-enforced, operationally ready hardening release | Not Started |

## Decision Log

| ID | Date | Decision | Choice | Rationale | Impacted Stages | Status |
|---|---|---|---|---|---|---|
| DEC-HARD-001 | 2026-08-02 | Identity/session architecture | Single-tenant Entra; separate SPA/API registrations; MSAL code + PKCE; JWT validation in Easy Auth and ASP.NET Core; one allowed object ID; sole user is DataTransfer admin | Planned Azure topology uses separate Static Web App and API App Service origins. This avoids app-owned passwords while restricting all data to the owner and keeping server authorization authoritative. | 2, 7, 8 | Decided |
| DEC-HARD-002 | 2026-08-02 | API problem/error contract | RFC 9457 ProblemDetails with `code`, stable `type`, `title`, `status`, safe `detail`, request `instance`, `correlationId`, and optional `errors` | Current exceptions, bare 404s, DataTransfer errors, and Angular extraction are inconsistent. One contract supports typed frontend behavior and correlated diagnostics without exposing internals. | 1-5, 7 | Decided |
| DEC-HARD-003 | 2026-08-02 | Calendar and expense date semantics | Expense purchase, work, leave, and holiday dates are timezone-free calendar dates serialized as `yyyy-MM-dd` and represented by `DateOnly` in backend contracts; audit values are UTC instants | Users enter and report these values by calendar day. Converting them through UTC can shift the day. Existing non-midnight values retain their stored year/month/day during normalization. | 3-7 | Decided |
| DEC-HARD-004 | 2026-08-02 | Calendar batch transaction semantics | Preserve partial success with deterministic per-item results and aggregate counts; malformed requests fail wholly, but one item failure does not roll back valid items | The API and Angular workflow already expose mixed outcomes, and users can correct only failed rows. Preserving this contract avoids a breaking endpoint and keeps feedback granular. | 5 | Decided |
| DEC-HARD-005 | 2026-08-02 | DataTransfer browser/API limits | 10 MiB maximum selected file; 12 MiB API request limit; reject before reading; stage-based status and cancellation only where real; use worker parsing only if measured latency justifies it | The smaller ceiling bounds browser memory risk. Near-ceiling `JSON.parse` measured 7.35 ms median / 8.29 ms p95 on 2026-08-23, so worker complexity is not currently justified. | 7 | Decided |
| DEC-HARD-006 | 2026-08-23 | Reference entity audit timestamps | All mutable reference entities have `CreatedAt` and `UpdatedAt`; every successful state change uses `TimeProvider` to update `UpdatedAt`; imports preserve optional source audit timestamps and otherwise use the injected clock | A consistent audit contract makes reference mutations diagnosable, removes hardcoded clocks, and preserves historical timestamps without breaking older import payloads that omit them. | 6 | Decided |

## Blocker Log

| Date | Task | Blocker | Owner | Resolution/Next Step | Status |
|---|---|---|---|---|---|
|  |  |  |  |  |  |
| 2026-08-23 | S2-09/S2-10 | Azure SPA/API registrations, consent, Easy Auth, and deployed role matrix require access to the target Azure tenant and final registration values | Owner | Provision Azure resources, configure runtime values, then run the documented anonymous/non-owner/owner matrix | Open |

## Validation Log

| Date | Stage/Task | Validation | Result | Notes |
|---|---|---|---|---|
| 2026-08-02 | Baseline | Backend unit tests | Pass: 96 | Existing baseline |
| 2026-08-02 | Baseline | Backend integration tests | Pass: 2 | Existing baseline |
| 2026-08-02 | Baseline | Frontend tests | Pass: 2 | Existing smoke tests only |
| 2026-08-02 | Baseline | Frontend production build | Pass with 2 existing budget warnings | Initial bundle and calendar day-cell SCSS |
| 2026-08-02 | S0-01 | Isolated API hosting test | Pass: 1 | Real HTTP pipeline and separate in-memory SQLite databases |
| 2026-08-02 | S0-01 | Backend integration suite | Pass: 3 | Includes API hosting and large DataTransfer roundtrip tests |
| 2026-08-02 | S0-05 | Identity architecture review | Decided | Separate Azure hosts require MSAL bearer-token flow; implementation follows Stage 1 |
| 2026-08-02 | S0-02 | Frontend HTTP test suite | Pass: 3 | `npm run test:ci`; includes first HttpClient request contract test and exits without watch mode |
| 2026-08-02 | S0-02 | Frontend production build | Pass with 2 existing warnings | Bundle and calendar day-cell style budgets unchanged from baseline |
| 2026-08-02 | S0-03 | Backend architecture tests | Pass: 3 | Domain, Application, and Infrastructure outward-dependency rules enforced with NetArchTest |
| 2026-08-02 | S0-03 | Backend integration suite | Pass: 6 | Existing API host and DataTransfer tests remain green with architecture rules |
| 2026-08-02 | S0-04 | API contract tests | Pass: 4 | Route/JSON casing, correlation ID, mixed batch result, and streamed DataTransfer export characterized |
| 2026-08-02 | S0-04 | Backend integration suite | Pass: 10 | API contracts, hosting isolation, architecture, smoke, and DataTransfer roundtrip all green |
| 2026-08-02 | S0-06 | API error contract review | Decided | RFC 9457 shape and status/code mapping recorded; Stage 1 implements it before Azure authentication |
| 2026-08-02 | S0-07 | Date semantics review | Decided | Calendar dates use `yyyy-MM-dd`/`DateOnly`; audit timestamps remain UTC instants |
| 2026-08-02 | S0-08 | Calendar batch semantics review | Decided | Preserve partial success and deterministic per-item status/count contracts |
| 2026-08-02 | S0-09 | DataTransfer limits review | Decided | 10 MiB browser, 12 MiB API, worker parsing, truthful stage-based status |
| 2026-08-02 | Stage 0 gate | Backend unit tests | Pass: 96 | Full unit suite |
| 2026-08-02 | Stage 0 gate | Backend integration tests | Pass: 10 | Hosting, architecture, API contracts, smoke, and DataTransfer roundtrip |
| 2026-08-02 | Stage 0 gate | Frontend tests | Pass: 3 | Deterministic non-watch suite including HTTP service contract |
| 2026-08-02 | Stage 0 gate | Frontend production build | Pass with 2 existing warnings | Bundle and calendar day-cell style budgets unchanged |
| 2026-08-02 | S1-01 | Backend unit tests | Pass: 100 | Typed validation, conflict, not-found, and missing-reference failures covered |
| 2026-08-02 | S1-02 | Exception middleware test | Pass: 1 | Unexpected exception and request context logged; response detail remains generic |
| 2026-08-02 | S1-02 | Backend integration suite | Pass: 11 | Middleware logging change validated through the real API host |
| 2026-08-23 | S1-03/S1-04 | ProblemDetails middleware/API tests | Pass: 7 | Typed exceptions, automatic validation, controller 404, correlation IDs, stable codes/types, and sanitized 500 responses |
| 2026-08-23 | S1-03/S1-04 | Backend integration suite | Pass: 17 | Full integration suite remains green after the shared error contract |
| 2026-08-23 | S1-05 | Frontend tests | Pass: 11 | Typed parser covers JSON, text, malformed, Blob JSON/text, timeout, correlation ID, field-error, and code-first classification behavior |
| 2026-08-23 | S1-05 | Frontend production build | Pass with 2 existing warnings | Bundle and calendar day-cell style budgets unchanged |
| 2026-08-23 | S1-06 | Frontend tests | Pass: 12 | Component extraction migrated to the shared parser; DataTransfer verifies Blob ProblemDetails rendering after async decoding |
| 2026-08-23 | S1-06 | Frontend production build | Pass with 2 existing warnings | Bundle and calendar day-cell style budgets unchanged |
| 2026-08-23 | S1-07 | Frontend tests | Pass: 14 | ExpenseList sends `price=0` for a zero filter and omits price when the field is blank |
| 2026-08-23 | S1-07 | Frontend production build | Pass with 2 existing warnings | Bundle and calendar day-cell style budgets unchanged |
| 2026-08-23 | S1-08 | Focused frontend error rendering | Pass: 2 | DataTransfer renders normal JSON and Blob ProblemDetails messages through the shared parser |
| 2026-08-23 | Stage 1 gate | Backend tests and solution build | Pass: 139 | Complete unit and integration suites; solution test build succeeded |
| 2026-08-23 | Stage 1 gate | Frontend tests | Pass: 15 | Complete Angular suite includes normal JSON, Blob error, parser, and zero-price request coverage |
| 2026-08-23 | Stage 1 gate | Frontend production build | Pass with 2 existing warnings | Bundle and calendar day-cell style budgets unchanged |
| 2026-08-23 | S2-01 | Entra options validation tests | Pass: 8 | Valid GUID/URI audiences accepted; invalid tenant, audience, and user IDs fail API startup with clear key names |
| 2026-08-23 | S2-01 | Backend tests and solution build | Pass: 147 | Complete unit and integration suites use deterministic valid test identity configuration |
| 2026-08-23 | S2-02 | Authentication configuration tests | Pass: 11 | Disabled mode permits blank IDs and registers no scheme; enabled mode uses strict Entra JWT bearer validation |
| 2026-08-23 | S2-02 | Backend tests and solution build | Pass: 150 | Complete suite remains green with authentication disabled by default and conditional middleware ordering |
| 2026-08-23 | S2-03 | Authorization policy tests | Pass: 8 | Both policies allow disabled mode and enforce authenticated matching `oid` claims when enabled |
| 2026-08-23 | S2-03 | Backend tests and solution build | Pass: 158 | Complete suite remains green before controller policy application |
| 2026-08-23 | S2-04 | Controller authorization coverage | Pass: 1 | All eight controllers carry the expected named policy and no controller or action allows anonymous access |
| 2026-08-23 | S2-04 | Backend tests and solution build | Pass: 159 | Complete suite remains green with class-level policies and authentication disabled by default |
| 2026-08-23 | S2-05 | HTTP authorization integration tests | Pass: 6 | Normal and DataTransfer endpoints return 401 anonymously, 403 for missing/different owner IDs, and 200 for the configured owner |
| 2026-08-23 | S2-05 | Backend tests and solution build | Pass: 165 | Complete suite includes 43 integration tests and preserves disabled-mode workflows |
| 2026-08-23 | S2-06 | Frontend authentication tests | Pass: 7 | Runtime config validation, enabled/disabled interception, API scope acquisition, and same-origin API URL matching covered |
| 2026-08-23 | S2-06 | Frontend tests | Pass: 22 | Complete Angular suite remains green with authentication disabled by default |
| 2026-08-23 | S2-06 | Frontend production build | Pass with 2 warnings | Initial bundle is 799.97 kB after MSAL; existing initial-bundle and calendar day-cell style budgets remain warnings |
| 2026-08-23 | S2-07/S2-08 | Frontend authentication experience tests | Pass: 22 | Guards, owner navigation, session initialization, login/logout, deep links, loading, 401 recovery, expired tokens, and non-looping 403 behavior covered |
| 2026-08-23 | S2-09/S2-10 local | Development authentication and HTTP role matrix | Pass: 7 | Development-only registration gate plus anonymous 401, non-owner 403, owner 200, and DataTransfer owner 200 covered |
| 2026-08-23 | S2-07/S2-08 | Frontend tests | Pass: 35 | Complete Angular suite remains green with authentication disabled by default |
| 2026-08-23 | S2-07/S2-08 | Frontend production build | Pass with 2 warnings | Initial bundle is 804.35 kB; initial-bundle and calendar day-cell style budgets remain warnings |
| 2026-08-23 | S2-09/S2-10 local | Backend tests and solution build | Pass: 172 | Complete suite includes 50 integration tests and proves Production cannot select the Development scheme |
| 2026-08-23 | S3-01/S3-06 | Restore conflict and non-midnight date tests | Pass: 4 | Restoring leave or work-location entries onto an occupied date is rejected, including same-date different-time values |
| 2026-08-23 | S3-02/S3-03 | Frontend calendar-date utility tests | Pass: 13 | Invalid dates, leap days, month/day bounds, ISO truncation, and timezone-stable round-trips covered |
| 2026-08-23 | S3-01/S3-02/S3-03/S3-06 | Backend and frontend suites | Pass: 176 backend, 48 frontend | Stage 3 code-only slice green; `DateOnly` conversion and calendar-date JSON deferred to the migration batch |
| 2026-08-23 | S3-04/S3-05 | Full backend and frontend suites | Pass: 104 unit, 50 integration, 48 frontend | Calendar dates are `DateOnly` end to end and serialize as `yyyy-MM-dd`; wire format preserved for the frontend |
| 2026-08-23 | S3-05 | EF migration `ConvertCalendarDatesToDateOnly` | Generated and script builds | SQLite stores dates as TEXT so no column alter is emitted; existing non-midnight normalization and preflight remain S3-08 |
| 2026-08-23 | S3-07/S3-08 | Active-date uniqueness integration tests | Pass: 3 | Duplicate active work-location and leave dates are rejected by the database; soft-deleted rows do not block a new active entry |
| 2026-08-23 | S3-07/S3-08 | Backend suite and migration script | Pass: 104 unit, 53 integration | `AddActiveDateUniqueIndexes` normalizes legacy values, soft-deletes duplicate active entries deterministically, and creates filtered unique indexes |
| 2026-08-23 | S3-09 | Unique-constraint race translation test | Pass: 1 | A duplicate active-date insert via the repository surfaces as `ApplicationConflictException` and the 409 conflict response |
| 2026-08-23 | Stage 3 gate | Backend, frontend, and production build | Pass: 104 unit, 54 integration, 48 frontend, build ok | Date-only contracts, restore conflicts, active-date uniqueness, race conflict mapping, and timezone-safe calendar dates validated |
| 2026-08-23 | S4-01/S4-02 | Expense reference rejection tests | Pass: 3 | Missing tag references throw `invalid_reference`, duplicate tag IDs are de-duplicated, and stale tags on update are rejected |
| 2026-08-23 | S4-01/S4-02 | Backend suite | Pass: 107 unit, 54 integration | Expense create/update reject missing source, bank, and tag references; DataTransfer already rejected missing references |
| 2026-08-23 | S4-06/S4-07/S4-08 | Aggregate tag-link behavior tests | Pass: 5 | `ReplaceTagLinks` preserves link identities and dedupes, `AddTagLink` is idempotent with preserved IDs, `Import` preserves the supplied ID |
| 2026-08-23 | S4-06/S4-07/S4-08 | Backend, frontend, and production build | Pass: 112 unit, 54 integration, 48 frontend, build ok | `TaxExpense` owns a private tag-link collection via behavior methods; EF field mapping and DataTransfer roundtrip preserve IDs and relationships |
| 2026-08-23 | S4-03/S4-04/S4-05 | Shared tag-resolution tests | Pass: 7 | Parsing dedupe, existing-tag reuse, missing-tag creation, partial-failure retry, and stale-tag pruning covered |
| 2026-08-23 | Stage 4 gate | Backend, frontend, and production build | Pass: 112 unit, 54 integration, 55 frontend, build ok | Both expense entry points share tag resolution; stale tags recover without losing form state and partial tag-creation failures remain for retry |
| 2026-08-23 | S5-01/S5-02/S5-03/S5-04 | Batch service and API contract tests | Pass: 48 | Leave and work-location batches preserve mixed statuses while using one occupied-date lookup, zero per-item existence queries, and at most one save; bounded behavior verified with 100-item batches |
| 2026-08-23 | S5-05/S5-06 | Frontend tests and production build | Pass: 6 focused, 61 frontend, build ok | Pure planning covers all create/update/delete groups; reconciliation preserves mixed results and retries only the unfinished half of partial category transitions; existing bundle warnings unchanged |
| 2026-08-23 | Stage 5 gate | Solution tests, frontend tests, and production build | Pass: 187 discovered .NET tests, solution test/build succeeded, 67 frontend, build ok | Shared only identical hours and paging policies; mixed create/update/delete, skips, conflicts, request failures, deterministic ordering, and retry-only unfinished operations covered; existing bundle warnings unchanged |
| 2026-08-23 | S6-01 / Slice A | Domain, service, import, and EF roundtrip tests | Pass: 190 discovered .NET tests | Leave and work-location state has private setters; explicit import factories preserve identity; SQLite rematerialization and DataTransfer roundtrip preserve encapsulated state. S6-04 remains open for shared base/reference clock initialization in Slice C. |
| 2026-08-23 | S6-02 / Slice B | Domain and full backend tests | Pass: 12 focused, 192 discovered .NET tests | Shared day-entry policy owns full-day, half-day, specific-hours, and invalid-type rules; aggregate APIs remain explicit and failed validation no longer partially mutates time-entry state. |
| 2026-08-23 | S6-03/S6-04/S6-05 / Slice C | Domain, DataTransfer, migration, SQLite roundtrip, and full backend tests | Pass: migration script generated, 197 discovered .NET tests | Reference and soft-delete state is encapsulated; all reference mutations use `TimeProvider`; optional source audit timestamps survive export/import/rematerialization; legacy payloads remain compatible; migration backfills `UpdatedAt` from `CreatedAt`. |
| 2026-08-23 | Stage 6 gate / S6-06 | Migration upgrade, API contract, backend/frontend suites, and production build | Pass: upgrade test, 198 discovered .NET tests, 67 frontend tests, build ok | Real SQLite upgrade backfills non-seed reference rows; streamed exports expose camel-case audit fields; DataTransfer remains opaque to frontend parsing; existing bundle warnings unchanged. |
| 2026-08-23 | S7-01/S7-02 / Slice A | Per-entity export query-boundary and single-transaction rollback tests | Pass: 203 discovered .NET tests | An EF command interceptor confirms each per-entity reference export queries only its owning table; a valid tracker sharing a payload with an invalid tag rolls back entirely, while a subsequent valid import commits. Public holidays remain hard-delete. |
| 2026-08-23 | S7-03/S7-04 / Slice B | API oversized-request and frontend file-rejection tests | Pass: 205 discovered .NET tests, 70 frontend tests | A Content-Length guard middleware returns a readable 413 above 12 MiB (the `[RequestSizeLimit]` attribute is not enforced under TestServer, so the middleware is the tested mechanism); the frontend rejects >10 MiB and non-JSON files before reading. API and browser limits share the `DEC-HARD-005` value. |
| 2026-08-23 | S7-05/S7-06/S7-07 / Slice C and Stage 7 gate | Parse benchmark, malformed/oversized/unauthorized/dry-run/rollback/large-roundtrip tests, frontend suite, and production build | Pass: 7.35 ms median / 8.29 ms p95 parse, 210 discovered .NET tests, 72 frontend tests, build ok | Worker parsing was not justified at the 10 MiB ceiling. Active HTTP operations expose real unsubscribe-based cancellation and truthful stages; malformed input returns correlated ProblemDetails; dry runs do not persist. Existing bundle warnings unchanged. |
| 2026-08-23 | S8-01/S8-02/S8-03 / Slice A | Persistence/route/repository tests, DataTransfer service/accessibility tests, CI-equivalent suites, migration script, and production build | Pass: 4 focused backend tests, 9 focused frontend tests, 214 discovered .NET tests, 74 frontend tests, migration script generated, build ok | EF metadata now guards filters, FKs, and unique indexes; DI and MVC discovery guard repositories and attributed routes; DataTransfer HTTP contracts and announced cancel state are covered. CI now enforces all backend, migration, frontend test, and production build gates. Existing bundle warnings unchanged. |

## Progress Update Procedure

At the end of each implementation session:

1. Change completed task markers to `[x]` only after validation.
2. Mark the current task `[~]`; mark blocked tasks `[!]` and add a blocker entry.
3. Recalculate each stage `Done`, percentage, and the overall `X/74` total.
4. Update `Current Stage`, `Next Task`, and `Last Updated` at the top.
5. Add validation results and relevant commit hashes to the validation log notes.
6. Record contract/product decisions before implementing dependent tasks.
7. Keep detailed backend/frontend plan statuses synchronized at phase boundaries.

## Definition of Done

- All 74 retained tasks are complete or explicitly removed by recorded decisions.
- Every stage validation gate and release checkpoint passes.
- API security and domain invariants are enforced server-side.
- Frontend session, errors, dates, and workflows match stable API contracts.
- Domain mutation is encapsulated without breaking EF or DataTransfer.
- Calendar and DataTransfer behavior is tested under failure and large-payload conditions.
- CI enforces architecture, migrations, backend tests, frontend tests, and production builds.
- Documentation, decision log, blocker log, dashboard, and validation log reflect the delivered system.
