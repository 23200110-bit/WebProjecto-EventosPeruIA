$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$apiOutput = 'bin\Debug\net10.0\EventosPeruIA.API.dll'
$apiDirectory = Join-Path $repoRoot 'EventosPeruIA.API'
$signalDirectory = Join-Path $env:TEMP 'EventosPeruIA-agent'
$signalPath = Join-Path $signalDirectory 'restart.json'
$healthUrl = 'http://127.0.0.1:5279/health'
$resultUrl = 'http://127.0.0.1:5279/internal/local-agent/restart-result'
$supervisorToken = [guid]::NewGuid().ToString('N')

New-Item -ItemType Directory -Force -Path $signalDirectory | Out-Null
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5279'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:AdminAgent__WorkspaceRoot = $repoRoot
$env:AdminAgent__RestartSignalPath = $signalPath
$env:AdminAgent__RestartToken = $supervisorToken
$env:AdminAgent__SupervisorToken = $supervisorToken
$env:AdminAgent__LocalSupervisorEnabled = 'true'

function Start-Backend {
    if (-not (Test-Path (Join-Path $apiDirectory $apiOutput))) {
        throw "Backend output is missing: $apiOutput"
    }
    Start-Process -FilePath 'dotnet' `
        -ArgumentList @($apiOutput) `
        -WorkingDirectory $apiDirectory -NoNewWindow -PassThru
}

function Build-Backend {
    & dotnet build 'EventosPeruIA.API/EventosPeruIA.API.csproj' --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
}

function Test-BackendPort {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $pending = $client.BeginConnect('127.0.0.1', 5279, $null, $null)
        if (-not $pending.AsyncWaitHandle.WaitOne(500)) { return $false }
        $client.EndConnect($pending)
        return $true
    }
    catch { return $false }
    finally { $client.Close() }
}

function Stop-Backend([System.Diagnostics.Process] $Process) {
    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force
        $Process.WaitForExit()
    }
}

function Wait-ForHealth([System.Diagnostics.Process] $Process) {
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        if ($null -ne $Process -and $Process.HasExited) { return $false }
        try {
            $health = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 3
            if ($health.status -eq 'ok') { return $true }
        }
        catch { }
        Start-Sleep -Seconds 1
    }
    return $false
}

function Write-Manifest([object] $Manifest) {
    $temporaryPath = "$signalPath.tmp"
    [System.IO.File]::WriteAllText($temporaryPath, (ConvertTo-Json $Manifest -Depth 8), [System.Text.UTF8Encoding]::new($false))
    if (Test-Path $signalPath) { [System.IO.File]::Replace($temporaryPath, $signalPath, $null) }
    else { [System.IO.File]::Move($temporaryPath, $signalPath) }
}

