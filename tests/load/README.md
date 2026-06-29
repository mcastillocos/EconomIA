# Load Testing con k6

## Requisitos
Instalar [k6](https://k6.io/docs/getting-started/installation/):
```bash
# Windows (chocolatey)
choco install k6

# macOS
brew install k6

# Docker
docker run --rm -i grafana/k6 run - <tests/load/k6-load-test.js
```

## Ejecutar tests

### Solo smoke test (rápido)
```bash
k6 run --env BASE_URL=http://localhost:5000 tests/load/k6-load-test.js --scenario smoke
```

### Test completo (smoke + load + stress)
```bash
k6 run --env BASE_URL=http://localhost:5000 tests/load/k6-load-test.js
```

### Contra producción
```bash
k6 run --env BASE_URL=http://20.203.185.54:5000 tests/load/k6-load-test.js --scenario smoke
```

## Thresholds
| Métrica | Umbral |
|---------|--------|
| HTTP P95 latencia | < 2s |
| HTTP P99 latencia | < 5s |
| Fondos P95 latencia | < 1.5s |
| Error rate | < 5% |
