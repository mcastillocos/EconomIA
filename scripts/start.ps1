# EconomIA - Script de inicio rápido (Windows)
Write-Host "🚀 EconomIA - Iniciando entorno de desarrollo..." -ForegroundColor Cyan
Write-Host ""

# Verificar dependencias
Write-Host "📋 Verificando dependencias..." -ForegroundColor Yellow

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { Write-Error "Docker no instalado"; exit 1 }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Write-Error ".NET SDK no instalado"; exit 1 }
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { Write-Error "Node.js no instalado"; exit 1 }

Write-Host "✅ Docker: $(docker --version)" -ForegroundColor Green
Write-Host "✅ .NET: $(dotnet --version)" -ForegroundColor Green
Write-Host "✅ Node: $(node --version)" -ForegroundColor Green
Write-Host ""

# Levantar infraestructura
Write-Host "🐳 Levantando infraestructura con Docker Compose..." -ForegroundColor Yellow
Push-Location docker
docker compose up -d sqlserver redis kafka zookeeper vault otel-collector prometheus loki tempo grafana kafka-ui
Pop-Location

Write-Host ""
Write-Host "⏳ Esperando a que SQL Server esté listo (30s)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Frontend
Write-Host "📦 Instalando dependencias del frontend..." -ForegroundColor Yellow
Push-Location frontend
npm install
Pop-Location

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  🌐 Accesos:" -ForegroundColor Cyan
Write-Host "    API:       http://localhost:5000/swagger" -ForegroundColor White
Write-Host "    Frontend:  http://localhost:3000" -ForegroundColor White
Write-Host "    Grafana:   http://localhost:3001 (admin/economia)" -ForegroundColor White
Write-Host "    Kafka UI:  http://localhost:8080" -ForegroundColor White
Write-Host "    Vault:     http://localhost:8200 (token: economia-dev-token)" -ForegroundColor White
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para iniciar desarrollo:" -ForegroundColor Yellow
Write-Host "  Backend:  cd src && dotnet run --project EconomIA.API" -ForegroundColor White
Write-Host "  Frontend: cd frontend && npm run dev" -ForegroundColor White
