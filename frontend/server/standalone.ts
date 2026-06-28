/**
 * Servidor standalone Node.js para los endpoints /api/llm/*
 * Replica la funcionalidad del llmPlugin de Vite para entornos de producción.
 */
import express from 'express';
import { readFileSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { TokenBucketLimiter, estimateTokens } from './tokenBucketLimiter';

const __dirname = dirname(fileURLToPath(import.meta.url));
const PROMPTS_DIR = resolve(__dirname, 'prompts');

const app = express();
app.use(express.json());

// ── Prompt loader ──────────────────────────────────────────────────────

function loadPrompt(name: string, vars: Record<string, string | number> = {}): string {
  const raw = readFileSync(resolve(PROMPTS_DIR, `${name}.md`), 'utf-8').trim();
  return Object.entries(vars).reduce(
    (text, [key, val]) => text.replaceAll(`{{${key}}}`, String(val)),
    raw,
  );
}

const SYSTEM_PROMPT = loadPrompt('system');

const CATEGORIES = [
  ['Renta Variable Global', 'Indexado S&P500'],
  ['Tecnología', 'ESG'],
  ['Emergentes', 'Asia-Pacífico'],
  ['Europa', 'Renta Fija'],
  ['Mixto', 'Materias Primas'],
  ['Renta Variable Global', 'Tecnología'],
  ['Indexado S&P500', 'ESG'],
  ['Europa', 'Emergentes'],
  ['Renta Fija', 'Mixto'],
  ['Asia-Pacífico', 'Materias Primas'],
];

function batchPrompt(batchIndex: number, batchSize: number): string {
  const cat = CATEGORIES[batchIndex % CATEGORIES.length].join(', ');
  const rankStart = batchIndex * batchSize + 1;
  const rankEnd = (batchIndex + 1) * batchSize;
  return loadPrompt('batch', {
    BATCH_SIZE: batchSize,
    CATEGORIES: cat,
    RANK_START: rankStart,
    RANK_END: rankEnd,
  });
}

function lookupPrompt(query: string): string {
  return loadPrompt('lookup', { QUERY: query });
}

// ── LLM Callers ────────────────────────────────────────────────────────

interface LLMUsage { promptTokens: number; completionTokens: number; totalTokens: number; }
interface LLMResult { content: string; usage: LLMUsage; }

async function callAzureOpenAI(
  systemPrompt: string, userPrompt: string, maxTokens: number, deploymentOverride?: string,
): Promise<LLMResult> {
  const apiKey = process.env.AZURE_OPENAI_API_KEY0;
  const endpoint = (process.env.AZURE_OPENAI_ENDPOINT0 || '').replace(/\/$/, '');
  const deployment = deploymentOverride || process.env.AZURE_OPENAI_DEPLOYMENT0;
  const apiVersion = process.env.AZURE_OPENAI_API_VERSION0;

  if (!apiKey || !endpoint || !deployment || !apiVersion) throw new Error(`Azure OpenAI config incompleta (${deployment})`);

  const url = `${endpoint}/openai/deployments/${deployment}/chat/completions?api-version=${apiVersion}`;
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), 90_000);

  try {
    const resp = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'api-key': apiKey },
      body: JSON.stringify({
        messages: [
          { role: 'system', content: systemPrompt },
          { role: 'user', content: userPrompt },
        ],
        max_completion_tokens: maxTokens,
      }),
      signal: controller.signal,
    });
    if (!resp.ok) throw new Error(`HTTP ${resp.status}: ${(await resp.text()).substring(0, 200)}`);
    const data = await resp.json() as any;
    return {
      content: data.choices[0].message.content,
      usage: {
        promptTokens: data.usage?.prompt_tokens ?? 0,
        completionTokens: data.usage?.completion_tokens ?? 0,
        totalTokens: data.usage?.total_tokens ?? 0,
      },
    };
  } finally { clearTimeout(timer); }
}

