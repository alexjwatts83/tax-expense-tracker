# Tax Expense Tracker Dev Launcher

Windows desktop launcher for running and monitoring the Tax Expense Tracker API and Angular frontend during local development. It provides one place to start services, inspect output, resolve port conflicts, and preview the application.

## Prerequisites

- Windows 10 or later
- .NET 10 SDK
- Node.js and npm
- Microsoft Edge WebView2 Runtime
- The backend and frontend dependencies installed for this repository

From the repository root, verify the development prerequisites with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Check-Prerequisites.ps1
```

The API must also have its normal local configuration, database, and user secrets set up. See the repository [README](../../README.md) for the full setup process.

## Run the launcher

Run this command from the repository root:

```powershell
dotnet run --project tools/TaxExpenseTrackerDevLauncher/TaxExpenseTrackerDevLauncher.csproj
```

The launcher locates the repository by searching parent directories for `TaxExpenseTracker.sln`. Starting it from outside the repository will fail.

Only one launcher instance runs per repository in the current Windows session. Opening it again restores and activates the existing window, then exits the duplicate without starting or stopping managed services.

## Launcher Settings

On first launch, the app saves defaults to `%LOCALAPPDATA%\TaxExpenseTrackerDevLauncher\settings.json` and shows a notice. Missing JSON properties use their defaults. Invalid JSON or values cause the whole file to fall back to defaults with a visible warning; the invalid file is preserved for correction. Storage errors also produce a warning rather than preventing startup.

```json
{
	"startupTimeoutSeconds": 60,
	"gracefulStopTimeoutSeconds": 2,
	"apiLogFolder": "C:\\logs\\TaxExpenseTracker.Api",
	"maximumLogLines": 5000,
	"autoRestartEnabled": false,
	"maximumRestartAttempts": 3,
	"restartBackoffSeconds": 2
}
```

Startup timeout must be 1-600 seconds, graceful-stop timeout 1-60 seconds, and maximum log lines 100-100,000. The API log folder must be an absolute path. Do not store secrets in this file.

Automatic restart is disabled by default. Set `autoRestartEnabled` to `true` to retry unexpected process exits. `maximumRestartAttempts` accepts 1-10 retries after the initial start; `restartBackoffSeconds` accepts 1-60 seconds. Delays double per retry, capped at 60 seconds. The per-service retry budget resets only on explicit **Start** or **Restart**, not when a retry reaches Running. **Stop** cancels pending retries, and closing the launcher never restarts services. Port ownership and API readiness checks still apply on every attempt.

Settings take effect when the launcher restarts. The timeouts control service startup and graceful-stop waits. `maximumLogLines` caps retained process output and API file-log lines, including the initial tail and live updates. `apiLogFolder` controls log discovery and **Open log folder**; it does not change the API's NLog output destination, which must be configured separately if moved.

## Managed services

| Service | Command | Ports | URL |
|---|---|---|---|
| API | `dotnet run --project Backend/TaxExpenseTracker.Api --launch-profile https` | 7152, 5158 | https://localhost:7152/swagger |
| Web | `npm.cmd start` in `Frontend` | 4200 | http://localhost:4200 |

A service is reported as **Running** only after its expected startup output is detected and its configured ports are listening. The API additionally requires HTTP 200 from `https://localhost:7152/health` within the configured startup timeout. HTTPS certificate validation remains enabled, and redirects are not followed. Frontend readiness is unchanged.

## Features

- Start, stop, or restart the API and Web services independently or together.
- View service state, process ID, uptime, ports, exit code, and local URL.
- Inspect unexpected exits as **Crashed**, with an exit code and the last three output lines; hover over the summary for the last 20 lines. Intentional stops remain **Stopped**.
- Filter combined API and frontend output, enable auto-scroll, clear the view, or save logs to a file.
- Use the dedicated **Frontend Output** tab for npm and Angular output.
- Follow API log files from the configured `apiLogFolder` (default `C:\logs\TaxExpenseTracker.Api`) in the **API Log Files** tab.
- Preview the frontend in the **App** tab using desktop or known Android and iPhone viewport sizes.
- Open the frontend in the system browser.
- Detect processes already using required ports and display their process IDs.
- Stop launcher-owned child process trees when the launcher closes.

The launcher retains the newest lines up to `maximumLogLines` in memory (default 5,000).

## Port conflicts

If another process owns a required port, startup is blocked and the owner is shown in the service panel. **Force free port** is displayed only while a conflict exists.

This action terminates an external process tree, so the launcher requires explicit confirmation. It verifies the process identity and port ownership again before terminating anything. Review the displayed process name and PID before confirming.

## Troubleshooting

### The launcher cannot find the repository

Run the launcher from the repository root or another directory beneath it. The repository must contain `TaxExpenseTracker.sln`.

### WebView2 initialization fails

Install or repair the Microsoft Edge WebView2 Runtime. Service controls and logs remain separate from the embedded preview.

### The frontend does not start

Install its dependencies and verify it runs directly:

```powershell
Set-Location Frontend
npm install
npm start
```

### The API does not start

Check the **Logs** tab for configuration, certificate, database, or port errors. Verify the API directly from the repository root:

```powershell
dotnet run --project Backend/TaxExpenseTracker.Api --launch-profile https
```

### The API listens but is not Running

Check the service state and **Logs** tab:

- **Unhealthy:** `/health` returned a non-200 response. Check the API/database configuration and logs; a listening API with an unreachable database is not ready.
- **Certificate error:** HTTPS certificate validation or TLS negotiation failed. Run `dotnet dev-certs https --check --trust` and check the certificate hostname and expiry. The launcher never bypasses certificate validation.
- **Health timeout:** Startup output, listening ports, and a successful health response were not all observed before `startupTimeoutSeconds`. Check connectivity and API logs, or increase the timeout if startup is legitimately slow.

The failed-health process remains owned by the launcher for inspection. Use **Stop** or **Restart** after correcting the problem; **Start** stays disabled to prevent a duplicate process. These are startup readiness checks, not continuous health monitoring.

### A service keeps crashing

Check the crash summary and service output. If automatic restart is enabled, the **Logs** tab reports each retry delay and when the retry budget is exhausted. Use **Stop** to cancel a queued retry. After fixing the cause, use **Start** or **Restart** to begin a fresh retry budget. Automatic restart applies to unexpected exits, not health-check failures where the process is still alive.

### API log files are not listed

The **API Log Files** tab reads the configured `apiLogFolder` (default `C:\logs\TaxExpenseTracker.Api`). Ensure it matches the API's NLog output destination. Start the API so it can create logs, then select **Refresh**. The launcher only reads these files; it does not modify them.

## Development

The main implementation areas are:

- `Services/ServiceRegistry.cs`: service commands, ports, readiness signals, and URLs
- `Services/ProcessSupervisor.cs`: process lifecycle, readiness, output capture, and cleanup
- `Services/ApiLogFileReader.cs`: API log discovery and live tailing
- `ViewModels/`: commands, state, filtering, and bounded log collections
- `MainWindow.xaml`: launcher controls, log tabs, and application preview

Keep service definitions aligned with `scripts/Start-Local.ps1`, `scripts/Stop-Local.ps1`, the API launch profiles, and `Frontend/package.json`.

Run launcher tests from the repository root:

```powershell
dotnet test tools/TaxExpenseTrackerDevLauncher.Tests/TaxExpenseTrackerDevLauncher.Tests.csproj
```

Build the launcher directly with:

```powershell
dotnet build tools/TaxExpenseTrackerDevLauncher/TaxExpenseTrackerDevLauncher.csproj
```
