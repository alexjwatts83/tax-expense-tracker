# Frontend Clean Code and Architecture Hardening Plan

Last Updated: 2026-08-02
Status: Planned

Cross-layer implementation order and progress are tracked in [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md). This document remains the detailed frontend reference.

## Goal

Improve frontend correctness, testability, and maintainability while preserving the current Angular Material user experience and API workflows.

This plan was derived from an independent frontend review. It does not assume a particular backend refactor, but it identifies contracts that both layers must agree on.

## Current Architecture Assessment

Strengths:

- Standalone lazy-loaded route components keep initial routing simple.
- API access is generally isolated in feature services.
- Reactive forms encode core required/range validation.
- Expense filtering guards against stale response application.
- Calendar batch entry displays mixed per-item outcomes rather than hiding partial failures.
- DataTransfer requires a successful dry-run before mutation.
- Shared date display/input components avoid direct UTC parsing in most templates.

Primary risks:

1. The application has no authentication/session model or protected routes.
2. HTTP error parsing is duplicated and untyped across components.
3. A zero-price expense filter is dropped because `0` is treated as absent.
4. DataTransfer export errors are requested as `Blob`, so JSON problem responses are not decoded for display.
5. DataTransfer reads and parses the entire selected file on the UI thread without a size guard.
6. Expense tag-name resolution and creation logic is duplicated across two components.
7. Work-location and leave management components duplicate most form, paging, summary, and restore behavior.
8. Calendar batch entry owns change-set construction, six request groups, reconciliation, and row feedback in one large component.
9. Date-only and timestamp values are both represented as unqualified `string` values.
10. Frontend automated coverage consists only of two application smoke tests.

## Guiding Decisions

1. Fix user-visible defects before structural refactoring.
2. Keep components responsible for interaction and presentation, not multi-entity workflow policy.
3. Keep HTTP services small; put reusable workflow orchestration in focused facades.
4. Do not introduce NgRx or another state library without demonstrated state complexity.
5. Use runtime checks only at untrusted boundaries; do not duplicate validation everywhere.
6. Treat route guards as UX controls, never as a replacement for server authorization.
7. Preserve existing partial-success feedback unless product semantics explicitly change.
8. Add focused tests around extracted logic before moving large components.

## Independent Findings

### High Priority

- No authentication/session state, route protection, or role-aware navigation exists.
- Calendar batch orchestration is too large and coupled to component state to test safely.
- Critical expense, calendar, DataTransfer, and management workflows have no focused tests.
- Large DataTransfer imports can block or exhaust the browser tab.

### Medium Priority

- Error extraction is repeated across at least ten components and can assign non-string objects to UI messages.
- Blob-based export errors cannot currently surface JSON problem details correctly.
- Expense form and inline creation duplicate tag parsing/resolution/creation behavior.
- Work-location and leave management duplicate nearly complete interaction flows.
- Date contracts do not distinguish calendar dates from instants.
- Reference data is loaded independently by multiple components with no coordinated invalidation.

### Confirmed Defect

The expense filter currently uses a truthiness check for price. A valid price cap of `0` becomes `undefined`, so the request is sent without the price filter.

## Cross-Layer Decision Gates

### FL-001: Identity and Session UX

Decide jointly with API authentication:

- Identity provider and login/logout mechanism.
- How the Angular app obtains and refreshes credentials.
- Which claims identify a DataTransfer administrator.
- Local-development authentication behavior.
- 401 session-expired behavior and 403 access-denied behavior.

Do not store a reusable API secret in browser code, local storage, or committed environment files.

### FL-002: Error Contract

Adopt one stable problem response shape for non-successful API requests:

- HTTP status
- stable machine-readable code or type
- user-safe title/detail
- correlation ID
- optional validation errors keyed by field

The frontend should classify status/code centrally and let components choose contextual fallback copy.

### FL-003: Date Contract

Use two explicit concepts:

- Calendar date: ISO `yyyy-MM-dd`, never parsed through UTC.
- Instant/audit timestamp: ISO UTC timestamp with offset or `Z`.

Work location, leave, public holidays, filters, and summary anchors are calendar dates. Expense date semantics must be confirmed separately because it may represent either a purchase date or timestamp.

### FL-004: Calendar Change-Set Semantics

Current behavior sends create batches plus individual update/delete requests in parallel and reports partial success.

Decide whether this remains the product contract:

- Option A: preserve partial success and make the orchestration explicit/testable.
- Option B: add one backend calendar change-set endpoint with a transaction and deterministic per-row results.

Do not make frontend orchestration appear atomic if the API contract is not atomic.

### FL-005: DataTransfer File Limits

Coordinate:

- Maximum accepted request size.
- Maximum browser-parse size.
- Whether large validation runs synchronously, in a Web Worker, or as an uploaded background job.
- Progress and cancellation behavior.

The browser limit may need to be lower than the API transport limit.

## Phase 0: Frontend Test Foundation

