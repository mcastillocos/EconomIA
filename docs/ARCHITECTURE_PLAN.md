# EconomIA - Plan de Arquitectura Completo

## 🎯 Visión del Proyecto

Plataforma que muestra en **tiempo real** los 100 mejores fondos de inversión del mercado para inversores medios, con un dashboard React y un backend .NET 10 basado en arquitectura hexagonal.

---

## 📐 Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         KUBERNETES (K3d local)                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────┐    ┌──────────────────┐    ┌───────────────────┐     │
│  │  React App   │───▶│  .NET 10 API     │───▶│   SQL Server      │     │
│  │  (Dashboard) │    │  (SignalR + REST) │    │   (Persistencia)  │     │
│  └──────────────┘    └────────┬─────────┘    └───────────────────┘     │
│                               │                                         │
│                    ┌──────────┼──────────┐                              │
│                    ▼          ▼          ▼                              │
│              ┌──────────┐ ┌───────┐ ┌────────┐                         │
│              │  Redis   │ │ Kafka │ │ Vault  │                         │
│              │  (Cache) │ │(Msgs) │ │(Secrets│                         │
│              └──────────┘ └───────┘ └────────┘                         │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              Observabilidad (OpenTelemetry + Grafana)            │   │
│  │  ┌──────────┐  ┌────────────┐  ┌───────┐  ┌───────────────┐   │   │
│  │  │  Tempo   │  │Prometheus  │  │ Loki  │  │   Grafana     │   │   │
│  │  │ (Traces) │  │ (Metrics)  │  │(Logs) │  │ (Dashboards)  │   │   │
│  │  └──────────┘  └────────────┘  └───────┘  └───────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🏗️ Arquitectura Hexagonal (Ports & Adapters) + DDD

```
┌─────────────────────────────────────────────────────────────────┐
│                        INFRASTRUCTURE                            │
│  (Adapters: SQL Server, Redis, Kafka, APIs externas)            │
├─────────────────────────────────────────────────────────────────┤
│                        APPLICATION                               │
│  (Use Cases, MediatR Handlers, CQRS Commands/Queries)           │
├─────────────────────────────────────────────────────────────────┤
│                          DOMAIN                                  │
│  (Entities, Value Objects, Aggregates, Domain Events, Ports)    │
└─────────────────────────────────────────────────────────────────┘
```

### Capas del Backend (.NET 10)

```
src/
├── EconomIA.Domain/                    # 🟡 NÚCLEO - Sin dependencias externas
│   ├── Entities/
│   │   ├── Fund.cs                     # Aggregate Root
│   │   ├── FundPerformance.cs
│   │   ├── MarketSector.cs
│   │   └── InvestorProfile.cs
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   ├── Percentage.cs
│   │   ├── FundRating.cs
│   │   ├── ISIN.cs
│   │   └── RiskLevel.cs
│   ├── Aggregates/
│   │   └── FundAggregate.cs
│   ├── Events/
│   │   ├── FundPriceUpdatedEvent.cs
│   │   ├── FundRankingChangedEvent.cs
│   │   └── NewFundDiscoveredEvent.cs
│   ├── Ports/
│   │   ├── IFundRepository.cs          # Port de salida (driven)
│   │   ├── IMarketDataProvider.cs      # Port de salida (driven)
│   │   ├── ICacheService.cs            # Port de salida (driven)
│   │   └── IEventBus.cs               # Port de salida (driven)
│   ├── Services/
│   │   ├── FundRankingService.cs       # Domain Service
│   │   └── RiskCalculatorService.cs
│   └── Exceptions/
│       ├── DomainException.cs
│       └── InvalidFundException.cs
│
├── EconomIA.Application/               # 🔵 CASOS DE USO
│   ├── Commands/
│   │   ├── UpdateFundPrice/
│   │   │   ├── UpdateFundPriceCommand.cs
│   │   │   ├── UpdateFundPriceHandler.cs
│   │   │   └── UpdateFundPriceValidator.cs
│   │   └── RefreshMarketData/
│   │       ├── RefreshMarketDataCommand.cs
│   │       └── RefreshMarketDataHandler.cs
│   ├── Queries/
│   │   ├── GetTopFunds/
│   │   │   ├── GetTopFundsQuery.cs
│   │   │   ├── GetTopFundsHandler.cs
│   │   │   └── GetTopFundsResponse.cs
│   │   ├── GetFundDetail/
│   │   │   ├── GetFundDetailQuery.cs
│   │   │   └── GetFundDetailHandler.cs
│   │   └── GetFundsByRisk/
│   │       ├── GetFundsByRiskQuery.cs
│   │       └── GetFundsByRiskHandler.cs
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs       # MediatR Pipeline
│   │   ├── LoggingBehavior.cs
│   │   └── CachingBehavior.cs
│   ├── DTOs/
│   │   ├── FundDto.cs
│   │   └── FundPerformanceDto.cs
│   ├── Mappings/
│   │   └── FundMappingProfile.cs
│   └── Interfaces/
│       ├── IFundNotificationService.cs # Port de entrada (driving)
│       └── IMarketDataScheduler.cs
│
├── EconomIA.Infrastructure/            # 🟢 ADAPTADORES DE SALIDA
│   ├── Persistence/
│   │   ├── EconomIADbContext.cs
│   │   ├── Configurations/
│   │   │   ├── FundConfiguration.cs
│   │   │   └── FundPerformanceConfiguration.cs
│   │   ├── Repositories/
│   │   │   └── FundRepository.cs       # Implementa IFundRepository
│   │   └── Migrations/
│   ├── Cache/
│   │   └── RedisCacheService.cs        # Implementa ICacheService
│   ├── Messaging/
│   │   ├── KafkaEventBus.cs            # Implementa IEventBus
│   │   ├── KafkaConsumerService.cs
│   │   └── KafkaProducerService.cs
│   ├── ExternalServices/
│   │   ├── MorningstarDataProvider.cs  # Implementa IMarketDataProvider
│   │   ├── YahooFinanceProvider.cs
│   │   └── FundDataAggregator.cs
│   └── Telemetry/
│       ├── OpenTelemetryConfig.cs
│       └── MetricsCollector.cs
│
├── EconomIA.API/                       # 🔴 ADAPTADOR DE ENTRADA (Driving)
│   ├── Controllers/
│   │   ├── FundsController.cs
│   │   └── MarketController.cs
│   ├── Hubs/
│   │   ├── FundPriceHub.cs            # SignalR Hub
│   │   └── RankingHub.cs
│   ├── Middleware/
│   │   ├── ExceptionMiddleware.cs
│   │   ├── CorrelationIdMiddleware.cs
│   │   └── RateLimitingMiddleware.cs
│   ├── BackgroundServices/
│   │   ├── MarketDataPollingService.cs
│   │   └── KafkaConsumerHostedService.cs
│   ├── Configuration/
│   │   └── DependencyInjection.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
│
tests/
├── EconomIA.Domain.Tests/              # 🧪 Unit Tests (TDD)
│   ├── Entities/
│   │   └── FundTests.cs
│   ├── ValueObjects/
│   │   ├── MoneyTests.cs
│   │   └── ISINTests.cs
│   └── Services/
│       └── FundRankingServiceTests.cs
├── EconomIA.Application.Tests/
│   ├── Handlers/
│   │   ├── GetTopFundsHandlerTests.cs
│   │   └── UpdateFundPriceHandlerTests.cs
│   └── Behaviors/
│       └── ValidationBehaviorTests.cs
├── EconomIA.Infrastructure.Tests/       # Integration Tests
│   ├── Repositories/
│   │   └── FundRepositoryTests.cs
│   └── Cache/
│       └── RedisCacheServiceTests.cs
└── EconomIA.API.Tests/                  # E2E / Integration
    ├── Controllers/
    │   └── FundsControllerTests.cs
    └── Hubs/
        └── FundPriceHubTests.cs
```

---

## 🖥️ Frontend React (Dashboard)

```
frontend/
├── src/
│   ├── components/
│   │   ├── Dashboard/
│   │   │   ├── FundTable.tsx           # Tabla top 100 fondos
│   │   │   ├── FundCard.tsx
│   │   │   ├── RealTimePrice.tsx       # Componente SignalR
│   │   │   └── RiskIndicator.tsx
│   │   ├── Charts/
│   │   │   ├── PerformanceChart.tsx
│   │   │   ├── SectorDistribution.tsx
│   │   │   └── RiskVsReturn.tsx
│   │   ├── Filters/
│   │   │   ├── RiskFilter.tsx
│   │   │   ├── SectorFilter.tsx
│   │   │   └── TimeRangeSelector.tsx
│   │   └── Layout/
│   │       ├── Header.tsx
│   │       ├── Sidebar.tsx
│   │       └── Footer.tsx
│   ├── hooks/
│   │   ├── useSignalR.ts              # Hook conexión SignalR
│   │   ├── useFunds.ts
│   │   └── useRealTimeUpdates.ts
│   ├── services/
│   │   ├── api.ts                     # Axios/Fetch wrapper
│   │   └── signalRService.ts
│   ├── store/                         # Zustand / Redux Toolkit
│   │   ├── fundStore.ts
│   │   └── filterStore.ts
│   ├── types/
│   │   └── fund.ts
│   ├── App.tsx
│   └── main.tsx
├── package.json
├── vite.config.ts
├── Dockerfile
└── nginx.conf
```

---

## 🐳 Docker & Kubernetes

### Docker Compose (Desarrollo Local)

```yaml
# Servicios:
# - economia-api        (.NET 10 API)
# - economia-frontend   (React + Nginx)
# - sqlserver           (SQL Server 2022)
# - redis               (Redis 7)
# - kafka + zookeeper   (Apache Kafka)
# - vault               (HashiCorp Vault)
# - grafana             (Grafana)
# - prometheus          (Prometheus)
# - loki                (Loki - logs)
# - tempo               (Tempo - traces)
# - otel-collector      (OpenTelemetry Collector)
```

### Kubernetes Local (K3d) - Recomendado

**¿Por qué K3d sobre Minikube/Kind?**
- Más ligero y rápido que Minikube
- Soporta multi-nodo
- Registry local integrado
- Más cercano a un cluster real (K3s = Kubernetes certificado)

```
k8s/
├── base/
│   ├── namespace.yaml
│   ├── api/
│   │   ├── deployment.yaml
│   │   ├── service.yaml
│   │   └── hpa.yaml                  # Auto-scaling
│   ├── frontend/
│   │   ├── deployment.yaml
│   │   └── service.yaml
│   ├── sqlserver/
│   │   ├── statefulset.yaml
│   │   ├── service.yaml
│   │   └── pvc.yaml
│   ├── redis/
│   │   ├── deployment.yaml
│   │   └── service.yaml
│   ├── kafka/
│   │   ├── statefulset.yaml
│   │   └── service.yaml
│   ├── observability/
│   │   ├── grafana/
│   │   ├── prometheus/
│   │   ├── loki/
│   │   ├── tempo/
│   │   └── otel-collector/
│   └── secrets/
│       └── external-secrets.yaml
├── overlays/
│   ├── dev/
│   │   └── kustomization.yaml
│   └── prod/
│       └── kustomization.yaml
└── helmfile.yaml
```

---

## 🔐 Gestión de Secretos

### Estrategia por capas:

| Nivel | Herramienta | Uso |
|-------|------------|-----|
| Desarrollo | `.env` + Docker Secrets | Variables locales |
| Staging/Prod | HashiCorp Vault | Secretos dinámicos |
| K8s | External Secrets Operator | Sync Vault → K8s Secrets |

### Flujo:
```
Vault (source of truth)
    │
    ▼
External Secrets Operator (K8s)
    │
    ▼
K8s Secrets (montados como volúmenes o env vars)
    │
    ▼
Pod (.NET API lee de env / montaje)
```

---

## 📊 Observabilidad (OpenTelemetry + Grafana Stack)

### Stack LGTM (Loki, Grafana, Tempo, Mimir/Prometheus):

```
┌──────────────┐
│  .NET API    │──── OpenTelemetry SDK ────┐
└──────────────┘                           │
                                           ▼
                                 ┌──────────────────┐
                                 │ OTel Collector    │
                                 └────────┬─────────┘
                                          │
                          ┌───────────────┼───────────────┐
                          ▼               ▼               ▼
                   ┌───────────┐   ┌───────────┐   ┌───────────┐
                   │   Tempo   │   │Prometheus │   │   Loki    │
                   │  (Traces) │   │ (Metrics) │   │  (Logs)   │
                   └─────┬─────┘   └─────┬─────┘   └─────┬─────┘
                         │               │               │
                         └───────────────┼───────────────┘
                                         ▼
                                  ┌─────────────┐
                                  │   GRAFANA   │
                                  │ (Dashboards)│
                                  └─────────────┘
```

### Métricas clave a monitorizar:
- **Latencia** de endpoints REST y SignalR
- **Throughput** de mensajes Kafka
- **Cache hit/miss ratio** en Redis
- **Fondos actualizados/segundo**
- **Errores** por tipo y endpoint
- **Tiempo de query** SQL Server

---

## 🔄 Flujo de Datos en Tiempo Real

```
[APIs Externas: Yahoo/Morningstar]
          │
          ▼
[BackgroundService: MarketDataPollingService]  ← Cada 30s-5min
          │
          ▼
[MediatR Command: UpdateFundPriceCommand]
          │
          ├──▶ [SQL Server] (persistencia)
          ├──▶ [Redis] (cache invalidation + update)
          ├──▶ [Kafka] (evento: FundPriceUpdated)
          │
          ▼
[KafkaConsumerHostedService]
          │
          ▼
[SignalR Hub: FundPriceHub]
          │
          ▼
[React Dashboard] ← Actualización en tiempo real
```

---

## 🧪 Estrategia TDD

### Red → Green → Refactor

1. **Domain Tests** (unitarios puros, sin mocks):
   - Validaciones de Value Objects (ISIN, Money, etc.)
   - Lógica de negocio del ranking
   - Cálculo de riesgo

2. **Application Tests** (con mocks de puertos):
   - Handlers de MediatR
   - Validaciones de FluentValidation
   - Behaviors del pipeline

3. **Infrastructure Tests** (integration con Testcontainers):
   - Repositorios contra SQL Server real (contenedor)
   - Redis cache real (contenedor)
   - Kafka producer/consumer (contenedor)

4. **API Tests** (WebApplicationFactory):
   - Endpoints REST
   - SignalR Hub connections
   - Middleware behavior

### Frameworks de Testing:
- **xUnit** - Framework principal
- **FluentAssertions** - Aserciones legibles
- **NSubstitute** - Mocking
- **Testcontainers** - Tests de integración con Docker
- **Bogus** - Generación de datos fake
- **Verify** - Snapshot testing

---

## 📦 Paquetes NuGet Principales

| Paquete | Propósito |
|---------|-----------|
| MediatR | CQRS + Pipeline behaviors |
| FluentValidation | Validación de commands/queries |
| Entity Framework Core 10 | ORM + Migrations |
| Microsoft.AspNetCore.SignalR | Comunicación real-time |
| StackExchange.Redis | Cliente Redis |
| Confluent.Kafka | Cliente Kafka |
| OpenTelemetry.* | Instrumentación |
| Mapster / AutoMapper | Mapeo DTO ↔ Entity |
| Polly | Resilience (retry, circuit breaker) |
| VaultSharp | Cliente HashiCorp Vault |
| Serilog | Structured logging |
| Swashbuckle | Swagger/OpenAPI |

---

## 🚀 Plan de Implementación por Fases

### Fase 1: Fundamentos (Semana 1-2)
- [ ] Setup solución .NET 10 con estructura hexagonal
- [ ] Definir entidades de dominio y Value Objects (TDD)
- [ ] Configurar EF Core + SQL Server (Docker)
- [ ] CRUD básico de fondos con MediatR
- [ ] Docker Compose inicial (API + SQL Server)
- [ ] Tests unitarios del dominio

### Fase 2: Tiempo Real (Semana 3-4)
- [ ] Integrar SignalR Hub para precios
- [ ] Background Service para polling de datos
- [ ] Integrar Kafka (producer/consumer)
- [ ] Integrar Redis cache
- [ ] React dashboard básico con tabla de fondos
- [ ] Conexión SignalR desde React

### Fase 3: Inteligencia (Semana 5-6)
- [ ] Algoritmo de ranking de fondos
- [ ] Filtros por riesgo, sector, rendimiento
- [ ] Gráficos de rendimiento (Recharts/D3)
- [ ] Comparador de fondos
- [ ] Tests de integración con Testcontainers

### Fase 4: Observabilidad (Semana 7)
- [ ] OpenTelemetry SDK en .NET
- [ ] OTel Collector deployment
- [ ] Grafana dashboards (métricas, logs, traces)
- [ ] Alertas configuradas
- [ ] Health checks

### Fase 5: Kubernetes & Producción (Semana 8-9)
- [ ] K3d cluster local
- [ ] Manifiestos K8s (deployments, services, HPA)
- [ ] HashiCorp Vault + External Secrets
- [ ] Ingress controller (Traefik incluido en K3d)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Load testing con k6

---

## 🛠️ Comandos de Inicio Rápido

```bash
# 1. Crear cluster K3d
k3d cluster create economia --servers 1 --agents 3 \
  --port "80:80@loadbalancer" --port "443:443@loadbalancer" \
  --registry-create economia-registry:0.0.0.0:5050

# 2. Levantar con Docker Compose (dev)
docker-compose -f docker-compose.dev.yml up -d

# 3. Crear solución .NET
dotnet new sln -n EconomIA
dotnet new webapi -n EconomIA.API
dotnet new classlib -n EconomIA.Domain
dotnet new classlib -n EconomIA.Application
dotnet new classlib -n EconomIA.Infrastructure
dotnet new xunit -n EconomIA.Domain.Tests
dotnet new xunit -n EconomIA.Application.Tests
dotnet new xunit -n EconomIA.Infrastructure.Tests
dotnet new xunit -n EconomIA.API.Tests

# 4. Frontend React
npm create vite@latest frontend -- --template react-ts

# 5. Vault dev mode
vault server -dev

# 6. Accesos
# API:       http://localhost:5000
# Frontend:  http://localhost:3000
# Grafana:   http://localhost:3001
# Vault UI:  http://localhost:8200
# Kafka UI:  http://localhost:8080
```

---

## 📋 Decisiones Técnicas

| Decisión | Elegido | Alternativa | Razón |
|----------|---------|-------------|-------|
| Kubernetes local | **K3d** | Minikube, Kind | Más ligero, registry integrado, multi-nodo |
| ORM | **EF Core 10** | Dapper | Migrations, LINQ, DDD-friendly |
| Real-time | **SignalR** | WebSockets puro | Integración nativa .NET, fallback automático |
| CQRS | **MediatR** | Custom mediator | Maduro, pipeline behaviors, comunidad |
| Cache | **Redis** | MemoryCache | Distribuido, pub/sub para invalidación |
| Messaging | **Kafka** | RabbitMQ | Alto throughput, replay, particiones |
| Secrets | **Vault** | K8s Secrets solo | Rotación automática, audit, dynamic secrets |
| Logs | **Serilog → Loki** | ELK | Más ligero, integración Grafana nativa |
| Frontend state | **Zustand** | Redux Toolkit | Menos boilerplate, más simple |
| Charts | **Recharts** | D3, Chart.js | React-native, declarativo |
| Testing | **xUnit + Testcontainers** | NUnit | Mejor paralelismo, containers reales |

---

## 🌐 APIs de Datos de Fondos (Gratuitas/Freemium)

| Proveedor | Datos | Límite Free |
|-----------|-------|-------------|
| Yahoo Finance (yfinance) | Precios, históricos | Sin límite oficial |
| Financial Modeling Prep | Fundamentales, fondos | 250 req/día |
| Alpha Vantage | Precios tiempo real | 5 req/min, 500/día |
| Morningstar (scraping) | Ratings, categorías | Con precaución |
| CNMV (España) | Fondos españoles | API pública |
| Investing.com (scraping) | Rankings | Con precaución |

---

## 🔑 Resumen de Puertos Expuestos

| Servicio | Puerto | Protocolo |
|----------|--------|-----------|
| .NET API | 5000/5001 | HTTP/HTTPS |
| React Frontend | 3000 | HTTP |
| SQL Server | 1433 | TCP |
| Redis | 6379 | TCP |
| Kafka | 9092 | TCP |
| Zookeeper | 2181 | TCP |
| Vault | 8200 | HTTP |
| Grafana | 3001 | HTTP |
| Prometheus | 9090 | HTTP |
| Loki | 3100 | HTTP |
| Tempo | 4317/4318 | gRPC/HTTP |
| OTel Collector | 4317 | gRPC |
| Kafka UI | 8080 | HTTP |

---

## Próximos Pasos

Una vez aprobado el plan, procederemos a:
1. Generar la estructura de carpetas completa
2. Crear los Dockerfiles y docker-compose
3. Implementar el dominio con TDD
4. Levantar la infraestructura
