# Frontend Quality Improvements Plan

## Plan Status Snapshot

- **Status:** Not Started - Deferred
- **Assessed:** 2026-10-25
- **Progress:** 0 of 9 tasks complete
- **Evidence:** The production build succeeds with two budget warnings: the initial bundle is 804.35 kB against a 500 kB budget, and `calendar-batch-day-cell.component.scss` is 5.42 kB against a 4 kB budget. `npm ci` reports 9 audit findings (1 low, 3 moderate, 5 high). MSAL is imported directly in `app.config.ts` even though authentication is disabled by default. The calendar batch page now shows full Monday-Friday weeks, but adjacent-month days look the same as in-month days.
- **Authority:** This plan is the sole tracker for these frontend improvements. Azure rollout remains in [AZURE_HARDENING_ROLLOUT_PLAN.md](AZURE_HARDENING_ROLLOUT_PLAN.md).

Last Updated: 2026-10-25
Current Task: None - deferred until selected in `NEXT_WORK.md`

## Goal

Remove known build warnings and dependency risk, and make the calendar batch page clearer, without changing API contracts.

## Constraints

- Preserve current routes, API contracts, and authentication behavior.
- Do not raise a budget only to silence a warning; any budget change requires a recorded decision with measurements.
- Do not upgrade outside supported Angular 22 ranges without a separate plan.
- Every behavior change includes a regression test.

## Progress Dashboard

| Stage | Scope | Done | Total | Status | Depends On |
|---|---|---:|---:|---|---|
| 1 | Dependency security | 0 | 2 | Not Started | None |
| 2 | Bundle and style budgets | 0 | 4 | Not Started | None |
| 3 | Calendar batch clarity | 0 | 3 | Not Started | None |
| **Total** |  | **0** | **9** | **Not Started** |  |

Stages are independent and can be delivered in any order.

## Stage 1: Dependency Security

- [ ] **FQ-01** Triage every `npm audit` finding: package, severity, runtime versus build-only exposure, and fixed version.
- [ ] **FQ-02** Apply compatible fixes, then record any remaining finding as an accepted risk with justification and review date.

### Acceptance Criteria

- No high-severity finding affects runtime code without a recorded decision.
- `npm ci`, tests, and production build pass after upgrades.

## Stage 2: Bundle And Style Budgets

- [ ] **FQ-03** Measure initial bundle composition with `ng build --stats-json` and record the largest contributors.
- [ ] **FQ-04** Load MSAL only when authentication is enabled, so disabled mode does not ship or initialize it in the initial bundle. Keep existing auth-session and interceptor tests passing.
- [ ] **FQ-05** Remove any remaining eager imports identified by FQ-03 (for example Angular Material modules used only by lazy routes).
- [ ] **FQ-06** Reduce `calendar-batch-day-cell.component.scss` below 4 kB by moving shared rules to a shared partial or global styles.

### Acceptance Criteria

- The production build reports no budget warnings, or a recorded decision justifies a measured budget change.
- Authentication-enabled mode still logs in, attaches tokens, and recovers expired sessions.

## Stage 3: Calendar Batch Clarity

- [ ] **FQ-07** Mark days outside the selected month and de-emphasize them visually, with an accessible label (for example "outside selected month"). They remain editable.
- [ ] **FQ-08** Add a view toggle between full Monday-Friday weeks and selected-month days only. Persist the choice locally; full weeks is the default.
- [ ] **FQ-09** Confirm pending-change counts, navigation locking, and batch submission behave correctly when the same adjacent-month date appears when viewing either neighboring month.

### Acceptance Criteria

- Component tests cover outside-month marking, both view modes, and the persisted preference.
- Edits to adjacent-month days submit correctly and appear when that month is viewed.

## Validation

```powershell
npm --prefix Frontend ci
npm --prefix Frontend run test:ci
npm --prefix Frontend run build
npm --prefix Frontend audit
```

## Open Decisions

| ID | Question | Default Until Decided |
|---|---|---|
| FQ-DEC-01 | Is a budget increase acceptable if the measured floor stays above 500 kB? | No; reduce first |
| FQ-DEC-02 | Should adjacent-month days be read-only instead of editable? | Editable |

## Blocker Log

| Date | Task | Blocker | Resolution | Status |
|---|---|---|---|---|
|  |  |  |  |  |

## Validation Log

| Date | Task | Validation | Result | Notes |
|---|---|---|---|---|
|  |  |  |  |  |

## Completion Rule

Mark this plan complete when all non-removed tasks are validated. Then update `NEXT_WORK.md` and the README if behavior changed, and move this plan to `plans/archive/`.
