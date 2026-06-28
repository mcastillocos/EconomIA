# ============================================================
# EconomIA - START LOCAL usando infraestructura Azure remota
# No necesita Docker local. Conecta a SQL Server y Redis de la VM.
# ============================================================

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot | Split-Path -Parent

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  EconomIA - Local con BD Azure (20.203.185.54)" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

# ---- 1. Ejecutar SQL script pendiente en Azure ----
Write-Host "[1/3] Ejecutando scripts SQL en Azure..." -ForegroundColor Yellow
& "$PSScriptRoot\execute_scripts.ps1" -Server "20.203.185.54,1433"
Write-Host ""

# ---- 2. Backend (.NET API) contra Azure ----
Write-Host "[2/3] Iniciando Backend .NET (conectado a Azure)..." -ForegroundColor Yellow
$apiProject = Join-Path $root "src\EconomIA.API\EconomIA.API.csproj"

$env:ASPNETCORE_ENVIRONMENT = "RemoteAzure"

$backendJob = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--project", $apiProject, "--urls", "http://localhost:5000" `
    -WorkingDirectory $root `
    -WindowStyle Hidden `
    -PassThru

$backendJob.Id | Out-File (Join-Path $root ".pid_backend") -Force
Write-Host "  Backend PID: $($backendJob.Id) -> http://localhost:5000" -ForegroundColor Green
Write-Host "  Conectado a: 20.203.185.54 (SQL + Redis)" -ForegroundColor DarkGray
Write-Host ""

# ---- 3. Frontend (Vite dev server) ----
Write-Host "[3/3] Iniciando Frontend React..." -ForegroundColor Yellow
$frontendDir = Join-Path $root "frontend"

if (-not (Test-Path (Join-Path $frontendDir "node_modules"))) {
    Write-Host "  Instalando dependencias npm..." -ForegroundColor DarkGray
    Push-Location $frontendDir
    npm install 2>&1 | Out-Null
    Pop-Location
}

$frontendJob = Start-Process -FilePath "cmd.exe" `
    -ArgumentList "/c", "npm run dev" `
    -WorkingDirectory $frontendDir `
    -WindowStyle Hidden `
    -PassThru

$frontendJob.Id | Out-File (Join-Path $root ".pid_frontend") -Force
Write-Host "  Frontend PID: $($frontendJob.Id) -> http://localhost:3000" -ForegroundColor Green
Write-Host ""

# ---- Resumen ----
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Listo! Sin Docker local." -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Frontend:     http://localhost:3000" -ForegroundColor White
Write-Host "  API Swagger:  http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  BD remota:    20.203.185.54:1433" -ForegroundColor DarkGray
Write-Host "  Redis remoto: 20.203.185.54:6379" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  Parar todo:   .\scripts\stop_app.ps1" -ForegroundColor DarkYellow
Write-Host ""