async function callClaude(systemPrompt: string, userPrompt: string, maxTokens = 4000): Promise<LLMResult> {
  const apiKey = process.env.CLAUDE_API_KEY;
  const endpoint = process.env.CLAUDE_ENDPOINT;
  const model = process.env.CLAUDE_MODEL1 || 'claude-opus-4-7';

  if (!apiKey || !endpoint) throw new Error('Claude config incompleta');

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), 90_000);

  try {
    const resp = await fetch(endpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': apiKey,
        'anthropic-version': '2023-06-01',
      },
      body: JSON.stringify({ model, max_tokens: maxTokens, system: systemPrompt, messages: [{ role: 'user', content: userPrompt }] }),
      signal: controller.signal,
    });
    if (!resp.ok) throw new Error(`HTTP ${resp.status}: ${(await resp.text()).substring(0, 200)}`);
    const data = await resp.json() as any;
    return {
      content: data.content[0].text,
      usage: {
        promptTokens: data.usage?.input_tokens ?? 0,
        completionTokens: data.usage?.output_tokens ?? 0,
        totalTokens: (data.usage?.input_tokens ?? 0) + (data.usage?.output_tokens ?? 0),
      },
    };
  } finally { clearTimeout(timer); }
}

// ── Provider chain ─────────────────────────────────────────────────────

interface LLMProvider {
  name: string;
  call: (sys: string, usr: string, max: number) => Promise<LLMResult>;
  available: () => boolean;
}

const LLM_PROVIDERS: LLMProvider[] = [
  {
    name: 'GPT-5.5',
    call: (sys, usr, max) => callAzureOpenAI(sys, usr, max),
    available: () => !!(process.env.AZURE_OPENAI_API_KEY0 && process.env.AZURE_OPENAI_ENDPOINT0 && process.env.AZURE_OPENAI_DEPLOYMENT0),
  },
  {
    name: 'GPT-5.4',
    call: (sys, usr, max) => callAzureOpenAI(sys, usr, max, 'gpt-5.4'),
    available: () => !!(process.env.AZURE_OPENAI_API_KEY0 && process.env.AZURE_OPENAI_ENDPOINT0),
  },
  {
    name: 'Claude',
    call: (sys, usr, max) => callClaude(sys, usr, max),
    available: () => !!(process.env.CLAUDE_API_KEY && process.env.CLAUDE_ENDPOINT),
  },
];

let providerRotation = 0;

interface ProviderStats { calls: number; errors: number; promptTokens: number; completionTokens: number; totalTokens: number; }
const providerStats: Record<string, ProviderStats> = {};

function getProviderStats(name: string): ProviderStats {
  if (!providerStats[name]) providerStats[name] = { calls: 0, errors: 0, promptTokens: 0, completionTokens: 0, totalTokens: 0 };
  return providerStats[name];
}

function resetProviderStats() {
  for (const key of Object.keys(providerStats)) delete providerStats[key];
}

// ── Rate limiter & config ──────────────────────────────────────────────

const runtimeConfig = {
  totalFunds: 100,
  batchSize: 1,
  numWorkers: 100,
  maxConcurrent: 400,
  staggerMs: 100,
  maxRetries: 5,
  baseDelayMs: 800,
  maxTokens: 4000,
  cacheTtlMinutes: 10,
  rateLimitTpm: 450_000,
};

let sharedLimiter: TokenBucketLimiter | null = null;

function getSharedLimiter(): TokenBucketLimiter {
  if (!sharedLimiter || sharedLimiter.stats().tpm !== runtimeConfig.rateLimitTpm) {
    sharedLimiter = new TokenBucketLimiter(runtimeConfig.rateLimitTpm);
    console.log(`[LLM] Rate limiter: ${runtimeConfig.rateLimitTpm} TPM`);
  }
  return sharedLimiter;
}

// ── Helpers ────────────────────────────────────────────────────────────

