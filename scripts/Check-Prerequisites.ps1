[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Get-CommandVersion {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [string[]]$Args = @("--version")
    )

    try {
        $output = & $Command @Args 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }

        if ($output -is [array]) {
            return ($output | Select-Object -First 1)
        }

        return [string]$output
    }
    catch {
        return $null
    }
}

function Format-Version {
    param([string]$VersionText)

    if ([string]::IsNullOrWhiteSpace($VersionText)) {
        return "unknown"
    }

    return $VersionText.Trim()
}

function Parse-SemVerMajor {
    param([string]$VersionText)

    if ([string]::IsNullOrWhiteSpace($VersionText)) {
        return $null
    }

    $match = [regex]::Match($VersionText, "(\d+)\.(\d+)\.(\d+)")
    if (-not $match.Success) {
        return $null
    }

    return [int]$match.Groups[1].Value
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$missing = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

Write-Section "Tax Expense Tracker Prerequisite Check"
Write-Host "Repository: $repoRoot"

Write-Section "Required Tools"

# Git
$gitCmd = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $gitCmd) {
    Write-Fail "Git is not installed or not on PATH."
    $missing.Add("Git") | Out-Null
}
else {
    $gitVersion = Get-CommandVersion -Command "git" -Args @("--version")
    Write-Pass "Git found ($(Format-Version $gitVersion))"
}

# .NET SDK 10+
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnetCmd) {
    Write-Fail ".NET SDK is not installed or not on PATH."
    $missing.Add(".NET SDK 10+") | Out-Null
}
else {
    $dotnetVersion = Get-CommandVersion -Command "dotnet" -Args @("--version")
    $dotnetMajor = Parse-SemVerMajor -VersionText $dotnetVersion
    if ($null -eq $dotnetMajor) {
        Write-Warn "Could not parse .NET version from '$dotnetVersion'."
        $warnings.Add("Unable to parse .NET SDK version") | Out-Null
    }
    elseif ($dotnetMajor -lt 10) {
        Write-Fail ".NET SDK 10+ required, found $dotnetVersion."
        $missing.Add(".NET SDK 10+") | Out-Null
    }
    else {
        Write-Pass ".NET SDK found ($(Format-Version $dotnetVersion))"
    }
}

# Volta
$voltaCmd = Get-Command volta -ErrorAction SilentlyContinue
if ($null -eq $voltaCmd) {
    Write-Fail "Volta is not installed or not on PATH."
    $missing.Add("Volta") | Out-Null
}
else {
    $voltaVersion = Get-CommandVersion -Command "volta" -Args @("--version")
    Write-Pass "Volta found ($(Format-Version $voltaVersion))"
}

# Node.js (expected to be managed by Volta)
$nodeCmd = Get-Command node -ErrorAction SilentlyContinue
if ($null -eq $nodeCmd) {
    Write-Fail "Node.js is not installed or not on PATH."
    $missing.Add("Node.js LTS") | Out-Null
}
else {
    $nodeVersionRaw = Get-CommandVersion -Command "node" -Args @("--version")
    $nodeVersion = (Format-Version $nodeVersionRaw).TrimStart("v")
    Write-Pass "Node.js found (v$nodeVersion)"
}

# npm
$npmCmd = Get-Command npm -ErrorAction SilentlyContinue
if ($null -eq $npmCmd) {
    Write-Fail "npm is not installed or not on PATH."
    $missing.Add("npm") | Out-Null
}
else {
    $npmVersion = Get-CommandVersion -Command "npm" -Args @("--version")
    Write-Pass "npm found ($(Format-Version $npmVersion))"
}

# Angular CLI
$ngCmd = Get-Command ng -ErrorAction SilentlyContinue
if ($null -eq $ngCmd) {
    Write-Fail "Angular CLI (ng) is not installed or not on PATH."
    $missing.Add("Angular CLI") | Out-Null
}
else {
    $ngVersionOutput = Get-CommandVersion -Command "ng" -Args @("version", "--json")
    if ($null -eq $ngVersionOutput) {
        $ngVersionOutput = Get-CommandVersion -Command "ng" -Args @("version")
    }
    Write-Pass "Angular CLI found"
}

Write-Section "Project-Specific Checks"

# Local dotnet tool manifest
$toolManifestPath = Join-Path $repoRoot "dotnet-tools.json"
if (-not (Test-Path $toolManifestPath)) {
    Write-Fail "dotnet-tools.json not found at repo root."
    $missing.Add("dotnet local tools manifest") | Out-Null
}
else {
    Write-Pass "dotnet-tools.json found"
}

# Backend project exists
$apiProjectPath = Join-Path $repoRoot "Backend\TaxExpenseTracker.Api\TaxExpenseTracker.Api.csproj"
if (-not (Test-Path $apiProjectPath)) {
    Write-Fail "Backend API project file not found."
    $missing.Add("Backend API project") | Out-Null
}
else {
    Write-Pass "Backend API project found"
}

Write-Section "Summary"

if ($warnings.Count -gt 0) {
    foreach ($warning in $warnings) {
        Write-Warn $warning
    }
}

if ($missing.Count -eq 0) {
    Write-Pass "All required prerequisites are installed for the current plan phase."
    Write-Host ""
    Write-Host "Suggested next commands:" -ForegroundColor Cyan
    Write-Host "  cd Backend/TaxExpenseTracker.Api"
    Write-Host "  dotnet restore"
    Write-Host "  dotnet ef database update"
    Write-Host "  dotnet user-secrets set \"Security:ApiKey\" \"<your-local-dev-api-key>\""
    Write-Host "  dotnet run"
    exit 0
}

Write-Fail "Missing prerequisites: $($missing -join ', ')"
Write-Host ""
Write-Host "Setup hints:" -ForegroundColor Yellow
Write-Host "  - Install .NET SDK 10"
Write-Host "  - Install Git"
Write-Host "  - Install Volta, then run:"
Write-Host "      volta install node@lts"
Write-Host "      volta pin node@lts"
Write-Host "      volta install @angular/cli"
Write-Host ""
exit 1