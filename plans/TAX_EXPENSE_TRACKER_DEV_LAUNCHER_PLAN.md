# Tax Expense Tracker Dev Launcher - Project Plan

## Plan Status Snapshot

- **Status:** Mostly Complete - Functional Delivery Complete, Quality Gates Remaining
- **Assessed:** 2026-08-16T13:11:29+10:00
- **Evidence:** 52 of 60 checklist items are complete. Service control, logs, API file tailing, embedded desktop and known-phone previews, port safety, documentation, and 15 launcher tests are present.
- **Remaining gate:** Add service-state, bounded UI-log, and process integration tests; verify all toolbar controls, independent scripts, and remaining lifecycle scenarios.
- **Authority:** This snapshot supersedes older progress text below until the next dated assessment.

## Project Overview

`TaxExpenseTrackerDevLauncher` is a Windows desktop control panel for this repository. It starts
and stops the ASP.NET Core API and Angular frontend, streams their console output into one
searchable log view, and embeds the running frontend in a second tab.

The launcher complements the existing `scripts/Start-Local.ps1` and `scripts/Stop-Local.ps1`
workflow. Those scripts remain available for terminal use and automation.

---

## Goals

- Provide Start, Stop, and Restart controls for the API and frontend.
- Provide Start All, Stop All, and Restart All controls with deterministic service ordering.
- Display live service states: `Stopped`, `Starting`, `Running`, `Stopping`, and `Crashed`.
- Stream stdout and stderr from both child process trees in real time.
- Support service and text filtering, clearing, auto-scroll, and saving logs to a file.
- Embed `http://localhost:4200` in an App tab.
- View and follow API log files from `C:\logs\TaxExpenseTracker.Api` in a dedicated tab.
- Show process id, uptime, port, and local URL for each service.
- Stop every process tree started by the launcher when the launcher closes.
- Detect occupied service ports without silently terminating unrelated processes.

## Non-Goals

- Controlling remote, shared, or production environments.
- Accepting arbitrary executables, commands, arguments, or working directories from the UI.
- Replacing the existing PowerShell scripts as the scriptable local-development entry point.
- Managing Azure deployment or infrastructure.
- Providing authentication or multi-user access.

---

## Technology Decision

Use WPF targeting `net10.0-windows`, matching the repository's current .NET SDK and backend
target framework.

- **UI:** WPF and XAML.
- **View models:** `CommunityToolkit.Mvvm` for observable properties and commands.
- **Process control:** `System.Diagnostics.Process` with redirected stdout and stderr.
- **Embedded frontend:** Microsoft Edge WebView2 (`Microsoft.Web.WebView2`).
- **Dependency injection:** Avoid unless launcher complexity later justifies it.

WPF is intentionally Windows-only. The repository's local scripts use Windows PowerShell and the
requested tool is a WPF utility, so adding a cross-platform desktop framework is unnecessary.

---

## Location

```text
tools/
  TaxExpenseTrackerDevLauncher/
    TaxExpenseTrackerDevLauncher.csproj
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    Models/
      ServiceDefinition.cs
      ServiceState.cs
      LogLine.cs
    Services/
      RepositoryLocator.cs
      ServiceRegistry.cs
      ProcessSupervisor.cs
      PortInspector.cs
      ApiLogFileReader.cs
    ViewModels/
      MainViewModel.cs
      ServiceViewModel.cs
      LogViewModel.cs
    Views/
      ServiceCard.xaml
      LogPanel.xaml
```

The project is included in `TaxExpenseTracker.sln`.

---

## Service Definitions

Service definitions are a fixed list compiled into `ServiceRegistry`. Repository paths resolve by
walking upward from the launcher executable until `TaxExpenseTracker.sln` is found.

| Id | Executable | Arguments | Working directory | Ports | Ready signal | URL |
|---|---|---|---|---|---|---|
| `api` | `dotnet` | `run --project Backend/TaxExpenseTracker.Api --launch-profile https` | repository root | 7152, 5158 | `Now listening on:` | `https://localhost:7152/swagger` |
| `web` | `npm.cmd` | `start` | `Frontend` | 4200 | Angular local server ready output or successful HTTP probe | `http://localhost:4200` |

Start All launches the API process first and then the frontend without making frontend startup
depend on API readiness detection. Stop All uses the reverse order. Each command uses `ProcessStartInfo.ArgumentList`,
`UseShellExecute = false`, and explicit working directories.

The launcher's commands must remain aligned with `scripts/Start-Local.ps1`,
`scripts/Stop-Local.ps1`, the API launch profiles, and `Frontend/package.json`.

---

## Process Supervision

### Lifecycle

