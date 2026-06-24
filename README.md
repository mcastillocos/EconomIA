# EconomIA

Plataforma de análisis de fondos de inversión en **tiempo real**. Muestra un ranking de los 100 mejores fondos del mercado orientados a inversores medios, con actualización automática de precios vía SignalR.

## Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10, C#, Web API, SignalR, MediatR (CQRS), DDD, Arquitectura Hexagonal |
| Frontend | React 19, TypeScript, Vite, TailwindCSS, Zustand, Recharts |
| Base de datos | SQL Server 2022 |
| Cache | Redis 7 |
| Mensajería | Apache Kafka |
| Secretos | HashiCorp Vault |
| Observabilidad | OpenTelemetry → Grafana + Loki + Tempo + Prometheus |
| Orquestación | Docker Compose (dev) / K3d - Kubernetes (staging/prod) |
| Testing | xUnit, FluentAssertions, NSubstitute, Bogus, Testcontainers |

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
├── frontend/                       # React dashboard
├── docker/                         # Dockerfiles + docker-compose
├── k8s/                            # Manifiestos Kubernetes (K3d)
├── observability/                  # Config OpenTelemetry, Grafana, Prometheus
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

```
GET  /api/funds/top/{count}       → Top N fondos (default 100)
GET  /api/funds/{id}              → Detalle de un fondo
GET  /api/funds/by-risk/{level}   → Fondos filtrados por nivel de riesgo
POST /api/funds/{id}/price        → Actualizar precio de un fondo
POST /api/funds/refresh           → Refrescar datos de mercado
GET  /health                      → Health check
```

### SignalR Hubs

| Hub | Ruta | Eventos |
|-----|------|---------|
| Precios | `/hubs/fund-prices` | `PriceUpdated`, `FundPriceUpdated` |
| Ranking | `/hubs/ranking` | `RankingChanged`, `TopFundsRefreshed` |

## Tests

```powershell
# Ejecutar todos los tests
dotnet test

# Solo tests del dominio
dotnet test tests/EconomIA.Domain.Tests

# Solo tests de application
dotnet test tests/EconomIA.Application.Tests
```

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
