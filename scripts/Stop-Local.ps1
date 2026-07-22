[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

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
        [int]$TimeoutSeconds = 8
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

$targetPorts = @(4200, 7152, 5158)

Write-Host 'Stopping local frontend/backend listeners...'
Stop-ListeningPorts -Ports $targetPorts

if (-not (Wait-ForPortsFree -Ports $targetPorts -TimeoutSeconds 8)) {
    Write-Host 'Some ports are still in use after stop attempts:' -ForegroundColor Yellow
    Get-PortOwners -Ports $targetPorts | Format-Table -AutoSize
    throw 'Shutdown incomplete. Close the listed processes (or run elevated) and execute this script again.'
}

Write-Host 'Local services stopped. Ports are free:'
$targetPorts | ForEach-Object { Write-Host "  - $_" }
