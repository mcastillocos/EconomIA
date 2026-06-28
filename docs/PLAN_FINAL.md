# EconomIA - Plan de Proyecto Final

## Visión Completa

Plataforma integral de análisis financiero con IA que combina:
- Ranking en tiempo real de fondos de inversión
- Análisis de empresas con datos fundamentales y técnicos
- 11 agentes IA especializados para distintas tareas financieras
- Observabilidad completa (métricas, trazas, logs)
- Despliegue en Azure con Docker y Kubernetes

---

## Estado Actual vs. Plan Inicial

### ✅ COMPLETADO

| Componente | Estado | Notas |
|-----------|--------|-------|
| Arquitectura hexagonal (.NET 10) | ✅ | Domain, Application, Infrastructure, API |
| Entidades y Value Objects (TDD) | ✅ | Fund, Company, Watchlist, UploadedDocument, FinancialMetric, Money, Percentage, ISIN, RiskLevel |
| EF Core + SQL Server | ✅ | Migrations, DbContext, Repositorios |
| MediatR (CQRS) | ✅ | Commands + Queries + Pipeline Behaviors |
| Redis cache | ✅ | RedisCacheService |
| Kafka messaging | ✅ | KafkaEventBus + KafkaConsumerService |
| SignalR real-time | ✅ | FundPriceHub + RankingHub |
| Background Services | ✅ | MarketDataPollingService + SignalRFundNotificationService |
| API REST Controllers | ✅ | Funds, Companies, Documents, Metrics, Reports, Watchlists, Chat |
| React Dashboard (Vite + TS) | ✅ | 17 vistas, sidebar colapsable, panel overview |
| Zustand store | ✅ | Estado global de fondos |
| Docker Compose | ✅ | API + Frontend + SQL + Redis + Kafka + Observabilidad |
| Despliegue Azure VM | ✅ | docker-compose en VM con SSH deploy scripts |
| Observabilidad (Fase 4) | ✅ | OpenTelemetry + Prometheus + Grafana + Tempo |
| Health checks | ✅ | /health/live, /health/ready |
| Métricas custom | ✅ | 8 contadores/histogramas en OpenTelemetryConfig |
| Grafana dashboards | ✅ | Pre-provisionado con 10 paneles |
| 11 Agentes IA | ✅ | Archivos individuales, código limpio |
| LLM multi-provider | ✅ | Azure OpenAI + Claude, con gestión de providers |
| 101 tests pasando | ✅ | Domain(52) + Application(4) + Infrastructure(45) |
| Manifiestos K8s | ✅ | Base + overlays (dev/prod) |

---

### 🔶 PARCIALMENTE IMPLEMENTADO

| Componente | Estado | Qué falta |
|-----------|--------|-----------|
| Tiempo real frontend | 🔶 | Hook `useSignalR` existe pero no se usa en todas las vistas. No hay reconexión automática |
| Kafka consumer integrado | 🔶 | KafkaConsumerService existe pero no está registrado como HostedService en prod |
| Charts | 🔶 | Recharts básico. Faltan: SectorDistribution, RiskVsReturn interactivo |
| Filtros avanzados | 🔶 | Filtro básico por nombre. Faltan: por riesgo, sector, rendimiento, fecha |
| Market Data Provider real | 🔶 | Solo SimulatedMarketDataProvider. No hay Yahoo/Morningstar/FMP real |
| Algoritmo de ranking | 🔶 | FundRankingService básico. Falta ponderar Sharpe, Sortino, MaxDrawdown |
| K8s despliegue real | 🔶 | Manifiestos existen. No hay deploy pipeline ni HPA activo |

---

### ❌ POR IMPLEMENTAR

