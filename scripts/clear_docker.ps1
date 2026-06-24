# ============================================================
# EconomIA - CLEAR DOCKER
# Elimina TODOS los contenedores, volumes y datos del proyecto
# para empezar de cero
# ============================================================
param(
    [switch]$Confirm,
    [switch]$KeepImages
)

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot | Split-Path -Parent

Write-Host ""
Write-Host "======================================================" -ForegroundColor Red
Write-Host "  EconomIA - CLEAR DOCKER (reset total)" -ForegroundColor Red
Write-Host "======================================================" -ForegroundColor Red
Write-Host ""
Write-Host "  Esto eliminara:" -ForegroundColor Yellow
Write-Host "    - Todos los contenedores del proyecto" -ForegroundColor White
Write-Host "    - Todos los volumes (datos SQL, Redis, Kafka, Grafana)" -ForegroundColor White
Write-Host "    - Redes del proyecto" -ForegroundColor White
if (-not $KeepImages) {
    Write-Host "    - Imagenes construidas localmente" -ForegroundColor White
}
Write-Host ""

if (-not $Confirm) {
    $response = Read-Host "  Estas seguro? (s/N)"
    if ($response -notin @("s", "S", "si", "SI", "y", "Y", "yes")) {
        Write-Host "  Cancelado." -ForegroundColor DarkGray
        exit 0
    }
}

# ---- 1. Parar todo primero ----
Write-Host ""
Write-Host "[1/4] Parando la aplicacion..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot "stop_app.ps1") -Force 2>&1 | Out-Null
Write-Host "  OK" -ForegroundColor Green

# ---- 2. Docker Compose down con volumes ----
Write-Host "[2/4] Eliminando contenedores y volumes..." -ForegroundColor Yellow
$dockerCompose = Join-Path $root "docker\docker-compose.yml"
docker compose -f $dockerCompose down -v --remove-orphans 2>&1 | ForEach-Object {
    Write-Host "  $_" -ForegroundColor DarkGray
}
Write-Host "  OK" -ForegroundColor Green

# ---- 3. Limpiar imagenes locales (opcional) ----
if (-not $KeepImages) {
    Write-Host "[3/4] Eliminando imagenes locales..." -ForegroundColor Yellow
    $images = docker images --filter "reference=*economia*" -q 2>$null
    if ($images) {
        $images | ForEach-Object { docker rmi $_ -f 2>&1 | Out-Null }
        Write-Host "  Imagenes eliminadas" -ForegroundColor Green
    } else {
        Write-Host "  No habia imagenes locales" -ForegroundColor DarkGray
    }
} else {
    Write-Host "[3/4] Imagenes: conservadas (-KeepImages)" -ForegroundColor DarkGray
}

# ---- 4. Limpiar archivos temporales ----
Write-Host "[4/4] Limpiando archivos temporales..." -ForegroundColor Yellow
Remove-Item (Join-Path $root ".pid_backend") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $root ".pid_frontend") -ErrorAction SilentlyContinue

# Limpiar dangling docker resources
docker system prune -f 2>&1 | Out-Null
Write-Host "  OK" -ForegroundColor Green

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Reset completo. Entorno limpio." -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Para volver a arrancar:" -ForegroundColor White
Write-Host "    .\scripts\start_app.ps1" -ForegroundColor DarkYellow
Write-Host ""
