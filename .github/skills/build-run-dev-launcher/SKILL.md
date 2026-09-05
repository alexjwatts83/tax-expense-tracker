---
name: build-run-dev-launcher
description: 'Build and run the TaxExpenseTrackerDevLauncher Windows desktop app. Use when asked to launch, start, build, rebuild, validate, or troubleshoot the local Tax Expense Tracker development launcher.'
argument-hint: 'Optional: Debug, Release, test, or rebuild'
user-invocable: true
disable-model-invocation: false
---

# Build And Run The Dev Launcher

Build and start the repository's WPF development launcher without confusing launcher readiness with the state of the API and Angular services it manages.

## Preconditions

- Run only on Windows.
- Work from the repository containing `TaxExpenseTracker.sln` and `tools/TaxExpenseTrackerDevLauncher/TaxExpenseTrackerDevLauncher.csproj`.
- Require the .NET 10 SDK. WebView2 is required for the embedded preview but not for compilation.
- Do not treat `scripts/Check-Prerequisites.ps1` as a launcher-only gate because it also checks unrelated infrastructure tools. Run it only when the user requests the full repository prerequisite check.

## Procedure

1. Confirm the repository root and required project files exist. Stop with a precise missing-path error if they do not.
2. Check for an existing `TaxExpenseTrackerDevLauncher` process.
   - If one is running and the user only asked to run the launcher, report its PID and do not start a duplicate.
   - If a rebuild is requested while one is running, do not terminate it automatically. Explain that it may lock build outputs and obtain approval before stopping it.
3. Select `Debug` unless the user explicitly requests `Release`.
4. Build the launcher project from the repository root:

   ```powershell
   dotnet build tools/TaxExpenseTrackerDevLauncher/TaxExpenseTrackerDevLauncher.csproj --configuration Debug
   ```

   Replace `Debug` with `Release` when requested. Do not launch if the build fails.
5. When the user asks to test or validate the launcher, run the focused test project after the build:

   ```powershell
   dotnet test tools/TaxExpenseTrackerDevLauncher.Tests/TaxExpenseTrackerDevLauncher.Tests.csproj --configuration Debug --no-restore
   ```

   Use the selected configuration. Report test failures before deciding whether launch should continue; do not hide them.
6. Recheck that no launcher process appeared during validation, then start the WPF app as a long-running asynchronous process:

   ```powershell
   dotnet run --project tools/TaxExpenseTrackerDevLauncher/TaxExpenseTrackerDevLauncher.csproj --configuration Debug --no-build
   ```

7. Confirm the launch command remains active or the launcher process exists. Report the process ID when available.
8. Tell the user that the launcher window is running. Do not claim the API or frontend is running until the launcher reports those services as **Running**.

## Failure Branches

- **Missing .NET 10 SDK:** Report the installed SDK state and stop before build.
- **Build output locked:** Identify a running launcher or test process. Ask before stopping any process, then rebuild.
- **Repository not found:** Return to the directory containing `TaxExpenseTracker.sln`; the launcher also relies on this repository marker at runtime.
- **WebView2 initialization failure:** Recommend installing or repairing Microsoft Edge WebView2 Runtime. The launcher service controls may still work without its embedded preview.
- **Port conflict after launch:** Let the launcher display the owning process. Never use **Force free port** or terminate an external process without explicit user approval.
- **API or frontend startup failure:** Inspect the launcher's relevant output tab, then validate that service directly using the commands documented in `tools/TaxExpenseTrackerDevLauncher/README.md`.

## Completion Checks

- The targeted launcher build exits successfully.
- Focused launcher tests pass when testing was requested.
- Exactly one launcher instance is running.
- The final response states the configuration used, build/test outcome, and launcher PID or any blocking error.
- The final response distinguishes the launcher process from its managed API and frontend service states.