| Componente | Prioridad | Complejidad | Descripción |
|-----------|-----------|-------------|-------------|
| **APIs de datos reales** | ALTA | Media | Integración con Yahoo Finance, Financial Modeling Prep, Alpha Vantage |
| **Vault / Secrets dinámicos** | ALTA | Media | HashiCorp Vault + External Secrets Operator |
| **CI/CD Pipeline** | ALTA | Baja | GitHub Actions: build → test → docker push → deploy |
| **Autenticación/Autorización** | ALTA | Media | JWT + Identity / Azure AD B2C |
| **Loki (logs centralizados)** | MEDIA | Baja | Serilog → Loki, ya está en docker-compose pero no configurado |
| **Polly (resiliencia)** | MEDIA | Baja | Retry, Circuit Breaker, Timeout en llamadas externas |
| **Comparador de fondos** | MEDIA | Baja | Vista side-by-side de 2-4 fondos |
| **Alertas Grafana** | MEDIA | Baja | Reglas de alerta por latencia, errores, caídas |
| **Load Testing (k6)** | MEDIA | Baja | Scripts de carga para validar rendimiento |
| **HPA (Auto-scaling)** | MEDIA | Baja | Configurar HPA en K8s para API pods |
| **Notificaciones push** | BAJA | Media | Alertas a usuario cuando cambia ranking de sus fondos |
| **Multi-tenant / perfiles** | BAJA | Alta | Perfiles de inversor, preferencias guardadas |
| **Mobile responsive** | BAJA | Media | Adaptar dashboard para móvil/tablet |
| **Export PDF/Excel** | BAJA | Baja | Exportar informes y análisis |
| **Webhooks** | BAJA | Baja | Notificaciones externas por eventos |
| **Rate Limiting avanzado** | BAJA | Baja | Middleware con sliding window por usuario |
| **Testcontainers** | BAJA | Media | Tests de integración con SQL Server/Redis reales en contenedor |

---

## Plan de Implementación (Proyecto Final)

### Fase 5: Datos Reales y Resiliencia
**Objetivo**: Conectar con APIs financieras reales y añadir robustez

| Tarea | Detalle |
|-------|---------|
| Yahoo Finance Adapter | Implementar `IMarketDataProvider` con yfinance/API |
| Financial Modeling Prep | Datos fundamentales (PER, ROE, Revenue) |
| Polly policies | Retry exponencial + Circuit Breaker en HttpClients |
| Rate limiter per-provider | No exceder límites de APIs gratuitas |
| Cache inteligente | TTL variable según tipo de dato (precio=5min, fundamental=24h) |
| Tests de integración | Validar parseo real de respuestas |

### Fase 6: Seguridad y Autenticación
**Objetivo**: Producción segura

| Tarea | Detalle |
|-------|---------|
| JWT Authentication | Tokens con refresh, almacenados en httpOnly cookies |
| Authorization policies | Roles: Free, Premium, Admin |
| HashiCorp Vault | Secretos dinámicos para DB, APIs |
| External Secrets Operator | Sync Vault → K8s |
| CORS configurado | Solo orígenes permitidos |
| Input sanitization | Validación en commands con FluentValidation |
| Audit logging | Log de acciones sensibles |

### Fase 7: Frontend Avanzado
**Objetivo**: Experiencia de usuario completa

| Tarea | Detalle |
|-------|---------|
| Comparador de fondos | Drag & drop, métricas side-by-side |
| Gráficos interactivos | Zoom, tooltips, selección de período |
| Filtros avanzados | Multi-filtro: riesgo, sector, rendimiento, región |
| SignalR reconexión | Backoff exponencial, indicador de estado |
| Tema claro/oscuro | Persistente en localStorage |
| PWA / offline | Service worker para datos básicos |
| Export PDF | Informes generados con puppeteer/wkhtmltopdf |
| Responsive | Breakpoints mobile/tablet |

### Fase 8: CI/CD y Producción
**Objetivo**: Pipeline completo, deploy automático