function extractJSON(raw: string): string {
  // 1. Code block
  const m = raw.match(/```(?:json)?\s*([\s\S]*?)```/);
  if (m) return m[1].trim();
  // 2. Determine if outermost structure is object or array
  const objStart = raw.indexOf('{');
  const arrStart = raw.indexOf('[');
  // Prefer whichever appears first (outermost structure)
  if (objStart !== -1 && (arrStart === -1 || objStart < arrStart)) {
    const objEnd = raw.lastIndexOf('}');
    if (objEnd > objStart) return raw.slice(objStart, objEnd + 1);
  }
  if (arrStart !== -1) {
    const arrEnd = raw.lastIndexOf(']');
    if (arrEnd > arrStart) return raw.slice(arrStart, arrEnd + 1);
  }
  return raw.trim();
}

function normalizeFund(f: any, idx: number) {
  return {
    id: f.id || crypto.randomUUID(),
    isin: f.isin || f.ISIN || f.isin_code || `XX${String(idx).padStart(10, '0')}`,
    name: f.name || f.fund_name || f.nombre || f.fundName || `Fund ${idx + 1}`,
    category: f.category || f.categoria || f.type || f.asset_class || 'Sin categoría',
    managementCompany: f.managementCompany || f.management_company || f.gestora || f.manager || f.company || 'Desconocido',
    riskLevel: Number(f.riskLevel ?? f.risk_level ?? f.riesgo ?? f.risk ?? 5),
    netAssetValue: Number(f.netAssetValue ?? f.nav ?? f.NAV ?? f.valor_liquidativo ?? f.price ?? 0),
    currency: f.currency || f.divisa || f.moneda || 'EUR',
    expenseRatio: Number(f.expenseRatio ?? f.expense_ratio ?? f.ter ?? f.TER ?? f.comision ?? 0),
    rating: Number(f.rating ?? f.morningstar_rating ?? f.stars ?? 3),
    rankingPosition: idx + 1,
    lastUpdated: new Date().toISOString(),
    latestPerformance: {
      return1Month: Number(f.latestPerformance?.return1Month ?? f.return_1m ?? f.rentabilidad_1m ?? f.perf_1m ?? 0),
      return3Months: Number(f.latestPerformance?.return3Months ?? f.return_3m ?? f.rentabilidad_3m ?? f.perf_3m ?? 0),
      return6Months: Number(f.latestPerformance?.return6Months ?? f.return_6m ?? f.rentabilidad_6m ?? f.perf_6m ?? 0),
      return1Year: Number(f.latestPerformance?.return1Year ?? f.return_1y ?? f.rentabilidad_1a ?? f.perf_1y ?? 0),
      return3Years: Number(f.latestPerformance?.return3Years ?? f.return_3y ?? f.rentabilidad_3a ?? f.perf_3y ?? 0),
      return5Years: Number(f.latestPerformance?.return5Years ?? f.return_5y ?? f.rentabilidad_5a ?? f.perf_5y ?? 0),
      volatility: Number(f.latestPerformance?.volatility ?? f.volatility ?? f.volatilidad ?? 0),
      sharpeRatio: Number(f.latestPerformance?.sharpeRatio ?? f.sharpe_ratio ?? f.sharpe ?? 0),
      recordedAt: new Date().toISOString(),
    },
  };
}

function delay(ms: number): Promise<void> { return new Promise((r) => setTimeout(r, ms)); }

function isRetryable(err: any): boolean {
  const code = err?.code ?? err?.cause?.code;
  const status = err?.status ?? err?.response?.status;
  const msg = String(err?.message ?? '');
  return code === 'ECONNRESET' || code === 'ETIMEDOUT' || code === 'ECONNREFUSED' || code === 'EPIPE' ||
    status === 429 || (typeof status === 'number' && status >= 500) || msg.includes('Unexpected end of JSON input');
}

