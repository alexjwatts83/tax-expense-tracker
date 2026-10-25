# Dev Launcher Improvements Plan

## Plan Status Snapshot

- **Status:** In Progress
- **Assessed:** 2026-10-25
- **Progress:** 1 of 15 tasks complete
- **Evidence:** The launcher builds, runs, and passes its focused test project. Service commands, ports, readiness signals, timeouts, log folder, and log line limits are currently hardcoded.
- **Authority:** This plan is the sole tracker for TaxExpenseTrackerDevLauncher enhancements. It does not change the API or frontend.

Last Updated: 2026-10-25
Current Task: DL-02 / Persistent validated launcher settings

## Goal

Make the launcher more reliable, configurable, and useful for daily development without weakening its process-safety guarantees.

## Constraints

- Windows-only WPF application targeting .NET 10.
- Never terminate a process the launcher does not own without explicit confirmation and ownership revalidation.
- Keep service definitions aligned with `scripts/Start-Local.ps1`, `scripts/Stop-Local.ps1`, API launch profiles, and `Frontend/package.json`.
- Defaults must reproduce today's behavior; new behavior is opt-in unless a decision below says otherwise.
- Do not disable TLS certificate validation. Readiness probes report an untrusted development certificate as a distinct failure.
- Do not store secrets in launcher settings.

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete and validated
- `[!]` Blocked; add an entry to the blocker log
- `[-]` Removed from scope by a recorded decision

## Progress Dashboard

| Stage | Scope | Done | Total | Status | Depends On |
|---|---|---:|---:|---|---|
| 1 | Foundations: single instance and settings | 1 | 3 | In Progress | None |
| 2 | Readiness and resilience | 0 | 3 | Not Started | Stage 1 |
| 3 | Developer workflow | 0 | 6 | Not Started | Stage 1 |
| 4 | Log usability | 0 | 3 | Not Started | Stage 1 |
| **Total** |  | **1** | **15** | **In Progress** |  |

## Stage 1: Foundations

Goal: prevent competing launcher instances and remove hardcoded operational values.

- [x] **DL-01** Enforce a single launcher instance per repository using a named mutex derived from the repository root. A second start activates the existing window and exits.
- [ ] **DL-02** Add a validated launcher settings model persisted per user under `%LOCALAPPDATA%\TaxExpenseTrackerDevLauncher\settings.json`. Missing values use current defaults; an invalid file falls back to defaults and shows a visible warning rather than failing startup.
- [ ] **DL-03** Move startup timeout (60 s), graceful stop timeout (2 s), API log folder, and output line limit (5,000) from constants into settings.

### Acceptance Criteria

- Starting the launcher twice leaves exactly one process and focuses the original window.
- Deleting or corrupting the settings file restores today's behavior with a visible notice.
- Unit tests cover settings defaults, validation bounds, and invalid-file fallback.

## Stage 2: Readiness And Resilience

Goal: report service health accurately and make failures visible.

- [ ] **DL-04** After the API startup signal and port check succeed, require `GET /health` to return 200 before reporting **Running**. Report timeout, unhealthy response, and untrusted certificate as distinct states with actionable messages.
- [ ] **DL-05** Distinguish user-initiated stops from unexpected exits. Show a **Crashed** state with exit code and last output lines. Add optional automatic restart, disabled by default, with bounded retries and backoff.
- [ ] **DL-06** Allow selecting the API launch profile (`https` or `http`) and build configuration (`Debug` or `Release`). Read ports for the selected profile from `Backend/TaxExpenseTracker.Api/Properties/launchSettings.json` instead of duplicating them.

### Acceptance Criteria

- An API that listens but cannot reach its database is reported unhealthy, not running.
- Killing a managed process externally produces **Crashed**, not **Stopped**.
- Automatic restart never exceeds its retry limit and never runs when disabled.
- Profile ports stay correct when `launchSettings.json` changes.

## Stage 3: Developer Workflow

Goal: reduce context switching for common local tasks.

- [ ] **DL-07** Persist window size/position, selected tab, preview viewport, and log filters between sessions.
- [ ] **DL-08** Add an auto-start option (setting and `--start-all` argument) that starts all services after the launcher opens.
- [ ] **DL-09** Add a launcher-specific prerequisite panel: .NET 10 SDK, Node/npm, WebView2 runtime, `Frontend/node_modules`, and HTTPS development certificate trust (`dotnet dev-certs https --check --trust`). Each failed check shows the remediation command; the launcher never runs installers automatically.
- [ ] **DL-10** Add quick actions that run as tracked, cancellable tasks with output in the log view: backend tests, frontend tests, `npm ci`, `scripts/Update-Database.ps1`, and `scripts/List-Migrations.ps1`. Warn before running database updates while the API is running.
- [ ] **DL-11** Add per-service links for Swagger, `/health`, the frontend URL, and the API log folder.
- [ ] **DL-12** Add an optional API watch mode using `dotnet watch`, with readiness detection that tolerates rebuild restarts.

### Acceptance Criteria

- Restarting the launcher restores the previous layout and filters.
- Every prerequisite failure is shown with a specific remediation step.
- Quick actions cannot run concurrently with a conflicting action and can be cancelled without orphaned processes.
- Watch-mode rebuilds do not mark the API as crashed.

## Stage 4: Log Usability

Goal: find problems faster in service and API log output.

- [ ] **DL-13** Highlight error and warning lines and add next/previous error navigation.
- [ ] **DL-14** Add API log file search by correlation ID and regular expression. Regex evaluation uses a match timeout to avoid pathological patterns.
- [ ] **DL-15** Make the in-memory output and API log line limits configurable within validated bounds.

### Acceptance Criteria

- Searching a known correlation ID returns every matching line across the API log files.
- An invalid or slow regex reports an error without freezing the UI.
- Memory remains bounded at the configured limit.

## Validation

Run after each task:

```powershell
dotnet build tools/TaxExpenseTrackerDevLauncher/TaxExpenseTrackerDevLauncher.csproj
dotnet test tools/TaxExpenseTrackerDevLauncher.Tests/TaxExpenseTrackerDevLauncher.Tests.csproj
```

Then run the launcher with the `build-run-dev-launcher` skill and manually verify the task's acceptance criteria.

## Open Decisions

| ID | Question | Default Until Decided |
|---|---|---|
| DL-DEC-01 | Should settings be per user or committed to the repository? | Per user under `%LOCALAPPDATA%` |
| DL-DEC-02 | Should automatic restart ever be enabled by default? | Disabled |
| DL-DEC-03 | Should quick actions be allowed while services are running? | Allowed, except database updates require confirmation |

## Blocker Log

| Date | Task | Blocker | Resolution | Status |
|---|---|---|---|---|
|  |  |  |  |  |

## Validation Log

| Date | Task | Validation | Result | Notes |
|---|---|---|---|---|
| 2026-10-25 | DL-01 | Focused coordinator tests, full launcher tests/build, and desktop double-launch check | Pass: 3 focused tests; 25 total tests; build succeeded | Duplicate exits 0 before creating a window or supervisor; original minimized window restores and gains foreground focus; one instance remains. Different repository keys and restart after disposal covered. |

## Completion Rule

Mark this plan complete when all non-removed tasks are validated. Then move durable usage information into `tools/TaxExpenseTrackerDevLauncher/README.md`, update `NEXT_WORK.md`, and move this plan to `plans/archive/`.