| Tarea | Detalle |
|-------|---------|
| GitHub Actions workflow | PR → build → test → lint → merge |
| Docker multi-stage | Builds optimizados con cache layers |
| Deploy automático | Push to main → deploy a Azure VM |
| Blue/Green deploy | Zero downtime con nginx upstream swap |
| K8s HPA | CPU/memory based auto-scaling |
| Load test (k6) | Scenarios: spike, soak, stress |
| Rollback automático | Si health check falla → revert |

### Fase 9: Observabilidad Completa
**Objetivo**: Visibilidad total del sistema

| Tarea | Detalle |
|-------|---------|
| Loki integration | Serilog → Loki sink configurado |
| Distributed tracing E2E | Trace desde React → API → SQL → Redis → Kafka |
| Alertas Grafana | Canales: email, Telegram, webhook |
| SLO/SLI definidos | Latency p99 < 200ms, Availability > 99.5% |
| Dashboard por agente IA | Tokens, latencia, éxito/fallo por agente |
| Cost tracking | Coste estimado por llamada LLM |

### Fase 10: Features Premium
**Objetivo**: Funcionalidades diferenciadoras

| Tarea | Detalle |
|-------|---------|
| Portfolio optimizer | Markowitz/Black-Litterman suggestions |
| Backtesting | Simular rendimiento histórico de cartera |
| News sentiment | NLP sobre noticias para score de sentimiento |
| Alertas personalizadas | "Avísame si Sharpe de X cae por debajo de 1.0" |
| Social features | Compartir carteras, rankings entre usuarios |
| API pública | REST API para terceros (rate limited) |
| Multi-idioma | i18n español/inglés/portugués |

---

## Mapa de Archivos de Test (Post-Renaming)

| Archivo | Agentes testeados |
|---------|------------------|
| `AnalysisAgentsTests.cs` | DailyNewsAgent, ScreenerAgent, PortfolioBriefingAgent |
| `DocumentAgentsTests.cs` | EarningsCallAgent, AnnualReportReaderAgent, DataValidationAgent, ComparisonAgent |
| `DataAgentsTests.cs` | FinancialDataExtractorAgent, RiskAgent |
| `AgentServiceTests.cs` | AgentService (orquestador) |
| `LlmServiceTests.cs` | LlmService (Azure OpenAI + Claude) |
| `OpenTelemetryConfigTests.cs` | Configuración de métricas |
| `CsvConnectorTests.cs` | Importación de datos CSV |

---

## Agentes IA (11 implementados)

| Agente | Función |
|--------|---------|
| CompanyAnalysisAgent | Análisis fundamental de una empresa |
| FundAnalysisAgent | Análisis detallado de un fondo |
| DailyNewsAgent | Briefing diario de noticias del portafolio |
| ScreenerAgent | Filtrado inteligente por criterios (ROE, PER, etc.) |
| PortfolioBriefingAgent | Resumen ejecutivo de la cartera |
| EarningsCallAgent | Extracción de datos de earnings calls |
| AnnualReportReaderAgent | Lectura y resumen de informes anuales |
| DataValidationAgent | Validación de calidad de datos financieros |
| ComparisonAgent | Comparación entre dos entidades |
| FinancialDataExtractorAgent | Extracción de datos de CSV/Excel/PDF |
| RiskAgent | Evaluación de riesgo por activo o cartera |

---

## Stack Tecnológico Final

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10, ASP.NET Core, EF Core 10 |
| Frontend | React 19, TypeScript 5.6, Vite 6, TailwindCSS 3.4 |
| Estado | Zustand 5, TanStack Query 5 |
| Real-time | SignalR |
| CQRS | MediatR |
| DB | SQL Server 2022 |
| Cache | Redis 7 |
| Messaging | Apache Kafka |
| IA | Azure OpenAI (GPT-5.5, GPT-5.4), Claude opus-4-7 |
| Observabilidad | OpenTelemetry + Prometheus + Grafana + Tempo |
| Deploy | Docker Compose en Azure VM |
| K8s | K3d / manifiestos Kustomize |
| Tests | xUnit, Moq, 101 tests |
