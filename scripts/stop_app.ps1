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
    $pid = Get-Content $pidFile -ErrorAction SilentlyContinue
    if ($pid) {
        $proc = Get-Process -Id $pid -ErrorAction SilentlyContinue
        if ($proc) {
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            Write-Host "[OK] Backend parado (PID $pid)" -ForegroundColor Green
        } else {
            Write-Host "[--] Backend ya no estaba corriendo" -ForegroundColor DarkGray
        }
    }
    Remove-Item $pidFile -Force
} else {
    # Fallback: buscar procesos dotnet con EconomIA
    $dotnetProcs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -like "*EconomIA.API*"
    }
    if ($dotnetProcs) {
        $dotnetProcs | Stop-Process -Force
        Write-Host "[OK] Backend parado (fallback)" -ForegroundColor Green
    } else {
        Write-Host "[--] Backend no encontrado" -ForegroundColor DarkGray
    }
}

# ---- 2. Parar Frontend ----
$pidFile = Join-Path $root ".pid_frontend"
if (Test-Path $pidFile) {
    $pid = Get-Content $pidFile -ErrorAction SilentlyContinue
    if ($pid) {
        $proc = Get-Process -Id $pid -ErrorAction SilentlyContinue
        if ($proc) {
            # npm spawns child node process, kill the tree
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            Write-Host "[OK] Frontend parado (PID $pid)" -ForegroundColor Green
        } else {
            Write-Host "[--] Frontend ya no estaba corriendo" -ForegroundColor DarkGray
        }
    }
    Remove-Item $pidFile -Force
} else {
    Write-Host "[--] Frontend no encontrado" -ForegroundColor DarkGray
}

# Kill any orphan node processes on port 3000
$nodeOnPort = Get-NetTCPConnection -LocalPort 3000 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
if ($nodeOnPort) {
    $nodeOnPort | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
    Write-Host "[OK] Procesos huerfanos en :3000 eliminados" -ForegroundColor Green
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
