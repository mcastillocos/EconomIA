# EconomIA

Plataforma de análisis de fondos de inversión en **tiempo real**. Muestra un ranking dinámico de los mejores fondos del mercado europeo orientados a inversores medios, con datos generados por LLM (GPT-5.5, GPT-5.4, Claude) y actualización en streaming vía SSE.

## Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10, C#, Web API, SignalR, MediatR (CQRS), DDD, Arquitectura Hexagonal |
| Frontend | React 19, TypeScript, Vite 6.4, TailwindCSS 3.4, Zustand, Recharts, @tanstack/react-query |
| LLM Providers | Azure OpenAI (GPT-5.5, GPT-5.4), Anthropic Claude Opus 4.7 (round-robin + failover) |
| Rate Limiting | TokenBucketLimiter por TPM (patrón CobolForge) |
| Base de datos | SQL Server 2022 |
| Cache | Redis 7 + caché in-memory LLM con TTL configurable |
| Mensajería | Apache Kafka |
| Secretos | HashiCorp Vault |
| Observabilidad | OpenTelemetry → Grafana + Loki + Tempo + Prometheus |
| Orquestación | Docker Compose (dev) / K3d - Kubernetes (staging/prod) |
| Testing | xUnit (.NET) + Vitest 4.1 + @testing-library/react (Frontend, 53 tests) |

## Características principales

- **Streaming SSE en tiempo real**: los fondos llegan uno a uno al dashboard vía Server-Sent Events
- **Multi-LLM con round-robin**: distribuye carga entre GPT-5.5, GPT-5.4 y Claude con failover automático
- **Rate Limiter por TPM**: controla tokens/minuto con ventana deslizante al 90% del límite real (450K TPM)
- **Configuración dinámica**: todos los parámetros (workers, reintentos, TPM, batch size) editables desde la UI sin reiniciar
- **Estadísticas y costes**: tokens reales por proveedor con coste calculado en € (precios CobolForge)
- **Mis Fondos**: añade fondos propios y compáralos con el Top N (percentiles, radar, barras)
- **Dark mode**: tema oscuro por defecto con toggle

## Estructura del proyecto

```
EconomIA/
├── src/
│   ├── EconomIA.Domain/            # Entidades, Value Objects, Ports, Domain Events
│   ├── EconomIA.Application/       # CQRS Commands/Queries, MediatR Handlers, Behaviors
│   ├── EconomIA.Infrastructure/    # EF Core, Redis, Kafka, OpenTelemetry, APIs externas
│   └── EconomIA.API/               # Controllers REST, SignalR Hubs, Middleware
├── tests/
│   ├── EconomIA.Domain.Tests/      # Tests unitarios del dominio
│   ├── EconomIA.Application.Tests/ # Tests de handlers y behaviors
│   ├── EconomIA.Infrastructure.Tests/
│   └── EconomIA.API.Tests/
├── frontend/
│   ├── server/                     # Vite middleware: LLM proxy, rate limiter, SSE
│   │   ├── llmPlugin.ts            # Plugin principal: workers, caché, endpoints
│   │   ├── tokenBucketLimiter.ts   # Rate limiter por TPM (patrón CobolForge)
│   │   └── prompts/                # Templates LLM (system, batch, lookup)
│   ├── src/
│   │   ├── components/
│   │   │   ├── Views/              # GlobalView, MisFondosView, ConfigView, StatsView, LogsView
│   │   │   ├── Dashboard/          # FundTable, FundDetailModal, WhyTheBest
│   │   │   ├── Charts/             # PerformanceCharts (Recharts)
│   │   │   ├── Layout/             # Header, Sidebar
│   │   │   └── Filters/            # RiskFilter
│   │   ├── store/                  # Zustand: fundStore, logStore, myFundsStore, configStore
│   │   ├── hooks/                  # useStreamFunds (SSE), useTheme, useSignalR
│   │   └── test/                   # 8 archivos, 53 tests (Vitest + Testing Library)
│   └── vite.config.ts
├── docker/                         # Dockerfiles + docker-compose
├── sql/                            # Esquema SQL + seeds
├── k8s/                            # Manifiestos Kubernetes (K3d)
├── observability/                  # Config OpenTelemetry, Grafana, Prometheus
├── docs/                           # Wiki del proyecto
└── scripts/                        # Scripts de arranque/parada
```

## Requisitos previos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (con WSL2 en Windows)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- (Opcional) [K3d](https://k3d.io/) + kubectl para el modo Kubernetes

## Arranque rápido

### Opción 1: Script automático (recomendado)

```powershell
# Levanta todo (Docker + Backend + Frontend) en background
.\scripts\start_app.ps1

# Para todo
.\scripts\stop_app.ps1

# Borrar contenedores y datos para empezar de cero
.\scripts\clear_docker.ps1
```

### Opción 2: Manual paso a paso

```powershell
# 1. Levantar infraestructura (SQL Server, Redis, Kafka, Grafana, etc.)
cd docker
docker compose up -d
cd ..

# 2. Esperar ~30s a que SQL Server arranque, luego lanzar el backend
cd src
dotnet run --project EconomIA.API --urls http://localhost:5000

# 3. En otra terminal, lanzar el frontend
cd frontend
npm install
npm run dev
```

## URLs de acceso

| Servicio | URL | Credenciales |
|----------|-----|-------------|
| API (Swagger) | http://localhost:5000/swagger | — |
| Dashboard React | http://localhost:3000 | — |
| Grafana | http://localhost:3001 | admin / economia |
| Kafka UI | http://localhost:8080 | — |
| Vault | http://localhost:8200 | token: `economia-dev-token` |

## Endpoints principales

### Backend (.NET API)

