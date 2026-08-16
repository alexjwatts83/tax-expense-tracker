# Full-Stack Hardening Implementation Plan

## Plan Status Snapshot

- **Status:** In Progress - 15% Complete
- **Assessed:** 2026-08-16T13:11:29+10:00
- **Evidence:** 11 of 74 tasks are complete. Stage 0 is complete, Stage 1 is 2 of 8, and Stages 2 through 8 have not started.
- **Remaining gate:** Finish Stage 1 error contracts and immediate defects before beginning authentication and dependent hardening stages.
- **Authority:** This snapshot supersedes older progress text below until the next dated assessment.

Last Updated: 2026-08-02
Status: In Progress
Overall Progress: 11/74 tasks (15%)
Current Stage: Stage 1 - Error Contract and Immediate Defects
Next Task: S1-03

## Purpose

Deliver the backend and frontend hardening work in the safest implementation order. Work is organized as vertical increments: agree the shared contract, implement backend authority, implement frontend behavior, then validate the complete workflow before moving on.

Detailed technical guidance remains in:

- [BACKEND_DDD_HARDENING_PLAN.md](BACKEND_DDD_HARDENING_PLAN.md)
- [FRONTEND_CLEAN_CODE_HARDENING_PLAN.md](FRONTEND_CLEAN_CODE_HARDENING_PLAN.md)

This document is the execution and progress tracker. When sequencing or status differs, this document controls delivery order; the detailed plans control layer-specific implementation details.

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
| 1 | Error contract and immediate defects | 2 | 8 | 25% | In Progress | Stage 0 |
| 2 | Authentication and authorization | 0 | 10 | 0% | Not Started | Stages 0-1 |
| 3 | Date integrity and restore safety | 0 | 10 | 0% | Not Started | Stages 0-1 |
| 4 | Expense references and aggregate ownership | 0 | 9 | 0% | Not Started | Stages 1, 3 |
| 5 | Calendar and day-entry workflow | 0 | 8 | 0% | Not Started | Stages 1, 3 |
| 6 | Remaining domain encapsulation | 0 | 6 | 0% | Not Started | Stages 3-5 |
| 7 | DataTransfer hardening | 0 | 7 | 0% | Not Started | Stages 1-4 |
| 8 | Quality gates and operational readiness | 0 | 7 | 0% | Not Started | Stages 2-7 |
| **Total** |  | **11** | **74** | **15%** | **In Progress** |  |

## Delivery Rules

1. Keep every commit buildable and every completed stage deployable.
2. Backend is the security and invariant authority; frontend guards and validation improve UX only.
3. Preserve wire compatibility while internals change unless a contract decision explicitly approves a breaking change.
4. Add a regression test with each defect fix; do not commit intentionally failing tests alone.
5. Update this dashboard, task checkboxes, validation log, and `Last Updated` after each work session.
6. Do not start a dependent stage while its decision or validation gate is incomplete.
7. Do not combine authentication rollout, date migration, and aggregate encapsulation in one release.
8. Keep generated migrations and contract changes in separate reviewable commits when practical.

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
- [ ] **S1-03** Map backend errors to the agreed ProblemDetails contract and status codes.
- [ ] **S1-04** Add middleware/API integration tests for 400, 404, 409, and 500 responses.
- [ ] **S1-05** Add typed frontend `ApiProblem` parsing/classification for JSON, text, and Blob responses.
- [ ] **S1-06** Replace duplicated component error extraction with the shared frontend utility.
- [ ] **S1-07** Fix the frontend zero-price expense filter and add a request-parameter regression test.
- [ ] **S1-08** Validate normal and Blob errors end to end and pass the stage validation gate.

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

- [ ] **S2-01** Add validated single-tenant Entra options, including tenant, API audience, and allowed user object ID.
- [ ] **S2-02** Register JWT bearer validation and place `UseAuthentication()` before authorization.
- [ ] **S2-03** Add an allowed-user policy and a named DataTransfer policy; both authorize the sole configured identity without introducing application roles.
- [ ] **S2-04** Apply policies to every controller and document intentionally anonymous endpoints.
- [ ] **S2-05** Add backend 401/403/authenticated/admin integration tests.
- [ ] **S2-06** Add MSAL Angular using authorization code with PKCE, in-memory token caching, and an API-scope interceptor.
- [ ] **S2-07** Add authenticated and administrator route guards plus role-aware navigation.
- [ ] **S2-08** Add login, logout, loading, expired-session, access-denied, and deep-link behavior.
- [ ] **S2-09** Provision separate SPA/API Entra registrations, expose and consent the API scope, assign only the owner, configure API Easy Auth, CORS, redirect URIs, and a Development-only test scheme.
- [ ] **S2-10** Run the anonymous/user/admin end-to-end matrix and pass the stage validation gate.

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

