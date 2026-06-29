import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Métricas custom
const errorRate = new Rate('errors');
const fundLatency = new Trend('fund_request_duration');

// Escenarios configurables
export const options = {
  scenarios: {
    // Smoke test: verificar que funciona
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      startTime: '0s',
      tags: { scenario: 'smoke' },
    },
    // Load test: carga normal esperada
    load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 20 },    // ramp up
        { duration: '3m', target: 20 },    // steady state
        { duration: '1m', target: 0 },     // ramp down
      ],
      startTime: '30s',
      tags: { scenario: 'load' },
    },
    // Stress test: encontrar límites
    stress: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 50 },
        { duration: '2m', target: 50 },
        { duration: '1m', target: 100 },
        { duration: '2m', target: 100 },
        { duration: '1m', target: 0 },
      ],
      startTime: '5m30s',
      tags: { scenario: 'stress' },
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000', 'p(99)<5000'],  // P95 < 2s, P99 < 5s
    errors: ['rate<0.05'],                              // <5% errores
    fund_request_duration: ['p(95)<1500'],              // Fondos P95 < 1.5s
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  group('Health Checks', () => {
    const liveRes = http.get(`${BASE_URL}/health/live`);
    check(liveRes, { 'health/live 200': (r) => r.status === 200 });
    errorRate.add(liveRes.status !== 200);
  });

  group('Top Funds', () => {
    const start = Date.now();
    const res = http.get(`${BASE_URL}/api/funds/top/100`);
    fundLatency.add(Date.now() - start);

    check(res, {
      'top funds 200': (r) => r.status === 200,
      'top funds is array': (r) => {
        try { return Array.isArray(JSON.parse(r.body)); }
        catch { return false; }
      },
    });
    errorRate.add(res.status !== 200);
  });

  group('Fund Detail', () => {
    // Primero obtener un fund ID real
    const topRes = http.get(`${BASE_URL}/api/funds/top/1`);
    if (topRes.status === 200) {
      try {
        const funds = JSON.parse(topRes.body);
        if (funds.length > 0) {
          const res = http.get(`${BASE_URL}/api/funds/${funds[0].id}`);
          check(res, { 'fund detail 200': (r) => r.status === 200 });
          errorRate.add(res.status !== 200);
        }
      } catch { /* ignore parse errors */ }
    }
  });

  group('Filtered Funds', () => {
    const filters = [
      'filter?riskLevel=Medium&pageSize=20',
      'filter?search=fund&sortBy=Rating&sortDesc=true',
      'filter?maxExpenseRatio=1.5&minRating=3',
      'filter?category=Renta%20Variable&page=1&pageSize=10',
    ];

    const filter = filters[Math.floor(Math.random() * filters.length)];
    const start = Date.now();
    const res = http.get(`${BASE_URL}/api/funds/${filter}`);
    fundLatency.add(Date.now() - start);

    check(res, {
      'filtered funds 200': (r) => r.status === 200,
      'filtered has totalCount': (r) => {
        try { return JSON.parse(r.body).totalCount !== undefined; }
        catch { return false; }
      },
    });
    errorRate.add(res.status !== 200);
  });

  group('Metadata', () => {
    const catRes = http.get(`${BASE_URL}/api/funds/categories`);
    check(catRes, { 'categories 200': (r) => r.status === 200 });

    const compRes = http.get(`${BASE_URL}/api/funds/management-companies`);
    check(compRes, { 'companies 200': (r) => r.status === 200 });
  });

  sleep(1);
}

export function handleSummary(data) {
  return {
    stdout: textSummary(data, { indent: ' ', enableColors: true }),
    'load-test-results.json': JSON.stringify(data),
  };
}

// k6 built-in text summary
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.2/index.js';
