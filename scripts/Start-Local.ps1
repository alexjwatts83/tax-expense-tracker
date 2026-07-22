[CmdletBinding()]
param(
    [switch]$ForceRestart
)

$ErrorActionPreference = 'Stop'

$scriptDir = if ($PSScriptRoot) {
    $PSScriptRoot
}
else {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Split-Path -Parent $scriptDir
$frontendPath = Join-Path $repoRoot 'Frontend'
$solutionPath = Join-Path $repoRoot 'TaxExpenseTracker.sln'

if (-not (Test-Path $solutionPath)) {
    throw "Could not locate solution file at '$solutionPath'. Ensure this script stays under the repository's scripts folder."
}

if (-not (Test-Path (Join-Path $frontendPath 'package.json'))) {
    throw "Could not locate frontend package.json at '$frontendPath'."
}

function Stop-ListeningPorts {
    param(
        [int[]]$Ports
    )

    $pids = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in $Ports } |
        Select-Object -ExpandProperty OwningProcess -Unique

    foreach ($processId in $pids) {
        try {
            Stop-Process -Id $processId -Force -ErrorAction Stop
            Write-Host "Stopped process $processId listening on requested ports."
        }
        catch {
            Write-Warning "Stop-Process failed for ${processId}: $($_.Exception.Message). Trying taskkill..."
            & taskkill /PID $processId /F | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "taskkill terminated process $processId."
            }
            else {
                Write-Warning "Unable to terminate process ${processId} with taskkill (exit code $LASTEXITCODE)."
            }
        }
    }
}

function Get-PortOwners {
    param(
        [int[]]$Ports
    )

    $listeners = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in $Ports } |
        Select-Object -Property LocalPort, OwningProcess -Unique

    foreach ($listener in $listeners) {
        $processName = '<unknown>'
        try {
            $proc = Get-Process -Id $listener.OwningProcess -ErrorAction Stop
            $processName = $proc.ProcessName
        }
        catch {
        }

        [PSCustomObject]@{
            Port = $listener.LocalPort
            ProcessId = $listener.OwningProcess
            ProcessName = $processName
        }
    }
}

function Wait-ForPortsFree {
    param(
        [int[]]$Ports,
        [int]$TimeoutSeconds = 10
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $owners = Get-PortOwners -Ports $Ports
        if (-not $owners) {
            return $true
        }

        Start-Sleep -Milliseconds 400
    } while ((Get-Date) -lt $deadline)

    return $false
}

function Test-IsExpectedOwner {
    param(
        [int]$Port,
        [string]$ProcessName
    )

    switch ($Port) {
        4200 { return $ProcessName -eq 'node' }
        7152 { return $ProcessName -in @('dotnet', 'TaxExpenseTracker.Api') }
        5158 { return $ProcessName -in @('dotnet', 'TaxExpenseTracker.Api') }
        default { return $false }
    }
}

function Start-BackendService {
    param(
        [string]$RepoRoot,
        [string]$SolutionPath
    )

    Write-Host 'Building backend solution...'
    Set-Location $RepoRoot
    & dotnet build $SolutionPath
    if ($LASTEXITCODE -ne 0) {
        throw 'Backend build failed. Startup aborted.'
    }

    Write-Host 'Starting backend API in a new terminal...'
    Start-Process powershell -ArgumentList @(
        '-NoExit',
        '-Command',
        "Set-Location '$RepoRoot'; dotnet run --project Backend/TaxExpenseTracker.Api --launch-profile https"
    )
}

function Start-FrontendService {
    param(
        [string]$FrontendPath
    )

    Write-Host 'Starting frontend app in a new terminal...'
    Start-Process powershell -ArgumentList @(
        '-NoExit',
        '-Command',
        "Set-Location '$FrontendPath'; npm start"
    )
}

$targetPorts = @(4200, 7152, 5158)
$owners = @(Get-PortOwners -Ports $targetPorts)
$frontendOwner = $owners | Where-Object { $_.Port -eq 4200 }
$backendOwners = $owners | Where-Object { $_.Port -in @(7152, 5158) }

$frontendRunning = $false
if ($frontendOwner) {
    $frontendRunning = Test-IsExpectedOwner -Port $frontendOwner.Port -ProcessName $frontendOwner.ProcessName
}

$backendRunning = ($backendOwners.Count -gt 0) -and ($backendOwners | Where-Object {
    Test-IsExpectedOwner -Port $_.Port -ProcessName $_.ProcessName
}).Count -gt 0

$unexpectedOwners = $owners | Where-Object {
    -not (Test-IsExpectedOwner -Port $_.Port -ProcessName $_.ProcessName)
}

if (($owners.Count -gt 0) -and (-not $ForceRestart)) {
    if ($unexpectedOwners.Count -gt 0) {
        Write-Host 'One or more required ports are already in use by unexpected processes:' -ForegroundColor Yellow
        $unexpectedOwners | Format-Table -AutoSize
        throw 'Startup blocked. Use Stop-Local.ps1 to clear ports, or rerun with -ForceRestart to attempt takeover.'
    }

    if ($frontendRunning -and $backendRunning) {
        Write-Host 'Local services already appear to be running. No action taken.'
        Write-Host '  Frontend: http://localhost:4200'
        Write-Host '  Swagger:  https://localhost:7152/swagger'
        return
    }
}

if ($ForceRestart) {
    Write-Host 'Force restart requested. Stopping existing frontend/backend listeners...'
    Stop-ListeningPorts -Ports @(4200)
    Stop-ListeningPorts -Ports @(7152, 5158)

    if (-not (Wait-ForPortsFree -Ports $targetPorts -TimeoutSeconds 8)) {
        Write-Host 'Ports are still in use after stop attempts:' -ForegroundColor Yellow
        Get-PortOwners -Ports $targetPorts | Format-Table -AutoSize
        throw 'Cannot continue while required ports are in use. Close the listed processes and run the script again.'
    }
}

if (-not $frontendRunning) {
    if ((Get-PortOwners -Ports @(4200)).Count -gt 0) {
        Write-Host 'Port 4200 is currently in use and frontend is not recognized as already running:' -ForegroundColor Yellow
        Get-PortOwners -Ports @(4200) | Format-Table -AutoSize
        throw 'Frontend startup blocked by port 4200 ownership.'
    }

    Start-FrontendService -FrontendPath $frontendPath
}

if (-not $backendRunning) {
    if ((Get-PortOwners -Ports @(7152, 5158)).Count -gt 0) {
        Write-Host 'Backend port(s) are currently in use and backend is not recognized as already running:' -ForegroundColor Yellow
        Get-PortOwners -Ports @(7152, 5158) | Format-Table -AutoSize
        throw 'Backend startup blocked by port ownership.'
    }

    Start-BackendService -RepoRoot $repoRoot -SolutionPath $solutionPath
}

Write-Host 'Done. Use:'
Write-Host '  Frontend: http://localhost:4200'
Write-Host '  Swagger:  https://localhost:7152/swagger'