function Restore-OriginalFile([object] $Manifest) {
    if ([System.IO.Path]::GetFullPath($Manifest.workspaceRoot).TrimEnd('\') -ne $repoRoot.TrimEnd('\')) {
        throw 'Restart manifest workspace does not match this checkout.'
    }
    $relativePath = [string]$Manifest.filePath
    $segments = $relativePath.Split('/')
    $invalidSegment = @($segments | Where-Object { $_ -in @('', '.', '..') -or $_ -match '[\x00-\x1f]' }).Count -gt 0
    if ($relativePath.Contains('\') -or $segments.Count -lt 2 -or $invalidSegment -or
        -not ($relativePath.StartsWith('EventosPeruIA.API/') -or $relativePath.StartsWith('EventosPeruIA.CORE/')) -or
        -not $relativePath.EndsWith('.cs')) {
        throw 'Restart manifest contains an invalid source path.'
    }
    $sourcePath = Join-Path $repoRoot ($relativePath.Replace('/', '\'))
    $original = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Manifest.originalContentBase64))
    $temporaryPath = "$sourcePath.agent-rollback"
    [System.IO.File]::WriteAllText($temporaryPath, $original, [System.Text.UTF8Encoding]::new($false))
    if (Test-Path -LiteralPath $sourcePath) { [System.IO.File]::Replace($temporaryPath, $sourcePath, $null) }
    else { [System.IO.File]::Move($temporaryPath, $sourcePath) }
    foreach ($staleFile in @("$sourcePath.agent-tmp", "$sourcePath.agent-rollback")) {
        if (Test-Path -LiteralPath $staleFile) { Remove-Item -LiteralPath $staleFile -Force }
    }
}

function Report-RestartResult([object] $Manifest, [bool] $Healthy, [string] $Details) {
    $body = @{ patchId = $Manifest.patchId; healthy = $Healthy; details = $Details } | ConvertTo-Json
    try {
        Invoke-RestMethod -Uri $resultUrl -Method Post `
            -Headers @{ 'X-Local-Agent-Token' = $supervisorToken } `
            -ContentType 'application/json' -Body $body -TimeoutSec 5 | Out-Null
    }
    catch {
        Write-Warning "Could not report local restart outcome for patch $($Manifest.patchId): $($_.Exception.Message)"
    }
}

function Complete-Restart([object] $Manifest, [System.Diagnostics.Process] $CurrentProcess) {
    if ($Manifest.state -in @('validated', 'restart_pending')) {
        Start-Sleep -Seconds 2
        $Manifest.state = 'restarting'
        Write-Manifest $Manifest
        Stop-Backend $CurrentProcess
        $CurrentProcess = Start-Backend
    }

    if (Wait-ForHealth $CurrentProcess) {
        Report-RestartResult $Manifest $true 'Build/tests passed and the restarted API health check succeeded.'
        Remove-Item -LiteralPath $signalPath -Force
        Write-Host "Patch $($Manifest.patchId) is active; health check passed."
        return $CurrentProcess
    }

    Stop-Backend $CurrentProcess
    Restore-OriginalFile $Manifest
    Build-Backend
    $CurrentProcess = Start-Backend
    if (Wait-ForHealth $CurrentProcess) {
        Report-RestartResult $Manifest $false 'The updated API failed health check; original source was restored.'
        Remove-Item -LiteralPath $signalPath -Force
        Write-Warning "Patch $($Manifest.patchId) was rolled back after health check failure."
        return $CurrentProcess
    }

    throw "Rollback completed, but the original backend did not become healthy. Restart manifest retained at $signalPath."
}

Push-Location $repoRoot
$backend = $null
try {
    if (Test-BackendPort) { throw 'Port 5279 is already in use; stop the existing backend before using this supervisor.' }
    & dotnet restore 'ProyectoFinalWeb-EeventosPeruIA.slnx'
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    & dotnet build 'EventosPeruIA.API/EventosPeruIA.API.csproj' --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Initial backend build failed.' }

    $backend = Start-Backend
    if (-not (Wait-ForHealth $backend)) {
        if (Test-Path $signalPath) {
            $failedManifest = Get-Content -LiteralPath $signalPath -Raw | ConvertFrom-Json
            Stop-Backend $backend
            Restore-OriginalFile $failedManifest
            Build-Backend
            $backend = Start-Backend
            if (-not (Wait-ForHealth $backend)) { throw 'Backend failed to start after restoring the original source.' }
            Report-RestartResult $failedManifest $false 'New backend failed startup health check; original source was restored.'
            Remove-Item -LiteralPath $signalPath -Force
        }
        else { throw 'Backend did not become healthy on startup.' }
    }
    if (Test-Path $signalPath) {
        $startupManifest = Get-Content -LiteralPath $signalPath -Raw | ConvertFrom-Json
        if ($startupManifest.state -eq 'validating') {
            Stop-Backend $backend
            Restore-OriginalFile $startupManifest
            Build-Backend
            $backend = Start-Backend
            if (-not (Wait-ForHealth $backend)) { throw 'Backend failed to recover after restoring an interrupted patch.' }
            Report-RestartResult $startupManifest $false 'Agent host restarted during validation; source was restored.'
            Remove-Item -LiteralPath $signalPath -Force
        }
        elseif ($startupManifest.state -in @('validated', 'restart_pending', 'restarting')) {
            $backend = Complete-Restart $startupManifest $backend
        }
    }
    Write-Host "Local backend ready at $healthUrl. Keep this window open for autonomous restarts."

    while ($true) {
        if (Test-Path $signalPath) {
            $manifest = Get-Content -LiteralPath $signalPath -Raw | ConvertFrom-Json
            if ($manifest.state -in @('restart_pending', 'restarting') -or
                ($manifest.state -eq 'validated' -and $backend.HasExited)) {
                $backend = Complete-Restart $manifest $backend
            }
            elseif ($backend.HasExited -and $manifest.state -eq 'validating') {
                Restore-OriginalFile $manifest
                Build-Backend
                $backend = Start-Backend
                if (-not (Wait-ForHealth $backend)) { throw 'Backend failed to recover after restoring an unvalidated patch.' }
                Report-RestartResult $manifest $false 'Agent process exited before validation completed; source was restored.'
                Remove-Item -LiteralPath $signalPath -Force
            }
        }
        elseif ($backend.HasExited) {
            Write-Warning 'Backend process exited unexpectedly; restarting it.'
            Start-Sleep -Seconds 2
            $backend = Start-Backend
            if (-not (Wait-ForHealth $backend)) { throw 'Backend failed its health check after an unexpected exit.' }
        }
        Start-Sleep -Seconds 1
    }
}
finally {
    if (Test-Path $signalPath) {
        try {
            $manifest = Get-Content -LiteralPath $signalPath -Raw | ConvertFrom-Json
            Restore-OriginalFile $manifest
            $manifest.state = 'validating'
            Write-Manifest $manifest
        }
        catch { Write-Warning "Could not safely restore the pending local patch: $($_.Exception.Message)" }
    }
    Stop-Backend $backend
    Pop-Location
}
