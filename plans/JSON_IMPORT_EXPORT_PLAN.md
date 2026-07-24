# JSON Import/Export Plan

Last Updated: 2026-07-24

## Objective

Add functionality to:

- Export reference/master entities as one "big JSON" payload.
- Import JSON back into the system.
- Import transactional entities (`expenses`, `work entries`, `leave entries`) via separate JSON imports.

## Scope

Entities in scope:

- Tracker
- Tag
- Bank
- TaxExpense
- TaxExpenseTag
- WorkLocationEntry
- LeaveEntry
- PublicHoliday

Big export includes only:

- Tracker
- Tag
- Bank
- PublicHoliday

Separate import flows required for:

- TaxExpense (and associated TaxExpenseTag payload)
- WorkLocationEntry
- LeaveEntry

Out of scope (phase 1):

- File encryption at rest
- Cross-version automatic migrations of old backup formats
- Background job queue for imports

## Guiding Decisions

1. Keep existing entity IDs during import by default.
2. Provide both per-entity endpoints and a full reference-data backup endpoint.
3. Use streamed JSON responses for large exports.
4. Use transaction boundaries and dependency-aware import ordering.
5. Add dry-run validation mode for imports before data mutation.

## API Design

Base route suggestion: `/api/data-transfer`

### Full Backup Export

- `GET /api/data-transfer/export`
- Query params:
  - `includeSoftDeleted=true|false` (default: `true`)
  - `pretty=true|false` (default: `false`)
- Response: `application/json`
- Payload: reference-data backup envelope (see schema below)

### Full Backup Import

- `POST /api/data-transfer/import`
- Content-Type: `application/json`
- Query params:
  - `mode=upsert|insertOnly|replace` (default: `upsert`)
  - `dryRun=true|false` (default: `false`)
  - `allowDeletes=true|false` (default: `false`; only meaningful with `replace`)
- Response:
  - Validation summary
  - Created/updated/skipped/error counts per entity

This endpoint handles reference data only:

- trackers
- tags
- banks
- publicHolidays

### Per-Entity Export

- `GET /api/data-transfer/export/{entityName}`
- `entityName`: `trackers|tags|banks|expenses|expense-tags|work-locations|leave|public-holidays`
- Query params:
  - `includeSoftDeleted=true|false` (where relevant)

### Per-Entity Import

- `POST /api/data-transfer/import/{entityName}`
- Content-Type: `application/json`
- Query params:
  - `mode=upsert|insertOnly|replace`
  - `dryRun=true|false`

## JSON Payload Contract

Use an envelope to support metadata and versioning.

```json
{
  "schemaVersion": 1,
  "exportedAtUtc": "2026-07-24T06:00:00Z",
  "source": {
    "app": "TaxExpenseTracker",
    "environment": "local"
  },
  "data": {
    "trackers": [],
    "tags": [],
    "banks": [],
    "publicHolidays": []
  }
}
```

Separate import JSON files:

- `expenses-import.json` containing `expenses` + `expenseTags`
- `work-location-import.json` containing `workLocationEntries`
- `leave-import.json` containing `leaveEntries`

### Schema Rules

- IDs must be valid GUIDs.
- Date-only values should stay in ISO date format (`yyyy-MM-dd`) where used by current API contracts.
- Date-time values should be UTC ISO (`yyyy-MM-ddTHH:mm:ssZ`).
- Unknown top-level sections should be ignored with warnings (forward compatibility).

## Import Dependency Order

Required order to satisfy foreign keys and references:

1. Trackers
2. Tags
3. Banks
4. PublicHolidays

Notes:

- For separate transactional imports:
  - `TaxExpense` depends on `Bank` and `Tracker`.
  - `TaxExpenseTag` depends on both `TaxExpense` and `Tag`.

## Import Semantics

### `insertOnly`

- Insert when ID does not exist.
- If ID exists, skip and record warning.

### `upsert`

- Insert when missing.
- Update mutable fields when existing.
- Preserve immutable fields (`CreatedAt`) unless explicitly configured.

### `replace`

- Advanced mode.
- Synchronize DB with payload for targeted scope.
- Requires transaction and optional `allowDeletes=true` for removing rows not present in payload.

### `dryRun`

- No writes.
- Return all validation and conflict results as if import were executed.

## Separate Transactional Imports

### Expenses Import

- Endpoint: `POST /api/data-transfer/import/expenses`
- Payload sections: `expenses`, `expenseTags`
- Precondition: referenced `banks`, `trackers`, and `tags` already exist.

### Work Entries Import

- Endpoint: `POST /api/data-transfer/import/work-locations`
- Payload section: `workLocationEntries`

### Leave Entries Import

- Endpoint: `POST /api/data-transfer/import/leave`
- Payload section: `leaveEntries`