Status: Not Started

### Tasks

- [ ] Add service tests using Angular HTTP testing providers.
- [ ] Add tests for query-parameter construction, especially zero-valued filters.
- [ ] Add tests for shared error extraction from JSON, text, and Blob responses.
- [ ] Add tests for date-only parsing, formatting, leap days, and invalid dates.
- [ ] Add focused component tests for expense submit, restore conflict, and DataTransfer dry-run gating.
- [ ] Add pure-unit tests for calendar change-set construction and result reconciliation before extraction.
- [ ] Define a practical frontend CI test command and coverage reporting.

### Acceptance Criteria

- Critical utilities and service contracts have deterministic tests.
- Every later refactor can begin from a green frontend suite.
- Tests avoid brittle assertions on Angular Material internal markup.

### Suggested Commit

`test frontend workflow baseline`

## Phase 1: Correctness and Error Handling

Status: Not Started
Depends On: FL-002

### Tasks

- [ ] Fix zero-price filter handling using an explicit null/empty check.
- [ ] Add typed `ApiProblem` and validation-error models.
- [ ] Add one error extraction/classification utility that accepts `unknown`.
- [ ] Decode Blob problem responses from export requests before displaying errors.
- [ ] Replace duplicated `err?.error?.detail` expressions with the shared utility.
- [ ] Distinguish validation, conflict, authentication, authorization, timeout, and server failures.
- [ ] Preserve contextual component fallback messages.
- [ ] Include correlation IDs in support-facing error details where useful.

### Acceptance Criteria

- UI error message properties always receive strings.
- A JSON problem response is readable for normal and Blob HTTP requests.
- A zero-price expense filter reaches the API.
- 401, 403, 409, validation, timeout, and 500 responses produce distinct behavior.

### Suggested Commits

1. `fix expense zero price filter`
2. `centralize frontend api errors`

## Phase 2: Date Contract Hardening

Status: Not Started
Depends On: FL-003

### Tasks

- [ ] Introduce `CalendarDate` and `UtcTimestamp` TypeScript aliases or branded types.
- [ ] Mark model properties according to their actual semantics.
- [ ] Centralize strict calendar-date parsing and formatting.
- [ ] Reject impossible dates instead of allowing JavaScript rollover.
- [ ] Remove scattered `.slice(0, 10)` conversions where the contract is date-only.
- [ ] Keep local calendar dates out of `new Date(isoString)` UTC parsing paths.
- [ ] Add contract fixtures for date-only and timestamp API responses.
- [ ] Verify DST boundaries even though the current locale is Australia/Sydney.

### Acceptance Criteria

- Calendar dates display as the same day in every supported timezone.
- Invalid dates return a clear validation state rather than rolling into another month.
- API request models send `yyyy-MM-dd` for date-only fields.
- Audit timestamps retain instant semantics.

### Suggested Commit

`separate calendar dates from timestamps`

## Phase 3: Expense Tag Workflow Extraction

Status: Not Started

### Tasks

- [ ] Extract manual-tag tokenization and case-insensitive deduplication into a pure utility.
- [ ] Add a focused tag-resolution facade for existing-name lookup and missing-tag creation.
- [ ] Use the same facade from expense form and inline expense creation.
- [ ] Define behavior when some tag creations succeed and a later request fails.
- [ ] Reload or reconcile tags after partial failure so retry cannot create accidental duplicates.
- [ ] Surface stale/missing selected tag IDs clearly when expense save is rejected.
- [ ] Decide whether Apply Tags remains an explicit pre-save operation.

### Acceptance Criteria

- Both expense entry points resolve tags identically.
- Partial tag-creation failures leave the local list synchronized with the server.
- Expense save errors retain the user-entered form state.
- The component no longer owns cross-entity tag creation policy.

### Suggested Commit

`extract expense tag resolution workflow`

## Phase 4: Day-Entry Component Boundaries

Status: Not Started
Depends On: FL-004

### Tasks

- [ ] Extract shared entry-type/specific-hours form policy used by work location and leave.
- [ ] Extract shared paging and summary-loading behavior only where it remains readable.
- [ ] Keep feature-specific labels, payload mapping, and domain terminology explicit.
- [ ] Move calendar change-set construction into a pure planner.
- [ ] Move result reconciliation into a tested pure reducer or facade.
- [ ] Keep the calendar component focused on user interaction and rendering.
- [ ] If atomic change sets are selected, add a dedicated API service method and simplify six-group orchestration.
- [ ] If partial success remains, document and test all mixed-operation outcomes.

### Acceptance Criteria

- Calendar planning and result reconciliation can be tested without rendering the component.
- Work-location and leave screens share proven mechanics without a deeply generic base component.
- Partial or atomic semantics match the API contract exactly.
- Existing per-row feedback remains available.

### Suggested Commits

1. `extract day entry form policies`
2. `extract calendar batch orchestration`

## Phase 5: DataTransfer Browser Hardening

