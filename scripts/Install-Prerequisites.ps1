[CmdletBinding()]
param(
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Gray
}

function Write-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Has-Command {
    param([Parameter(Mandatory = $true)][string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Description,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    if ($DryRun) {
        Write-Info "DRY RUN: $Description"
        return
    }

    Write-Info $Description
    & $Action
}

function Require-Winget {
    if (-not (Has-Command "winget")) {
        throw "winget is required to auto-install prerequisites. Install App Installer from Microsoft Store and re-run."
    }
}

Write-Section "Tax Expense Tracker Prerequisite Installer"
Write-Host "Dry run: $DryRun"

Require-Winget

Write-Section "Checking and Installing Required Tools"

if (-not (Has-Command "git")) {
    Invoke-Step -Description "Installing Git (Git.Git)" -Action {
        winget install --id Git.Git -e --source winget --accept-source-agreements --accept-package-agreements
    }
}
else {
    Write-Pass "Git already installed"
}

if (-not (Has-Command "dotnet")) {
    Invoke-Step -Description "Installing .NET SDK 10 (Microsoft.DotNet.SDK.10)" -Action {
        winget install --id Microsoft.DotNet.SDK.10 -e --source winget --accept-source-agreements --accept-package-agreements
    }
}
else {
    Write-Pass ".NET already installed"
}

if (-not (Has-Command "volta")) {
    Invoke-Step -Description "Installing Volta (Volta.Volta)" -Action {
        winget install --id Volta.Volta -e --source winget --accept-source-agreements --accept-package-agreements
    }
}
else {
    Write-Pass "Volta already installed"
}

Write-Section "Refreshing PATH"
Invoke-Step -Description "Refreshing PATH for current session" -Action {
    $machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = "$machinePath;$userPath"
}

Write-Section "Installing Node Toolchain via Volta"

if ((-not $DryRun) -and (-not (Has-Command "volta"))) {
    Write-Warn "Volta is still unavailable in this session. Open a new terminal and run this script again."
    exit 1
}

Invoke-Step -Description "Installing Node LTS via Volta" -Action {
    volta install node@lts
}

Invoke-Step -Description "Pinning Node LTS for this repository" -Action {
    Push-Location (Join-Path $PSScriptRoot "..")
    try {
        volta pin node@lts
    }
    finally {
        Pop-Location
    }
}

Invoke-Step -Description "Installing Angular CLI via Volta" -Action {
    volta install @angular/cli
}

Write-Section "Verification"

$missingAfterInstall = New-Object System.Collections.Generic.List[string]

foreach ($cmd in @("git", "dotnet", "volta", "node", "npm", "ng")) {
    if (Has-Command $cmd) {
        Write-Pass "$cmd is available"
    }
    else {
        Write-Fail "$cmd is missing"
        $missingAfterInstall.Add($cmd) | Out-Null
    }
}

if ($missingAfterInstall.Count -eq 0) {
    Write-Pass "All prerequisites are installed."
    Write-Host ""
    Write-Host "Next step:" -ForegroundColor Cyan
    Write-Host "  powershell -ExecutionPolicy Bypass -File .\scripts\Check-Prerequisites.ps1"
    exit 0
}

Write-Warn "Some tools are still missing: $($missingAfterInstall -join ', ')"
Write-Warn "Try opening a new terminal and re-running this script or install missing tools manually."
exit 1