## Validation Checklist

- JSON well-formed and matches envelope structure.
- `schemaVersion` supported.
- IDs unique within each collection.
- Foreign key references resolvable inside payload or existing DB (based on mode).
- One-entry-per-date domain rules for WorkLocation/Leave still enforced (in separate imports).
- Public holiday uniqueness and workable flag format valid.
- Decimal ranges and required text fields valid.

## Backend Implementation Plan

### Phase 1: Contracts and Application Layer

- [ ] Add DTO contracts in Application layer for:
  - Export envelope
  - Per-entity payloads
  - Import request options
  - Import result summary
- [ ] Add `IDataTransferService` abstraction.
- [ ] Implement `DataTransferService` with:
  - Reference-data export builder
  - Reference-data import orchestration
  - Separate transactional import orchestration
  - Validation and dry-run execution

### Phase 2: Infrastructure and Persistence

- [ ] Add repository helpers for bulk read/write patterns.
- [ ] Add transaction support wrapping full import operations.
- [ ] Implement efficient lookup dictionaries keyed by ID for upsert.
- [ ] Keep EF tracking/memory pressure low (batching + no-tracking reads for export).

### Phase 3: API Endpoints

- [ ] Add `DataTransferController` in API project.
- [ ] Add endpoints for:
  - Full reference-data export/import
  - Per-entity export/import
  - Dedicated imports for expenses/work/leave
- [ ] Stream large export response (`IAsyncEnumerable`/stream writer pattern).
- [ ] Add request-size and timeout safeguards.

### Phase 4: Safety and Observability

- [ ] Add structured logs for import/export start/end and counts.
- [ ] Add correlation ID in responses for troubleshooting.
- [ ] Add explicit warning/error codes in import result payload.
- [ ] Optional: feature flag or environment guard for import endpoints in Production.

## Frontend Implementation Plan

### Admin Data Transfer Screen

- [ ] Add route/page: `/data-transfer`.
- [ ] Export actions:
  - Export Reference Data JSON
  - Export per-entity JSON
- [ ] Import actions:
  - Upload JSON file
  - Import Expenses JSON
  - Import Work Entries JSON
  - Import Leave Entries JSON
  - Select mode (`insertOnly`, `upsert`, `replace`)
  - Optional dry-run toggle
  - Show validation summary before confirm
- [ ] Show result report table (created/updated/skipped/errors by entity).

## Testing Plan

### Unit Tests

- [ ] Serialization/deserialization for envelope and entity payloads.
- [ ] Import mode behavior (`insertOnly`, `upsert`, `replace`).
- [ ] Dependency ordering and FK validation.
- [ ] Dry-run returns expected report with no DB mutation.

### Integration Tests

- [ ] Export (reference-data) returns valid payload with expected sections.
- [ ] Import (reference-data) roundtrip test:
  - Seed reference data -> export -> clear reference data -> import -> compare counts/key fields.
- [ ] Separate expenses import succeeds when references exist.
- [ ] Separate work entries import succeeds.
- [ ] Separate leave entries import succeeds.
- [ ] Import rejects invalid foreign keys.
- [ ] Import handles duplicate IDs according to mode.

### Manual Tests

- [ ] Large export/download in browser.
- [ ] Large import upload and progress feedback.
- [ ] Error messages readable for malformed files.

## Performance Considerations

- Use gzip compression for large JSON responses.
- Use paged reads or streaming to avoid large in-memory object graphs.
- Batch writes and disable expensive change tracking where safe.
- Consider import chunking if payload size grows significantly.

## Rollout Plan

1. Implement backend export/import services and endpoints behind feature flag.
2. Add tests and run integration suite.
3. Add frontend admin page with dry-run first workflow.
4. Enable in non-prod and run roundtrip verification with realistic dataset.
5. Enable in prod with guarded access and monitoring.

## Risks and Mitigations

- Risk: Data overwrite in `replace` mode.
  - Mitigation: Require explicit mode and confirmation, default to `upsert`.
- Risk: Invalid payload relationships.
  - Mitigation: Full pre-validation and dry-run.
- Risk: Large payload memory pressure.
  - Mitigation: streaming and batching.
- Risk: Version drift over time.
  - Mitigation: `schemaVersion` with parser branching.

## Acceptance Criteria

- Export reference entities to one JSON payload succeeds.
- Export any single entity to JSON succeeds.
- Import full reference payload in `upsert` mode succeeds with deterministic summary.
- Import per-entity payload succeeds with deterministic summary.
- Separate expenses import succeeds and links tags correctly.
- Separate work entries import succeeds.
- Separate leave entries import succeeds.
- Dry-run mode validates and returns issues without modifying data.
- Roundtrip (reference export -> clean reference data -> import) restores functional reference relationships.