async function callLLM(sys: string, usr: string, maxTokens: number): Promise<string> {
  const available = LLM_PROVIDERS.filter((p) => p.available());
  if (available.length === 0) throw new Error('No LLM providers available');

  const limiter = getSharedLimiter();
  const estimated = estimateTokens(sys + usr) + maxTokens;
  const resId = await limiter.reserve(estimated);

  const startIdx = providerRotation % available.length;
  providerRotation++;

  for (let i = 0; i < available.length; i++) {
    const provider = available[(startIdx + i) % available.length];
    const stats = getProviderStats(provider.name);
    try {
      stats.calls++;
      const result = await provider.call(sys, usr, maxTokens);
      limiter.confirm(resId, result.usage.totalTokens);
      stats.promptTokens += result.usage.promptTokens;
      stats.completionTokens += result.usage.completionTokens;
      stats.totalTokens += result.usage.totalTokens;
      return result.content;
    } catch (err) {
      stats.errors++;
      console.warn(`[LLM] ${provider.name} failed: ${(err as Error).message}`);
      if (i === available.length - 1) { limiter.release(resId); throw err; }
    }
  }
  limiter.release(resId);
  throw new Error('All providers failed');
}

// ── Cache ──────────────────────────────────────────────────────────────

let fundCache: { funds: unknown[]; timestamp: number } | null = null;

// ── Worker pool ────────────────────────────────────────────────────────

async function runPool<T>(count: number, worker: (i: number) => Promise<T>, maxConcurrency: number, staggerMs: number): Promise<T[]> {
  const results = new Array<T>(count);
  let next = 0;
  async function runner(slot: number): Promise<void> {
    await delay(slot * staggerMs);
    while (true) { const idx = next++; if (idx >= count) return; results[idx] = await worker(idx); }
  }
  await Promise.all(Array.from({ length: Math.min(maxConcurrency, count) }, (_, slot) => runner(slot)));
  return results;
}

async function fetchBatchWithRetry(batchIndex: number): Promise<unknown[]> {
  const { batchSize: BATCH_SIZE, maxRetries: MAX_RETRIES, baseDelayMs: BASE_DELAY_MS } = runtimeConfig;
  let lastErr: unknown;
  for (let attempt = 0; attempt <= MAX_RETRIES; attempt++) {
    try {
      const prompt = batchPrompt(batchIndex, BATCH_SIZE);
      const raw = await callLLM(SYSTEM_PROMPT, prompt, runtimeConfig.maxTokens);
      const json = extractJSON(raw);
      const parsed = JSON.parse(json);
      if (!Array.isArray(parsed)) throw new Error(`Batch ${batchIndex}: not an array`);
      return parsed.map((f: any, i: number) => normalizeFund(f, batchIndex * BATCH_SIZE + i));
    } catch (err: any) {
      lastErr = err;
      if (attempt === MAX_RETRIES || !isRetryable(err)) throw err;
      const backoff = BASE_DELAY_MS * 2 ** attempt + Math.random() * BASE_DELAY_MS;
      await delay(backoff);
    }
  }
  throw lastErr;
}

// ── Routes ─────────────────────────────────────────────────────────────

