[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$infrastructureProject = Join-Path $repoRoot 'Backend\TaxExpenseTracker.Infrastructure'
$apiProject = Join-Path $repoRoot 'Backend\TaxExpenseTracker.Api'
$databasePath = Join-Path $apiProject 'tax-expense-tracker.dev.db'
$connectionString = "Data Source=$databasePath"

Push-Location $repoRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to restore .NET tools.'
    }

    & dotnet ef migrations list `
        --project $infrastructureProject `
        --startup-project $apiProject `
        --context AppDbContext `
        --connection $connectionString

    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to list EF migrations.'
    }
}
finally {
    Pop-Location
}