- [ ] **S3-01** Add backend regression tests for restore conflicts and non-midnight date behavior.
- [ ] **S3-02** Add frontend date utility/contract tests including invalid dates, leap days, and timezones.
- [ ] **S3-03** Introduce frontend calendar-date and UTC-timestamp types and strict utilities.
- [ ] **S3-04** Preserve `yyyy-MM-dd` calendar-date JSON across forms, filters, summaries, and DataTransfer.
- [ ] **S3-05** Convert backend date-only domain/application contracts to `DateOnly` where agreed.
- [ ] **S3-06** Add restore conflict checks for work-location and leave entries.
- [ ] **S3-07** Add filtered unique indexes for active work-location and leave dates.
- [ ] **S3-08** Create migration normalization, duplicate preflight, repair, and rollback steps.
- [ ] **S3-09** Translate database uniqueness races to the agreed conflict response and frontend UX.
- [ ] **S3-10** Validate upgrade, roundtrip, timezone, restore, batch, and import behavior and pass the stage gate.

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

- [ ] **S4-01** Add backend tests for missing, duplicate, and stale expense tag IDs on create/update.
- [ ] **S4-02** Reject missing source, bank, and tag references consistently with stable codes.
- [ ] **S4-03** Add frontend stale-tag recovery while preserving unsaved form state.
- [ ] **S4-04** Extract shared frontend manual-tag parsing and resolution workflow.
- [ ] **S4-05** Define and test partial tag-creation failure/retry behavior.
- [ ] **S4-06** Add `TaxExpense` tag-link behavior and private backing collection.
- [ ] **S4-07** Replace direct tag collection assignment/mutation in Application and DataTransfer.
- [ ] **S4-08** Restrict `TaxExpense` mutation and add explicit import/rehydration behavior preserving IDs.
- [ ] **S4-09** Run expense UI, API, EF, and DataTransfer roundtrip tests and pass the stage gate.

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

- [ ] **S5-01** Implement the recorded partial-success or atomic change-set contract.
- [ ] **S5-02** Bulk-load occupied work/leave dates and remove per-item existence queries.
- [ ] **S5-03** Reduce save calls while preserving the selected transaction semantics.
- [ ] **S5-04** Add backend query-count and mixed-result regression tests.
- [ ] **S5-05** Extract frontend calendar change-set planning into a pure tested module.
- [ ] **S5-06** Extract result reconciliation into a pure reducer/facade with mixed-outcome tests.
- [ ] **S5-07** Share work/leave form and paging policies only where domain language stays clear.
- [ ] **S5-08** Validate create/update/delete mixtures, retries, conflicts, and partial failures and pass the stage gate.

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

- [ ] **S6-01** Replace public time-entry mutation with private/protected setters and domain methods.
- [ ] **S6-02** Centralize shared day-entry hours rules without hiding feature terminology.
- [ ] **S6-03** Encapsulate Tracker, Tag, Bank, PublicHoliday, and soft-delete transitions.
- [ ] **S6-04** Remove hardcoded `DateTime.UtcNow` initializers and use explicit clock-driven creation.
- [ ] **S6-05** Decide and implement consistent audit timestamps for mutable reference entities.
- [ ] **S6-06** Validate EF materialization, imports, migrations, APIs, and frontend contracts and pass the stage gate.

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

- [ ] **S7-01** Avoid reference-data queries for unrelated per-entity exports.
- [ ] **S7-02** Validate import modes and maintain atomic rollback for all writes.
- [ ] **S7-03** Define and enforce matching API/browser file limits.
- [ ] **S7-04** Reject unsupported files before frontend `file.text()`/`JSON.parse()`.
- [ ] **S7-05** Move supported large parsing off the UI thread if measurements require it.
- [ ] **S7-06** Add readable Blob errors, correlation IDs, authorization states, cancellation, and truthful progress behavior.
- [ ] **S7-07** Run malformed, oversized, unauthorized, dry-run, rollback, and large-roundtrip tests and pass the stage gate.

### Acceptance Criteria