Status: Not Started
Depends On: FL-001, FL-005

### Tasks

- [ ] Restrict the route and navigation item to authorized administrators for UX purposes.
- [ ] Handle 401 and 403 independently from validation failures.
- [ ] Validate file extension, MIME type where available, and configured size before reading.
- [ ] Avoid parsing oversized JSON synchronously on the UI thread.
- [ ] Evaluate a Web Worker for accepted medium-sized files.
- [ ] Use upload progress only if the API/hosting path can report it accurately.
- [ ] Preserve dry-run-first validation and settings fingerprinting.
- [ ] Display correlation IDs for failed imports and exports.
- [ ] Revoke object URLs after the download click has been safely dispatched.

### Acceptance Criteria

- Unauthorized users cannot navigate to or see DataTransfer controls, while the API remains the security authority.
- Oversized files are rejected before `file.text()` and `JSON.parse()`.
- Parsing does not freeze the main thread for the supported file size.
- Blob error responses display readable problem details.
- Dry-run validation cannot be reused after payload or option changes.

### Suggested Commits

1. `harden data transfer file handling`
2. `protect data transfer admin ui`

## Phase 6: Authentication UX

Status: Not Started
Depends On: FL-001

### Tasks

- [ ] Add an authentication/session service around the selected identity SDK.
- [ ] Add credential attachment or rely on secure same-site hosting as designed.
- [ ] Add route guards for authenticated and administrator routes.
- [ ] Make navigation role-aware.
- [ ] Add login, logout, loading, expired-session, and access-denied states.
- [ ] Prevent redirect loops when token acquisition fails.
- [ ] Test deep links and return URLs.
- [ ] Keep credentials out of logs and persisted application state.

### Acceptance Criteria

- Reloading a protected deep link restores or requests a valid session.
- 401 initiates the agreed session recovery flow.
- 403 shows access denied without repeated retries.
- DataTransfer visibility follows administrator claims.
- Logout clears local session state and protected cached data.

### Suggested Commit

`add frontend authentication experience`

## Phase 7: Shared Reference Data and State

Status: Not Started

### Tasks

- [ ] Measure duplicate tracker/tag/bank requests before adding caching.
- [ ] If justified, add a lightweight reference-data facade using signals or RxJS state.
- [ ] Define explicit invalidation after create, update, delete, and restore.
- [ ] Do not add a global state library solely for reference lookups.
- [ ] Keep server data reloadable after authorization/session changes.

### Acceptance Criteria

- Reference data is not stale after management operations.
- Any cache has explicit invalidation and session boundaries.
- Components do not maintain conflicting long-lived copies without reconciliation.

### Suggested Commit

`coordinate frontend reference data`

## Phase 8: Frontend Quality Gates

Status: Not Started

### Tasks

- [ ] Add service contract coverage for every API service.
- [ ] Add component coverage for expense, time tracking, calendar batch, and DataTransfer critical paths.
- [ ] Add route-guard and authentication tests.
- [ ] Add accessibility checks for forms, menus, result tables, and error states.
- [ ] Keep production build budget warnings visible and intentional.
- [ ] Add lint/format checks only with an agreed repository configuration.
- [ ] Add a small browser smoke suite only if manual validation becomes a delivery bottleneck.

### Acceptance Criteria

- CI runs frontend tests and production build.
- Critical workflows have success, validation, conflict, and authorization coverage.
- Complex workflow logic is tested outside component rendering where possible.
- No new unreviewed production budget warning is introduced.

## Recommended Delivery Order

1. Phase 0: test foundation
2. Phase 1: correctness and error handling
3. Phase 2: date contract hardening
4. Phase 3: expense tag workflow
5. Phase 4: day-entry component boundaries
6. Phase 5: DataTransfer browser hardening
7. Phase 6: authentication UX
8. Phase 7: measured reference-data state
9. Phase 8: quality gates

Authentication backend enforcement and frontend UX should ship as one deployable capability even if implemented in separate commits.

## Validation Matrix

```powershell
Set-Location Frontend
npm test -- --watch=false
npm run build
```

Phase-specific validation:

- Error handling: JSON/text/Blob problem fixtures and 401/403/409 behavior.
- Dates: timezone matrix and leap-day fixtures.
- Calendar: mixed create/update/delete result matrix.
- DataTransfer: malformed, oversized, unauthorized, validation-error, and successful files.
- Authentication: anonymous, authenticated, administrator, expired-session, and deep-link scenarios.

## Definition of Done

- User-visible filter and error-handling defects are fixed.
- Calendar dates and timestamps have distinct contracts.
- Components do not duplicate expense tag workflow policy.
- Calendar batch orchestration is testable outside the component.
- DataTransfer is safe for the supported browser file size and restricted to administrator UX.
- Authentication/session behavior is complete and aligned with API enforcement.
- Shared state is introduced only where measured value justifies it.
- Critical frontend workflows are protected by automated tests and production builds.