```
GET  /api/funds/top/{count}       → Top N fondos (default 100)
GET  /api/funds/{id}              → Detalle de un fondo
GET  /api/funds/by-risk/{level}   → Fondos filtrados por nivel de riesgo
POST /api/funds/{id}/price        → Actualizar precio de un fondo
POST /api/funds/refresh           → Refrescar datos de mercado
GET  /health                      → Health check
```

### Frontend (Vite Middleware — LLM API)

```
GET  /api/llm/funds/stream        → SSE streaming de fondos (1 por worker)
GET  /api/llm/funds               → JSON con todos los fondos (batch)
GET  /api/llm/lookup?q=           → Buscar un fondo concreto por nombre/ISIN
GET  /api/llm/config              → Config actual + providers + caché + stats
POST /api/llm/config              → Actualizar config en caliente (JSON parcial)
POST /api/llm/reload              → Invalidar caché + resetear stats
```

### SignalR Hubs

| Hub | Ruta | Eventos |
|-----|------|---------|
| Precios | `/hubs/fund-prices` | `PriceUpdated`, `FundPriceUpdated` |
| Ranking | `/hubs/ranking` | `RankingChanged`, `TopFundsRefreshed` |

## Tests

```powershell
# Backend (.NET)
dotnet test

# Frontend (Vitest — 53 tests, 8 archivos)
cd frontend
npx vitest run

# Solo tests del dominio
dotnet test tests/EconomIA.Domain.Tests
```

## Vistas del Dashboard

| Vista | Descripción |
|-------|-------------|
| **Global** | Ranking Top N con tabla, métricas clave (WhyTheBest), gráficas |
| **Datos** | Tabla detallada de fondos con filtro de riesgo |
| **Gráficas** | Charts de rendimiento (Recharts) |
| **Mis Fondos** | Añade fondos propios, compáralos vs Top N (radar, barras, percentiles) |
| **Logs** | Logs en tiempo real con filtros y búsqueda |
| **Estadísticas** | Uso por proveedor LLM, tokens, costes en €, precios por modelo |
| **Config** | Proveedores, caché, rate limiter, workers, reintentos — todo editable |

## Variables de entorno

```env
AZURE_OPENAI_API_KEY0=...
AZURE_OPENAI_ENDPOINT0=https://oai-genai-mm-dev.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT0=gpt-5.5
AZURE_OPENAI_API_VERSION0=2024-08-01-preview
CLAUDE_API_KEY=...
CLAUDE_ENDPOINT=...
CLAUDE_MODEL1=claude-opus-4-7
```

## Wiki

Documentación detallada en la [Wiki de GitHub](https://github.com/mcastillocos/EconomIA/wiki):

- [Arquitectura LLM](https://github.com/mcastillocos/EconomIA/wiki/LLM-Architecture) — Providers, rate limiting, retry, SSE streaming
- [Configuración dinámica](https://github.com/mcastillocos/EconomIA/wiki/Dynamic-Configuration) — Parámetros editables en caliente
- [Costes y estadísticas](https://github.com/mcastillocos/EconomIA/wiki/Costs-and-Stats) — Cálculo de costes por proveedor en €
- [TokenBucketLimiter](https://github.com/mcastillocos/EconomIA/wiki/TokenBucketLimiter) — Rate limiter TPM (patrón CobolForge)
- [Vistas del Dashboard](https://github.com/mcastillocos/EconomIA/wiki/Dashboard-Views) — Las 7 vistas del frontend

## Arquitectura

```
┌────────────────────────────────────────────────────────────┐
│                     API (Driving Adapter)                   │
│          Controllers REST + SignalR Hubs                    │
├────────────────────────────────────────────────────────────┤
│                    APPLICATION                              │
│     Commands / Queries (MediatR) + Pipeline Behaviors      │
├────────────────────────────────────────────────────────────┤
│                      DOMAIN                                 │
│   Entities, Value Objects, Domain Events, Ports            │
├────────────────────────────────────────────────────────────┤
│               INFRASTRUCTURE (Driven Adapters)             │
│     SQL Server │ Redis │ Kafka │ APIs externas             │
└────────────────────────────────────────────────────────────┘
```

**Flujo real-time:**

```
APIs de mercado → BackgroundService (polling 5min) → MediatR Command
    → SQL Server (persistencia) + Redis (cache) + Kafka (evento)
    → SignalR Hub → Dashboard React (actualización instantánea)
```

## Despliegue con K3d (Kubernetes local)

```powershell
# Crear cluster
k3d cluster create economia --servers 1 --agents 3 `
  --port "80:80@loadbalancer" `
  --registry-create economia-registry:0.0.0.0:5050

# Construir y publicar imágenes
docker build -t economia-registry:5050/economia-api:latest -f docker/Dockerfile.api .
docker build -t economia-registry:5050/economia-frontend:latest -f docker/Dockerfile.frontend .
docker push economia-registry:5050/economia-api:latest
docker push economia-registry:5050/economia-frontend:latest

# Aplicar manifiestos
kubectl apply -k k8s/overlays/dev

# Acceder
# http://economia.localhost (frontend + api)
# http://grafana.economia.localhost (grafana)
```

## Scripts disponibles

| Script | Descripción | Flags |
|--------|-------------|-------|
| `start_app.ps1` | Levanta Docker + Back + Front en background | `-SkipDocker`, `-SkipFrontend`, `-SkipBackend` |
| `stop_app.ps1` | Para todo | `-KeepDocker` (mantiene contenedores) |
| `clear_docker.ps1` | Elimina contenedores, volumes y datos | `-KeepImages`, `-Confirm` (sin preguntar) |

## Licencia

MIT