// SSE streaming
app.get('/api/llm/funds/stream', async (_req, res) => {
  const cacheTtl = runtimeConfig.cacheTtlMinutes * 60_000;
  res.writeHead(200, { 'Content-Type': 'text/event-stream', 'Cache-Control': 'no-cache', Connection: 'keep-alive' });

  if (fundCache && Date.now() - fundCache.timestamp < cacheTtl) {
    res.write(`data: ${JSON.stringify({ type: 'batch', workerIndex: 0, workersCompleted: runtimeConfig.numWorkers, workersTotal: runtimeConfig.numWorkers, newFunds: fundCache.funds, totalSoFar: fundCache.funds.length })}\n\n`);
    res.write(`data: ${JSON.stringify({ type: 'done', totalFunds: fundCache.funds.length, elapsed: '0.0 (cached)' })}\n\n`);
    res.end();
    return;
  }

  const start = Date.now();
  const allFunds: unknown[] = [];
  const seenIsins = new Set<string>();
  let completedWorkers = 0;

  res.write(`data: ${JSON.stringify({ type: 'status', message: `Lanzando ${runtimeConfig.numWorkers} workers...`, total: runtimeConfig.totalFunds })}\n\n`);

  await runPool(runtimeConfig.numWorkers, async (i) => {
    try {
      const funds = await fetchBatchWithRetry(i);
      completedWorkers++;
      const newFunds: unknown[] = [];
      for (const fund of funds) {
        const isin = (fund as any).isin;
        if (!seenIsins.has(isin)) { seenIsins.add(isin); (fund as any).rankingPosition = allFunds.length + newFunds.length + 1; newFunds.push(fund); }
      }
      allFunds.push(...newFunds);
      res.write(`data: ${JSON.stringify({ type: 'batch', workerIndex: i, workersCompleted: completedWorkers, workersTotal: runtimeConfig.numWorkers, newFunds, totalSoFar: allFunds.length })}\n\n`);
    } catch (err) {
      completedWorkers++;
      res.write(`data: ${JSON.stringify({ type: 'error', workerIndex: i, workersCompleted: completedWorkers, workersTotal: runtimeConfig.numWorkers, message: (err as Error).message })}\n\n`);
    }
  }, runtimeConfig.maxConcurrent, runtimeConfig.staggerMs);

  if (allFunds.length > 0) fundCache = { funds: allFunds, timestamp: Date.now() };
  const elapsed = ((Date.now() - start) / 1000).toFixed(1);
  res.write(`data: ${JSON.stringify({ type: 'done', totalFunds: allFunds.length, elapsed })}\n\n`);
  res.end();
});

// Classic JSON
app.get('/api/llm/funds', async (_req, res) => {
  const cacheTtl = runtimeConfig.cacheTtlMinutes * 60_000;
  if (fundCache && Date.now() - fundCache.timestamp < cacheTtl) {
    return res.json(fundCache.funds);
  }
  try {
    const allFunds: unknown[] = [];
    const seenIsins = new Set<string>();
    const results = await runPool(runtimeConfig.numWorkers, async (i) => {
      try { return { funds: await fetchBatchWithRetry(i) }; }
      catch { return { funds: [] }; }
    }, runtimeConfig.maxConcurrent, runtimeConfig.staggerMs);
    for (const r of results) for (const f of r.funds) {
      const isin = (f as any).isin;
      if (!seenIsins.has(isin)) { seenIsins.add(isin); allFunds.push(f); }
    }
    allFunds.forEach((f: any, i) => { f.rankingPosition = i + 1; });
    if (allFunds.length > 0) fundCache = { funds: allFunds, timestamp: Date.now() };
    res.json(allFunds);
  } catch (err) {
    res.status(500).json({ error: (err as Error).message });
  }
});

// Config GET/POST
app.get('/api/llm/config', (_req: express.Request, res: express.Response) => {
  const providers = LLM_PROVIDERS.map((p) => ({
    name: p.name,
    available: p.available(),
    endpoint: p.name === 'Claude'
      ? (process.env.CLAUDE_ENDPOINT || '').replace(/\/anthropic.*/, '/...')
      : (process.env.AZURE_OPENAI_ENDPOINT0 || '').replace(/\/$/, ''),
    model: p.name === 'Claude' ? (process.env.CLAUDE_MODEL1 || 'claude-opus-4-7') : undefined,
    deployment: p.name !== 'Claude' ? (p.name === 'GPT-5.4' ? 'gpt-5.4' : (process.env.AZURE_OPENAI_DEPLOYMENT0 || 'gpt-5.5')) : undefined,
  }));
  const cacheInfo = fundCache ? {
    funds: fundCache.funds.length,
    ageMinutes: +((Date.now() - fundCache.timestamp) / 60000).toFixed(1),
    ttlMinutes: runtimeConfig.cacheTtlMinutes,
    fresh: (Date.now() - fundCache.timestamp) < runtimeConfig.cacheTtlMinutes * 60000,
  } : null;
  res.json({ config: { ...runtimeConfig }, providers, cache: cacheInfo, rateLimiter: getSharedLimiter().stats(), providerStats: { ...providerStats } });
});

