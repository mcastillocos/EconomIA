# ============================================================
# EconomIA - START APP (non-blocking)
# Levanta Docker + Backend + Frontend en background
# ============================================================
param(
    [switch]$SkipDocker,
    [switch]$SkipFrontend,
    [switch]$SkipBackend
)

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot | Split-Path -Parent

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  EconomIA - Iniciando aplicacion completa" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

# ---- 1. Docker Compose ----
if (-not $SkipDocker) {
    Write-Host "[1/3] Levantando infraestructura Docker..." -ForegroundColor Yellow
    $dockerCompose = Join-Path $root "docker\docker-compose.yml"
    
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Host "  ERROR: Docker no encontrado en PATH" -ForegroundColor Red
        exit 1
    }

    docker compose -f $dockerCompose up -d 2>&1 | ForEach-Object {
        Write-Host "  $_" -ForegroundColor DarkGray
    }

    Write-Host "  Esperando a SQL Server (20s)..." -ForegroundColor DarkGray
    Start-Sleep -Seconds 20
    Write-Host "  Docker OK" -ForegroundColor Green
} else {
    Write-Host "[1/3] Docker SKIP" -ForegroundColor DarkGray
}

# ---- 2. Backend (.NET API) ----
if (-not $SkipBackend) {
    Write-Host "[2/3] Iniciando Backend .NET (background)..." -ForegroundColor Yellow
    $apiProject = Join-Path $root "src\EconomIA.API\EconomIA.API.csproj"
    
    $backendJob = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--project", $apiProject, "--urls", "http://localhost:5000" `
        -WorkingDirectory $root `
        -WindowStyle Hidden `
        -PassThru

    $backendJob.Id | Out-File (Join-Path $root ".pid_backend") -Force
    Write-Host "  Backend PID: $($backendJob.Id) -> http://localhost:5000" -ForegroundColor Green
} else {
    Write-Host "[2/3] Backend SKIP" -ForegroundColor DarkGray
}

# ---- 3. Frontend (Vite dev server) ----
if (-not $SkipFrontend) {
    Write-Host "[3/3] Iniciando Frontend React (background)..." -ForegroundColor Yellow
    $frontendDir = Join-Path $root "frontend"
    
    # Instalar deps si no existen
    if (-not (Test-Path (Join-Path $frontendDir "node_modules"))) {
        Write-Host "  Instalando dependencias npm..." -ForegroundColor DarkGray
        Push-Location $frontendDir
        npm install 2>&1 | Out-Null
        Pop-Location
    }

    $frontendJob = Start-Process -FilePath "npm" `
        -ArgumentList "run", "dev" `
        -WorkingDirectory $frontendDir `
        -WindowStyle Hidden `
        -PassThru

    $frontendJob.Id | Out-File (Join-Path $root ".pid_frontend") -Force
    Write-Host "  Frontend PID: $($frontendJob.Id) -> http://localhost:3000" -ForegroundColor Green
} else {
    Write-Host "[3/3] Frontend SKIP" -ForegroundColor DarkGray
}

# ---- Resumen ----
Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Todo levantado en background!" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  API Swagger:  http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  Frontend:     http://localhost:3000" -ForegroundColor White
Write-Host "  Grafana:      http://localhost:3001  (admin/economia)" -ForegroundColor White
Write-Host "  Kafka UI:     http://localhost:8080" -ForegroundColor White
Write-Host "  Vault:        http://localhost:8200  (token: economia-dev-token)" -ForegroundColor White
Write-Host "  SQL Server:   localhost,1433" -ForegroundColor White
Write-Host "  Redis:        localhost:6379" -ForegroundColor White
Write-Host ""
Write-Host "  Parar todo:   .\scripts\stop_app.ps1" -ForegroundColor DarkYellow
Write-Host "  Reset total:  .\scripts\clear_docker.ps1" -ForegroundColor DarkYellow
Write-Host ""