`ProcessSupervisor` owns only processes it starts and tracks each parent process, start time, and
last exit code.

- Start is a no-op when the service is already starting or running.
- Stop first requests normal termination when practical, then uses
  `Process.Kill(entireProcessTree: true)` after a short timeout.
- Restart awaits a complete stop before starting a new process.
- Unexpected process exit changes the service to `Crashed` and retains its final output.
- Closing the main window awaits shutdown of all launcher-owned process trees.
- A dispatcher timer refreshes uptime without creating one timer per service.

### Readiness

A service remains `Starting` until its readiness log pattern is observed and its configured port
is listening. A startup timeout changes the state to `Crashed` and emits a diagnostic log line.
Combining output matching with a port check avoids reporting `Running` too early.

### Output Streaming

- Redirect standard output and standard error.
- Set output encodings to UTF-8.
- Use asynchronous output/error events or asynchronous stream reads.
- Marshal collection changes onto the WPF dispatcher.
- Tag every line with timestamp, service id, stream, and text.
- Keep at most 5,000 lines in memory, removing the oldest lines at capacity.
- Do not add environment variables, secrets, or connection-string values to launcher logs.

### Port Handling

Before starting a service, `PortInspector` checks its configured ports. If a port is occupied by a
process the launcher does not own, startup is blocked and the UI displays the process name and id.
A Force Free Port action requires an explicit confirmation before terminating that process tree.
The launcher never silently kills an external process.

---

## UI Design

### Main Window

- Compact header with launcher name and overall status.
- Toolbar with Start All, Stop All, and Restart All commands.
- One service row/card per service with state, process id, uptime, ports, local link, and controls.
- Logs, Frontend Output, API Log Files, and App tabs fill the remaining window height.
- Status colors remain readable without relying on color alone.

### Logs Tab

- Virtualized list for responsive rendering with thousands of lines.
- Distinct API and Web labels; stderr uses an error foreground and stream label.
- Service filter controls and a text search field.
- Auto-scroll toggle, Clear action, and Save action.
- Filtering uses an `ICollectionView` rather than copying log collections.
- Auto-scroll pauses when disabled and does not steal the user's scroll position.

### Frontend Output Tab

- Display only redirected stdout, stderr, and launcher messages from the Web service.
- Show npm and Angular startup progress independently from the combined Logs tab filters.
- Provide Web Start and Stop controls alongside the current service state.
- Support independent text filtering and auto-scroll without duplicating stored log lines.

### API Log Files Tab

- Read log files only from the fixed `C:\logs\TaxExpenseTracker.Api` directory.
- List available files by most recently modified first and select the newest file by default.
- Display the selected file in a read-only, virtualized log view.
- Follow appended lines while the selected file is active, without locking the file against NLog.
- Support text filtering, auto-scroll, manual refresh, and Open Folder actions.
- Keep only a bounded number of loaded lines in memory when following a large or long-lived file.
- Detect log rotation, file replacement, truncation, and deletion without crashing the launcher.
- Show clear empty, missing-directory, access-denied, and read-error states.
- Stop file watchers and release file handles when the tab or launcher closes.

### App Tab

- WebView2 navigates to `http://localhost:4200` after the Web service reaches `Running`.
- Switch between a full-width desktop preview and known Android/iPhone viewport sizes for responsive development.
- Show a useful stopped/starting/error state while the site is unavailable.
- Provide Reload and Open in Browser actions.
- Repeated service restarts reuse the browser control and navigate again when ready.
- WebView2 initialization and navigation failures appear in the launcher log.

---

## Safety and Error Handling

- Launchable services and arguments are immutable application definitions.
- No local listening socket or remote control API is added.
- UI commands are disabled while conflicting lifecycle operations are in progress.
- Missing `dotnet`, `npm.cmd`, repository files, frontend dependencies, or WebView2 Runtime produce
  actionable errors instead of unhandled exceptions.
- The API Log Files tab is read-only and cannot delete, rename, or modify log files.
- A port conflict identifies the owner and requires confirmation before force termination.
- Process disposal, cancellation, and window shutdown are idempotent.
- The launcher does not run package installation, database migration, or restore commands without
  an explicit future product decision.

---

## Implementation Phases

### Phase 1 - Project Shell

1. [x] Create `tools/TaxExpenseTrackerDevLauncher` as a WPF project targeting `net10.0-windows`.
2. [x] Add `TaxExpenseTrackerDevLauncher.csproj` to `TaxExpenseTracker.sln`.
3. [x] Verify the generated WPF project builds.
4. [x] Add `CommunityToolkit.Mvvm` and `Microsoft.Web.WebView2` dependencies.
5. [x] Add the planned folders and application theme resources.