app.post('/api/llm/config', (req, res) => {
  const updates = req.body;
  const allowed = ['totalFunds', 'batchSize', 'numWorkers', 'maxConcurrent', 'staggerMs', 'maxRetries', 'baseDelayMs', 'maxTokens', 'cacheTtlMinutes', 'rateLimitTpm'] as const;
  for (const key of allowed) {
    if (key in updates && typeof updates[key] === 'number' && updates[key] > 0) {
      (runtimeConfig as any)[key] = updates[key];
    }
  }
  if ('batchSize' in updates || 'totalFunds' in updates) {
    runtimeConfig.numWorkers = Math.ceil(runtimeConfig.totalFunds / runtimeConfig.batchSize);
  }
  if ('rateLimitTpm' in updates) sharedLimiter = null;
  console.log('[LLM] Config actualizada:', runtimeConfig);
  res.json({ ok: true, config: { ...runtimeConfig } });
});

// Reload
app.post('/api/llm/reload', (_req, res) => {
  fundCache = null;
  resetProviderStats();
  res.json({ ok: true, message: 'Caché invalidada. Reconecta al stream para recargar.' });
});

// Compare - AI analysis of user's funds ranking
app.post('/api/llm/compare', async (req, res) => {
  const { funds } = req.body;
  if (!Array.isArray(funds) || funds.length < 2) {
    return res.status(400).json({ error: 'Se necesitan al menos 2 fondos para comparar' });
  }
  console.log(`[LLM] Compare: ${funds.length} fondos`);
  try {
    const fundsInfo = funds.map((f: any, i: number) =>
      `${i + 1}. ${f.name} (${f.isin}) - Categoría: ${f.category}, Gestora: ${f.managementCompany}, ` +
      `NAV: ${f.netAssetValue} ${f.currency}, TER: ${f.expenseRatio}%, Riesgo: ${f.riskLevel}/7, ` +
      `Rent 1A: ${f.return1Year}%, Rent 3A: ${f.return3Years}%, Rent 5A: ${f.return5Years}%, ` +
      `Volatilidad: ${f.volatility}%, Sharpe: ${f.sharpeRatio}`
    ).join('\n');

    const prompt = `Analiza y compara estos fondos de inversión del usuario. Ordénalos del mejor al peor y explica por qué. Además, indica cuál de los fondos genera más rentabilidad a corto plazo (1 año), medio plazo (3 años) y largo plazo (5 años).

${fundsInfo}

Responde con un JSON con esta estructura exacta:
{
  "ranking": [
    {
      "position": 1,
      "isin": "ISIN del fondo",
      "score": 85,
      "strengths": ["punto fuerte 1", "punto fuerte 2"],
      "weaknesses": ["punto débil 1"]
    }
  ],
  "bestByHorizon": {
    "shortTerm": { "isin": "ISIN", "name": "nombre del fondo", "return": 12.5, "reason": "Explicación breve en español" },
    "mediumTerm": { "isin": "ISIN", "name": "nombre del fondo", "return": 35.2, "reason": "Explicación breve en español" },
    "longTerm": { "isin": "ISIN", "name": "nombre del fondo", "return": 60.0, "reason": "Explicación breve en español" }
  },
  "summary": "Resumen general de 2-3 frases en español comparando los fondos",
  "recommendation": "Recomendación personalizada en español de 1-2 frases"
}

Criterios de puntuación (score 0-100):
- Rentabilidad ajustada al riesgo (Sharpe ratio) es lo más importante
- Menor TER es mejor a igualdad de rentabilidad
- Mayor diversificación geográfica es preferible
- Consistencia a largo plazo (rent 3A y 5A) pesa más que corto plazo

Para bestByHorizon usa los datos reales de rentabilidad proporcionados (Rent 1A, 3A, 5A). El campo "return" es el % de rentabilidad en ese horizonte.`;

    const raw = await callLLM(SYSTEM_PROMPT, prompt, 4000);
    console.log(`[LLM] Compare raw (first 1500): ${raw.substring(0, 1500)}`);
    const json = extractJSON(raw);
    const parsed = JSON.parse(json);
    console.log(`[LLM] Compare parsed keys: ${Object.keys(parsed).join(', ')}`);
    console.log(`[LLM] Compare bestByHorizon: ${JSON.stringify(parsed.bestByHorizon || parsed.best_by_horizon || 'NOT FOUND')}`);
    // Ensure expected structure
    const result = {
      ranking: Array.isArray(parsed.ranking) ? parsed.ranking : (Array.isArray(parsed) ? parsed.map((r: any, i: number) => ({ position: i + 1, isin: r.isin || '', score: r.score ?? 50, strengths: r.strengths || [], weaknesses: r.weaknesses || [] })) : []),
      bestByHorizon: parsed.bestByHorizon || parsed.best_by_horizon || parsed.bestbyhorizon || parsed.mejor_por_plazo || null,
      summary: parsed.summary || parsed.resumen || '',
      recommendation: parsed.recommendation || parsed.recomendacion || '',
    };
    console.log(`[LLM] Compare result: ${result.ranking.length} fondos rankeados`);
    res.json(result);
  } catch (err) {
    console.error(`[LLM] Compare error: ${(err as Error).message}`);
    res.status(500).json({ error: (err as Error).message });
  }
});