- Per-entity exports query only required data.
- Unsupported or oversized files do not freeze the browser.
- DataTransfer remains dry-run-first and administrator-only.
- Import atomicity and ID/relationship restoration remain intact.

### Suggested Commits

1. `tighten data transfer query boundaries`
2. `harden data transfer file handling`

## Stage 8: Quality Gates and Operational Readiness

Goal: make the hardened architecture and workflows enforceable in CI and diagnosable in deployment.

### Tasks

- [ ] **S8-01** Complete backend architecture, API route, repository, query-filter, FK, and uniqueness coverage.
- [ ] **S8-02** Complete frontend service, critical component, guard, accessibility, and error-state coverage.
- [ ] **S8-03** Add CI checks for backend build/tests, migration script, frontend tests, and production build.
- [ ] **S8-04** Measure reference-data requests and add lightweight frontend caching only if justified.
- [ ] **S8-05** Profile expense paging/filtering and DataTransfer chunking before further optimization.
- [ ] **S8-06** Finalize Production startup validation, migration execution policy, monitoring, and correlated logs.
- [ ] **S8-07** Run local and Azure smoke matrices, update all plans/docs, and pass final sign-off.

### Acceptance Criteria

- Forbidden dependencies and persistence invariant regressions fail CI.
- Critical frontend workflows have success and failure coverage.
- Production configuration failures are immediate and actionable.
- No optimization is added without a measurement and invalidation strategy.
- Deployment smoke tests cover anonymous, user, and administrator roles.

### Suggested Commits

1. `enforce full stack quality gates`
2. `finalize hardening operations`

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
| R3 | 3 | Date-safe work/leave workflows and database uniqueness | Not Started |
| R4 | 4-5 | Strict expense references and maintainable calendar workflow | Not Started |
| R5 | 6-7 | Encapsulated domain and hardened DataTransfer | Not Started |
| R6 | 8 | CI-enforced, operationally ready hardening release | Not Started |

## Decision Log

| ID | Date | Decision | Choice | Rationale | Impacted Stages | Status |
|---|---|---|---|---|---|---|
| DEC-HARD-001 | 2026-08-02 | Identity/session architecture | Single-tenant Entra; separate SPA/API registrations; MSAL code + PKCE; JWT validation in Easy Auth and ASP.NET Core; one allowed object ID; sole user is DataTransfer admin | Planned Azure topology uses separate Static Web App and API App Service origins. This avoids app-owned passwords while restricting all data to the owner and keeping server authorization authoritative. | 2, 7, 8 | Decided |
| DEC-HARD-002 | 2026-08-02 | API problem/error contract | RFC 9457 ProblemDetails with `code`, stable `type`, `title`, `status`, safe `detail`, request `instance`, `correlationId`, and optional `errors` | Current exceptions, bare 404s, DataTransfer errors, and Angular extraction are inconsistent. One contract supports typed frontend behavior and correlated diagnostics without exposing internals. | 1-5, 7 | Decided |
| DEC-HARD-003 | 2026-08-02 | Calendar and expense date semantics | Expense purchase, work, leave, and holiday dates are timezone-free calendar dates serialized as `yyyy-MM-dd` and represented by `DateOnly` in backend contracts; audit values are UTC instants | Users enter and report these values by calendar day. Converting them through UTC can shift the day. Existing non-midnight values retain their stored year/month/day during normalization. | 3-7 | Decided |
| DEC-HARD-004 | 2026-08-02 | Calendar batch transaction semantics | Preserve partial success with deterministic per-item results and aggregate counts; malformed requests fail wholly, but one item failure does not roll back valid items | The API and Angular workflow already expose mixed outcomes, and users can correct only failed rows. Preserving this contract avoids a breaking endpoint and keeps feedback granular. | 5 | Decided |
| DEC-HARD-005 | 2026-08-02 | DataTransfer browser/API limits | 10 MiB maximum selected file; 12 MiB API request limit; reject before reading; worker-based parse/validation; stage-based status and cancellation only where real | The current 100 MiB API allowance and main-thread `file.text()`/`JSON.parse()` combination can exhaust or freeze the browser. A small solo-use ceiling is safer and can be raised later from measurements. | 7 | Decided |
| DEC-HARD-006 |  | Reference entity audit timestamps |  |  | 6 | Open |

## Blocker Log

| Date | Task | Blocker | Owner | Resolution/Next Step | Status |
|---|---|---|---|---|---|
|  |  |  |  |  |  |

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