### Phase 2 - Service Model and Process Control

1. [x] Implement repository-root discovery with a clear failure message.
2. [x] Define the fixed API and Web service metadata.
3. [x] Implement start, stop, restart, and process-tree termination.
4. [x] Implement service state transitions, pid, exit code, and uptime tracking.
5. [x] Implement Start All, Stop All, and Restart All ordering.
6. [x] Stop launcher-owned services during window shutdown.

### Phase 3 - Live Logs

1. [x] Capture stdout and stderr asynchronously using UTF-8.
2. [x] Marshal log events safely to the WPF dispatcher.
3. [x] Implement a bounded 5,000-line observable log collection.
4. [x] Add the virtualized, color-coded log list.
5. [x] Add service filters, text filtering, and auto-scroll.
6. [x] Add Clear and Save to File actions.

### Phase 4 - Launcher UI and Embedded App

1. [x] Build service controls with state, pid, uptime, ports, and links.
2. [x] Bind command availability to service lifecycle state.
3. [x] Add a dedicated Frontend Output tab with Web controls, filtering, and auto-scroll.
4. [x] Complete the Logs and App tabs.
5. [x] Implement API log file discovery under `C:\logs\TaxExpenseTracker.Api`.
6. [x] Implement read-only file loading, live tailing, rotation handling, and bounded retention.
7. [x] Add API log file selection, text filtering, auto-scroll, refresh, and Open Folder actions.
8. [x] Initialize WebView2 and navigate when the frontend is ready.
9. [x] Add Reload and Open in Browser actions.
10. [x] Add unavailable, loading, and navigation-error states.
11. [x] Add desktop and selectable known-phone mobile preview modes.

### Phase 5 - Robustness

1. [x] Detect readiness from output plus listening ports.
2. [x] Add startup timeout and failed-start diagnostics.
3. [x] Detect external process termination and report `Crashed`.
4. [x] Detect port conflicts and display process details.
5. [x] Add confirmed Force Free Port behavior.
6. [x] Verify repeated start/stop/restart cycles leave no orphan processes.

### Phase 6 - Testing and Documentation

1. [ ] Unit test valid service state transitions.
2. [ ] Unit test bounded log eviction and filtering.
3. [x] Unit test API log file discovery, appended-line reads, truncation, and rotation handling.
4. [x] Unit test repository-root discovery and service definitions.
5. [ ] Add integration coverage for start, readiness, stop, and port release where practical.
6. [x] Manually verify external termination reports `Crashed`.
7. [ ] Manually verify API log viewing when the folder is missing, empty, active, and rotating.
8. [x] Manually verify closing the launcher leaves ports 4200, 7152, and 5158 free.
9. [x] Document launcher prerequisites and usage in the root README.
10. [x] Add a VS Code task or documented `dotnet run` command for launching the tool.
11. [x] Run the complete solution build and relevant test suites.

---

## Validation Checklist

- [ ] API can be started, stopped, and restarted independently.
- [ ] Frontend can be started, stopped, and restarted independently.
- [x] Start All launches API first and starts both services without a readiness dependency.
- [x] Stop All terminates both complete process trees.
- [x] Status, pid, uptime, and exit information remain accurate.
- [x] stdout and stderr appear live and remain responsive at the 5,000-line limit.
- [ ] Every log toolbar control works with both services running.
- [x] Frontend Output tab shows live npm and Angular progress without API lines.
- [x] API Log Files tab lists and follows files from `C:\logs\TaxExpenseTracker.Api`.
- [x] API Log Files tab handles missing folders, empty folders, truncation, and rotation cleanly.
- [x] App tab loads the Angular application and recovers after a frontend restart.
- [x] Port conflicts never terminate an unrelated process without confirmation.
- [x] Closing the launcher leaves no launcher-owned `dotnet` or `node` descendants.
- [ ] Existing PowerShell start/stop scripts continue to work independently.
- [x] `dotnet build TaxExpenseTracker.sln` succeeds.

---

## Open Decisions

1. Should the launcher persist its window size, selected tab, filters, and auto-scroll preference?
2. Should Save to File export the visible filtered lines or all retained lines? Recommended: all
   retained lines, with the active filter stated in the save dialog.
3. Should readiness require a successful HTTP request in addition to log and port detection?
   Recommended: use an HTTP probe for the frontend and a port plus log signal for the API.
4. Should the launcher offer a database migration action later? Recommended: keep it out of the
   first version and retain `scripts/Update-Database.ps1` as the explicit workflow.

---

## Progress Notes

- 2026-08-16: Created the `TaxExpenseTrackerDevLauncher` WPF shell, added it to the solution, and
  verified that the generated `net10.0-windows` project builds successfully.