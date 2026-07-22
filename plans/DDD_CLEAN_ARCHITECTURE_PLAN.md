# Tax Expense Tracker - DDD and Clean Architecture Refactor Plan

## Goal

Refactor the current solution into a layered DDD and Clean Architecture structure while preserving existing behavior and API contracts.

## Current State Summary

- Domain, Application, and Infrastructure projects have been created and added to the solution.
- Core domain entities have been extracted to the Domain project.
- Tracker API orchestration has been moved to an Application service with repository abstraction.
- Tag and Expense controllers still contain direct orchestration logic.
- EF Core DbContext and persistence concerns are still in the API project.
- Application use cases have started with the Tracker slice.

## Progress Snapshot (2026-07-22)

Completed:

1. Solution now includes Domain/Application/Infrastructure projects.
2. Project references were added to establish initial dependency flow.
3. Core entities moved from API models into Domain entities:
   - Tracker
   - Tag
   - TaxExpense
   - TaxExpenseTag
4. API was rewired to compile against Domain entities.
5. Solution build passes after the layering bootstrap.
6. Domain-level invariant methods were added to core entities.
7. API create/update/delete flows now call domain behavior methods.

In progress:

1. Phase B (domain extraction) is substantially complete; base abstractions still pending.
2. Phase C (application use cases) has progressed through tracker, tag, and expense slices.
3. Phase D (infrastructure ownership) has started with DbContext and repository relocation.
4. Phase F (testing) has started with baseline test project scaffolding.

Not started:

1. Phase D Infrastructure ownership of DbContext/repositories/migrations.
2. Phase E API thinning and middleware cleanup.
3. Phase F full test coverage and CI quality gates.

## Target Architecture

Proposed projects:

1. TaxExpenseTracker.Domain
2. TaxExpenseTracker.Application
3. TaxExpenseTracker.Infrastructure
4. TaxExpenseTracker.Api
5. TaxExpenseTracker.Tests.Unit
6. TaxExpenseTracker.Tests.Integration

Dependency direction:

- Api -> Application
- Infrastructure -> Application + Domain
- Application -> Domain
- Domain -> (no project dependencies)

## Refactor Principles

1. Keep public API routes and payloads stable during refactor.
2. Move behavior by feature slice (Trackers, Tags, Expenses), not by file type only.
3. Prefer incremental pull requests with green builds at every step.
4. Add tests before and during logic movement where possible.
5. Do not big-bang rewrite.

## Phase Plan

### Phase A - Solution and Project Restructure

Status: Complete

Deliverables:

1. Add new projects for Domain, Application, Infrastructure, and tests.
2. Add project references according to target dependency graph.
3. Keep API project compiling with temporary adapters.

Progress:

1. Done: Domain/Application/Infrastructure projects were added.
2. Done: API/Application/Infrastructure/Domain project references were wired.
3. Done: API compiles against extracted Domain entities.
4. Done: Unit and integration test projects were added to the solution.

Acceptance criteria:

1. Solution builds with all new projects.
2. Existing API still runs unchanged.

### Phase B - Domain Extraction

Status: In progress

Deliverables:

1. Move core entities to Domain:
   - Tracker
   - Tag
   - TaxExpense
   - TaxExpenseTag
2. Introduce domain-level invariants:
   - required names
   - non-negative price
   - valid date semantics
3. Add base domain abstractions (optional at first):
   - Entity base
   - Domain event marker

Progress:

1. Done: Core entities were moved into Domain.
2. Done: Added invariant checks and behavior methods for Tracker/Tag/TaxExpense/TaxExpenseTag.
3. Pending: Add optional base abstractions if needed.

Acceptance criteria:

1. Domain project has no EF Core or ASP.NET dependencies.
2. Existing API behavior remains unchanged.

### Phase C - Application Layer and Use Cases

Status: In progress (scaffolded)

Deliverables:

1. Create use cases for each feature:
   - Commands: create/update/delete
   - Queries: list/get/filter/summary
2. Move validation rules from controllers into application validators.
3. Define repository interfaces in Application or Domain (single convention).
4. Keep DTOs in Application (request/response contracts for use cases).

Progress:

1. Done: Added tracker application contracts (`CreateTrackerCommand`, `UpdateTrackerCommand`, `TrackerReadDto`).
2. Done: Added tracker use-case service (`ITrackerService`, `TrackerService`).
3. Done: Added tracker repository abstraction in Application and EF-backed repository wiring in API.
4. Done: Refactored `TrackersController` to delegate orchestration to `ITrackerService`.
5. Done: Added tag application contracts (`CreateTagCommand`, `UpdateTagCommand`, `TagReadDto`).
6. Done: Added tag use-case service (`ITagService`, `TagService`).
7. Done: Added tag repository abstraction in Application and EF-backed repository wiring in API.
8. Done: Refactored `TagsController` to delegate orchestration to `ITagService`.
9. Done: Added expense application contracts (`CreateExpenseCommand`, `UpdateExpenseCommand`, `ExpenseReadDto`, `ExpenseSummaryDto`).
10. Done: Added expense use-case service (`IExpenseService`, `ExpenseService`).
11. Done: Added expense repository abstraction in Application and EF-backed repository wiring in Infrastructure.
12. Done: Refactored `ExpensesController` to delegate orchestration to `IExpenseService`.
13. Pending: Add dedicated application validators for command/query input where needed.

Acceptance criteria:

1. Controllers delegate to use cases only.
2. Business rules no longer live in controllers.

### Phase D - Infrastructure Layer

Status: In progress

Deliverables:

1. Move AppDbContext and EF configurations to Infrastructure.
2. Implement repositories using EF Core.
3. Keep migrations in Infrastructure or a dedicated migrations project.
4. Add mapping between Domain and persistence models if needed.

Progress:

1. Done: Moved `AppDbContext` to Infrastructure.
2. Done: Moved EF tracker/tag repository implementations to Infrastructure.
3. Done: API composition root now resolves persistence services from Infrastructure.
4. Pending: Move migrations ownership from API to Infrastructure.
5. Pending: Remove direct EF queries from `ExpensesController` via application/infrastructure abstractions.

Acceptance criteria:

1. API project has no direct EF queries.
2. Infrastructure owns persistence details.

### Phase E - API Layer Cleanup

Status: Not started

Deliverables:

1. Keep controllers thin:
   - model binding
   - auth/claims access
   - HTTP response mapping
2. Centralize exception handling middleware.
3. Add versioning strategy if needed.
4. Keep Swagger docs accurate and updated.

Acceptance criteria:

1. Controllers mostly orchestrate HTTP concerns.
2. No duplicated validation or rule logic across controllers.

### Phase F - Testing and Quality Gates

Status: In progress (initial coverage)

Deliverables:

1. Unit tests for domain rules and application handlers.
2. Integration tests for repositories and API endpoints.
3. Add CI checks:
   - build
   - unit tests
   - integration tests
4. Add basic architecture tests (optional): enforce dependency direction.

Progress:

1. Done: Added `TaxExpenseTracker.Tests.Unit` project.
2. Done: Added `TaxExpenseTracker.Tests.Integration` project.
3. Done: Added baseline unit and integration smoke tests.
4. Done: Added initial unit coverage for domain invariants.
5. Done: Added initial unit coverage for tracker and tag application services.
6. Done: Added initial unit coverage for expense application service behavior.
7. Pending: Expand use-case/domain rule coverage and add CI gating.

Acceptance criteria:

1. Core use cases covered by automated tests.
2. Build and tests run in CI before merge.

## Feature Slice Execution Order

Execute phases C and D by vertical feature slice:

1. Trackers
2. Tags
3. Expenses

Reason:

- Lower complexity first, then highest complexity (Expenses).

## Mapping Strategy

1. API contracts stay stable in Api layer models if needed.
2. Application contracts represent use-case input/output.
3. Domain models represent business behavior and invariants.
4. Persistence mappings stay in Infrastructure.

## Risks and Mitigation

1. Risk: route/payload regression.
   Mitigation: contract tests and snapshot checks.
2. Risk: behavior drift during logic moves.
   Mitigation: move one use case at a time with tests.
3. Risk: over-engineering early.
   Mitigation: start simple; only add patterns when needed.

## Definition of Done

1. Layered projects are established and enforced by references.
2. Controllers are thin and use application use cases.
3. Domain is framework-agnostic.
4. Infrastructure owns EF Core and persistence.
5. Automated tests cover critical flows.
6. Existing client-facing API behavior is preserved.

## Suggested First Implementation Sprint

1. Create Domain/Application/Infrastructure projects.
2. Move Tracker entity + Tracker CRUD path end-to-end first.
3. Add unit tests for Tracker rules and handler behavior.
4. Commit and release as milestone: "DDD foundation + Trackers slice".