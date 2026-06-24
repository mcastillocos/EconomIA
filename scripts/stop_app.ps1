# ============================================================
# EconomIA - STOP APP
# Para Backend, Frontend y (opcionalmente) Docker
# ============================================================
param(
    [switch]$KeepDocker,
    [switch]$Force
)

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot | Split-Path -Parent

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  EconomIA - Parando aplicacion" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

# ---- 1. Parar Backend ----
$pidFile = Join-Path $root ".pid_backend"
if (Test-Path $pidFile) {
    $backendPid = Get-Content $pidFile -ErrorAction SilentlyContinue
    if ($backendPid -and $backendPid -ne $PID) {
        $proc = Get-Process -Id $backendPid -ErrorAction SilentlyContinue
        if ($proc) {
            Stop-Process -Id $backendPid -Force -ErrorAction SilentlyContinue
            Write-Host "[OK] Backend parado (PID $backendPid)" -ForegroundColor Green
        } else {
            Write-Host "[--] Backend ya no estaba corriendo" -ForegroundColor DarkGray
        }
    }
    Remove-Item $pidFile -Force
} else {
    Write-Host "[--] Backend PID file no encontrado" -ForegroundColor DarkGray
}

# ---- 2. Parar Frontend ----
$pidFile = Join-Path $root ".pid_frontend"
if (Test-Path $pidFile) {
    $frontendPid = Get-Content $pidFile -ErrorAction SilentlyContinue
    if ($frontendPid -and $frontendPid -ne $PID) {
        $proc = Get-Process -Id $frontendPid -ErrorAction SilentlyContinue
        if ($proc) {
            # Matar proceso y sus hijos (node spawns children)
            try {
                taskkill /PID $frontendPid /T /F 2>&1 | Out-Null
            } catch {
                Stop-Process -Id $frontendPid -Force -ErrorAction SilentlyContinue
            }
            Write-Host "[OK] Frontend parado (PID $frontendPid)" -ForegroundColor Green
        } else {
            Write-Host "[--] Frontend ya no estaba corriendo" -ForegroundColor DarkGray
        }
    }
    Remove-Item $pidFile -Force
} else {
    Write-Host "[--] Frontend PID file no encontrado" -ForegroundColor DarkGray
}

# ---- 3. Parar Docker ----
if (-not $KeepDocker) {
    Write-Host ""
    Write-Host "Parando contenedores Docker..." -ForegroundColor Yellow
    $dockerCompose = Join-Path $root "docker\docker-compose.yml"
    docker compose -f $dockerCompose stop 2>&1 | ForEach-Object {
        Write-Host "  $_" -ForegroundColor DarkGray
    }
    Write-Host "[OK] Docker parado" -ForegroundColor Green
} else {
    Write-Host "[--] Docker: mantenido activo (-KeepDocker)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "  Todo parado." -ForegroundColor Green
Write-Host ""