// Lookup
app.get('/api/llm/lookup', async (req, res) => {
  const query = (req.query.q as string)?.trim();
  if (!query) return res.status(400).json({ error: 'Missing ?q= parameter' });
  console.log(`[LLM] Lookup: "${query}"`);

  const tryLookup = async (attempt: number): Promise<any> => {
    const extraHint = attempt > 1
      ? `\n\nNOTA: Este es un fondo real registrado en CNMV/Morningstar. Búscalo con más detalle. ISIN: ${query}. Devuelve datos estimados si es necesario, NUNCA respondas con error si el ISIN tiene formato válido (empieza con código país de 2 letras + 10 caracteres alfanuméricos).`
      : '';
    const raw = await callLLM(SYSTEM_PROMPT, lookupPrompt(query) + extraHint, 3000);
    console.log(`[LLM] Lookup raw response (attempt ${attempt}, first 500 chars): ${raw.substring(0, 500)}`);
    const json = extractJSON(raw);
    const parsed = JSON.parse(json);
    const fund = Array.isArray(parsed) ? parsed[0] : parsed;
    console.log(`[LLM] Lookup parsed keys: ${Object.keys(fund).join(', ')}`);
    return fund;
  };

  try {
    let fund = await tryLookup(1);

    // Si el LLM responde con error y el query tiene formato de ISIN válido, reintentar
    if (fund.error) {
      const isISIN = /^[A-Z]{2}[A-Z0-9]{9}[0-9]$/.test(query.toUpperCase());
      if (isISIN) {
        console.log(`[LLM] Lookup: primer intento falló para ISIN ${query}, reintentando...`);
        fund = await tryLookup(2);
      }
      if (fund.error) {
        return res.status(404).json({ error: 'Fondo no encontrado' });
      }
    }

    res.json(normalizeFund(fund, 0));
  } catch (err) {
    console.error(`[LLM] Lookup error: ${(err as Error).message}`);
    res.status(500).json({ error: (err as Error).message });
  }
});

// Health
app.get('/health', (_req, res) => res.json({ status: 'ok' }));

// ── Start ──────────────────────────────────────────────────────────────

const PORT = Number(process.env.LLM_PORT) || 3100;
app.listen(PORT, '0.0.0.0', () => {
  console.log(`[LLM Server] Running on port ${PORT}`);
  console.log(`[LLM Server] Providers: ${LLM_PROVIDERS.map(p => `${p.name}=${p.available() ? 'OK' : 'N/A'}`).join(', ')}`);
});
