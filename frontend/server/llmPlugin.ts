import type { Plugin } from 'vite';
import { loadEnv } from 'vite';
import { readFileSync, writeFileSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { TokenBucketLimiter, estimateTokens } from './tokenBucketLimiter';

// ── Prompt loader ──────────────────────────────────────────────────────────

const __dirname = dirname(fileURLToPath(import.meta.url));
const PROMPTS_DIR = resolve(__dirname, 'prompts');

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

// ── LLM Callers ────────────────────────────────────────────────────────────

interface LLMUsage {
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
}

interface LLMResult {
  content: string;
  usage: LLMUsage;
}

async function callAzureOpenAI(
  env: Record<string, string>,
  systemPrompt: string,
  userPrompt: string,
  maxTokens: number,
  deploymentOverride?: string,
): Promise<LLMResult> {
  const apiKey = env.AZURE_OPENAI_API_KEY0;
  const endpoint = (env.AZURE_OPENAI_ENDPOINT0 || '').replace(/\/$/, '');
  const deployment = deploymentOverride || env.AZURE_OPENAI_DEPLOYMENT0;
  const apiVersion = env.AZURE_OPENAI_API_VERSION0;

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
    const data = await resp.json();
    const usage: LLMUsage = {
      promptTokens: data.usage?.prompt_tokens ?? 0,
      completionTokens: data.usage?.completion_tokens ?? 0,
      totalTokens: data.usage?.total_tokens ?? 0,
    };
    return { content: data.choices[0].message.content, usage };
  } finally {
    clearTimeout(timer);
  }
}

async function callClaude(env: Record<string, string>, systemPrompt: string, userPrompt: string, maxTokens = 4000): Promise<LLMResult> {
  const apiKey = env.CLAUDE_API_KEY;
  const endpoint = env.CLAUDE_ENDPOINT;
  const model = env.CLAUDE_MODEL1 || 'claude-opus-4-7';

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
      body: JSON.stringify({
        model,
        max_tokens: maxTokens,
        system: systemPrompt,
        messages: [{ role: 'user', content: userPrompt }],
      }),
      signal: controller.signal,
    });
    if (!resp.ok) throw new Error(`HTTP ${resp.status}: ${(await resp.text()).substring(0, 200)}`);
    const data = await resp.json();
    const usage: LLMUsage = {
      promptTokens: data.usage?.input_tokens ?? 0,
      completionTokens: data.usage?.output_tokens ?? 0,
      totalTokens: (data.usage?.input_tokens ?? 0) + (data.usage?.output_tokens ?? 0),
    };
    return { content: data.content[0].text, usage };
  } finally {
    clearTimeout(timer);
  }
}

// ── LLM chain: GPT-5.5 → GPT-5.4 → Claude (auto-failover) ────────────

interface LLMProvider {
  name: string;
  call: (env: Record<string, string>, sys: string, usr: string, max: number) => Promise<LLMResult>;
  available: (env: Record<string, string>) => boolean;
  enabled: boolean;
}

const LLM_PROVIDERS: LLMProvider[] = [
  {
    name: 'GPT-5.5',
    call: (env, sys, usr, max) => callAzureOpenAI(env, sys, usr, max),
    available: (env) => !!(env.AZURE_OPENAI_API_KEY0 && env.AZURE_OPENAI_ENDPOINT0 && env.AZURE_OPENAI_DEPLOYMENT0),
    enabled: true,
  },
  {
    name: 'GPT-5.4',
    call: (env, sys, usr, max) => callAzureOpenAI(env, sys, usr, max, 'gpt-5.4'),
    available: (env) => !!(env.AZURE_OPENAI_API_KEY0 && env.AZURE_OPENAI_ENDPOINT0),
    enabled: true,
  },
  {
    name: 'Claude',
    call: (env, sys, usr, max) => callClaude(env, sys, usr, max),
    available: (env) => !!(env.CLAUDE_API_KEY && env.CLAUDE_ENDPOINT),
    enabled: true,
  },
];

// ── Provider state persistence ─────────────────────────────────────────────
const PROVIDERS_STATE_FILE = resolve(__dirname, '..', '.llm-providers-state.json');

interface ProviderState {
  enabled: Record<string, boolean>;
  deleted: string[];
  custom: Array<{ name: string; type: 'azure' | 'claude'; endpoint?: string; deployment?: string; model?: string; apiKey?: string }>;
}

const BUILTIN_NAMES = ['GPT-5.5', 'GPT-5.4', 'Claude'];
let deletedProviders: string[] = [];

function loadProviderState(): ProviderState {
  try {
    if (existsSync(PROVIDERS_STATE_FILE)) {
      const raw = JSON.parse(readFileSync(PROVIDERS_STATE_FILE, 'utf-8'));
      return { enabled: raw.enabled ?? {}, deleted: raw.deleted ?? [], custom: raw.custom ?? [] };
    }
  } catch { /* ignore parse errors */ }
  return { enabled: {}, deleted: [], custom: [] };
}

function saveProviderState() {
  const state: ProviderState = {
    enabled: Object.fromEntries(LLM_PROVIDERS.map(p => [p.name, p.enabled])),
    deleted: deletedProviders,
    custom: LLM_PROVIDERS
      .filter(p => !BUILTIN_NAMES.includes(p.name))
      .map(p => ({ name: p.name, type: 'azure', endpoint: '', deployment: '' })),
  };
  writeFileSync(PROVIDERS_STATE_FILE, JSON.stringify(state, null, 2));
}

function restoreProviderState(env: Record<string, string>) {
  const state = loadProviderState();
  deletedProviders = state.deleted;

  // Remove built-in providers that were previously deleted
  for (const name of state.deleted) {
    const idx = LLM_PROVIDERS.findIndex(p => p.name === name);
    if (idx !== -1) {
      LLM_PROVIDERS.splice(idx, 1);
    }
  }

  // Restore enabled flags for remaining built-in providers
  for (const provider of LLM_PROVIDERS) {
    if (provider.name in state.enabled) {
      provider.enabled = state.enabled[provider.name];
    }
  }
  // Restore custom providers
  for (const custom of state.custom) {
    if (LLM_PROVIDERS.find(p => p.name === custom.name)) continue;
    if (state.deleted.includes(custom.name)) continue;
    const newProvider: LLMProvider = custom.type === 'claude'
      ? {
          name: custom.name,
          call: (_env, sys, usr, max) => callClaude({ ...env, CLAUDE_API_KEY: custom.apiKey || env.CLAUDE_API_KEY, CLAUDE_ENDPOINT: custom.endpoint || env.CLAUDE_ENDPOINT, CLAUDE_MODEL1: custom.model || 'claude-opus-4-7' }, sys, usr, max),
          available: () => !!(custom.apiKey || env.CLAUDE_API_KEY),
          enabled: state.enabled[custom.name] ?? true,
        }
      : {
          name: custom.name,
          call: (_env, sys, usr, max) => callAzureOpenAI({ ...env, AZURE_OPENAI_API_KEY0: custom.apiKey || env.AZURE_OPENAI_API_KEY0, AZURE_OPENAI_ENDPOINT0: custom.endpoint || env.AZURE_OPENAI_ENDPOINT0, AZURE_OPENAI_DEPLOYMENT0: custom.deployment || 'gpt-4o', AZURE_OPENAI_API_VERSION0: env.AZURE_OPENAI_API_VERSION0 || '2024-08-01-preview' }, sys, usr, max, custom.deployment),
          available: () => !!(custom.apiKey || env.AZURE_OPENAI_API_KEY0),
          enabled: state.enabled[custom.name] ?? true,
        };
    LLM_PROVIDERS.push(newProvider);
  }
  console.log(`[LLM] Provider state restored: ${LLM_PROVIDERS.map(p => `${p.name}(${p.enabled ? 'on' : 'off'})`).join(', ')}${state.deleted.length ? ` | deleted: ${state.deleted.join(', ')}` : ''}`);
}

// Round-robin counter to distribute load across providers
let providerRotation = 0;

// ── Provider Stats Tracker ─────────────────────────────────────────────────

interface ProviderStats {
  calls: number;
  errors: number;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
}

const providerStats: Record<string, ProviderStats> = {};

function getProviderStats(name: string): ProviderStats {
  if (!providerStats[name]) {
    providerStats[name] = { calls: 0, errors: 0, promptTokens: 0, completionTokens: 0, totalTokens: 0 };
  }
  return providerStats[name];
}

function resetProviderStats() {
  for (const key of Object.keys(providerStats)) {
    delete providerStats[key];
  }
}

async function callLLMRaw(env: Record<string, string>, systemPrompt: string, userPrompt: string, maxTokens = 4000): Promise<LLMResult> {
  const available = LLM_PROVIDERS.filter((p) => p.enabled && p.available(env));
  if (available.length === 0) throw new Error('No LLM providers available (all disabled or missing keys)');

  const startIdx = providerRotation % available.length;
  providerRotation++;

  const errors: string[] = [];

  for (let offset = 0; offset < available.length; offset++) {
    const provider = available[(startIdx + offset) % available.length];
    const stats = getProviderStats(provider.name);
    try {
      const result = await provider.call(env, systemPrompt, userPrompt, maxTokens);
      stats.calls++;
      stats.promptTokens += result.usage.promptTokens;
      stats.completionTokens += result.usage.completionTokens;
      stats.totalTokens += result.usage.totalTokens;
      return result;
    } catch (err) {
      stats.errors++;
      const msg = (err as Error).message;
      console.warn(`[LLM] ${provider.name} failed: ${msg}`);
      errors.push(`${provider.name}: ${msg}`);
    }
  }

  throw new Error(`All LLM providers failed: ${errors.join(' | ')}`);
}

/**
 * callLLM con rate-limiting por TPM (patrón CobolForge).
 * Reserva tokens ANTES de llamar, espera si no cabe, libera si falla.
 */
async function callLLM(env: Record<string, string>, systemPrompt: string, userPrompt: string, maxTokens = 4000): Promise<string> {
  const limiter = getSharedLimiter();
  const estInput = estimateTokens(systemPrompt + userPrompt);
  const estOutput = Math.ceil(maxTokens * 0.5);
  const estTotal = estInput + estOutput;

  const reservationId = await limiter.reserve(estTotal);
  let ok = false;
  try {
    const result = await callLLMRaw(env, systemPrompt, userPrompt, maxTokens);
    // Confirma con uso real del proveedor
    limiter.confirm(reservationId, result.usage.totalTokens || (estInput + estimateTokens(result.content)));
    ok = true;
    return result.content;
  } finally {
    if (!ok) limiter.release(reservationId);
  }
}

// ── Utilities ──────────────────────────────────────────────────────────────

function extractJSON(text: string): string {
  let s = text.trim();
  if (s.startsWith('```')) s = s.replace(/^```(?:json)?\s*\n?/, '').replace(/\n?```\s*$/, '');
  const arrStart = s.indexOf('[');
  const arrEnd = s.lastIndexOf(']');
  if (arrStart !== -1 && arrEnd !== -1 && arrStart < arrEnd) return s.substring(arrStart, arrEnd + 1);
  const objStart = s.indexOf('{');
  const objEnd = s.lastIndexOf('}');
  if (objStart !== -1 && objEnd !== -1 && objStart < objEnd) return s.substring(objStart, objEnd + 1);
  return s;
}

function normalizeFund(f: any, index: number): any {
  const now = new Date().toISOString();
  let perf = f.latestPerformance || f.performance || f.latest_performance || null;

  if (!perf && (f.return1Year !== undefined || f.return_1_year !== undefined)) {
    perf = {
      return1Month: f.return1Month ?? f.return_1_month ?? 0,
      return3Months: f.return3Months ?? f.return_3_months ?? 0,
      return6Months: f.return6Months ?? f.return_6_months ?? 0,
      return1Year: f.return1Year ?? f.return_1_year ?? 0,
      return3Years: f.return3Years ?? f.return_3_years ?? 0,
      return5Years: f.return5Years ?? f.return_5_years ?? 0,
      volatility: f.volatility ?? 12,
      sharpeRatio: f.sharpeRatio ?? f.sharpe_ratio ?? 1.0,
      recordedAt: now,
    };
  }

  if (!perf) {
    const rating = Number(f.rating) || 3;
    const base = rating * 4 + Math.random() * 5;
    perf = {
      return1Month: +(base / 12).toFixed(2), return3Months: +(base / 4).toFixed(2),
      return6Months: +(base / 2).toFixed(2), return1Year: +base.toFixed(2),
      return3Years: +(base * 2.5).toFixed(2), return5Years: +(base * 4).toFixed(2),
      volatility: +(8 + Math.random() * 12).toFixed(2),
      sharpeRatio: +(0.5 + rating * 0.4).toFixed(2), recordedAt: now,
    };
  }

  return {
    id: f.id || `fund-${String(index + 1).padStart(3, '0')}`,
    isin: f.isin || 'N/A',
    name: f.name || `Fund ${index + 1}`,
    category: f.category || 'Renta Variable Global',
    managementCompany: f.managementCompany || f.management_company || 'Unknown',
    riskLevel: Number(f.riskLevel ?? f.risk_level) || 5,
    netAssetValue: Number(f.netAssetValue ?? f.nav ?? f.net_asset_value) || 100,
    currency: f.currency || 'EUR',
    expenseRatio: Number(f.expenseRatio ?? f.expense_ratio ?? f.ter) || 0.2,
    rating: Number(f.rating) || 3,
    rankingPosition: Number(f.rankingPosition ?? f.ranking_position) || index + 1,
    lastUpdated: f.lastUpdated || f.last_updated || now,
    latestPerformance: {
      return1Month: Number(perf.return1Month ?? perf.return_1_month) || 0,
      return3Months: Number(perf.return3Months ?? perf.return_3_months) || 0,
      return6Months: Number(perf.return6Months ?? perf.return_6_months) || 0,
      return1Year: Number(perf.return1Year ?? perf.return_1_year) || 0,
      return3Years: Number(perf.return3Years ?? perf.return_3_years) || 0,
      return5Years: Number(perf.return5Years ?? perf.return_5_years) || 0,
      volatility: Number(perf.volatility) || 12,
      sharpeRatio: Number(perf.sharpeRatio ?? perf.sharpe_ratio) || 1.0,
      recordedAt: perf.recordedAt || perf.recorded_at || now,
    },
  };
}

// ── Cache ──────────────────────────────────────────────────────────────────

interface FundCache {
  funds: unknown[];
  timestamp: number;
}

let fundCache: FundCache | null = null;

// ── Runtime Config (mutable from /api/llm/config) ──────────────────────────

export interface LLMRuntimeConfig {
  totalFunds: number;
  batchSize: number;
  numWorkers: number;
  maxConcurrent: number;
  staggerMs: number;
  maxRetries: number;
  baseDelayMs: number;
  maxTokens: number;
  cacheTtlMinutes: number;
  rateLimitTpm: number;
}

const runtimeConfig: LLMRuntimeConfig = {
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

// ── Shared Token Bucket Limiter (patrón CobolForge) ────────────────────────

let sharedLimiter: TokenBucketLimiter | null = null;

function getSharedLimiter(): TokenBucketLimiter {
  if (!sharedLimiter || sharedLimiter.stats().tpm !== runtimeConfig.rateLimitTpm) {
    sharedLimiter = new TokenBucketLimiter(runtimeConfig.rateLimitTpm);
    console.log(`[LLM] Rate limiter creado: ${runtimeConfig.rateLimitTpm} TPM (threshold ${Math.floor(runtimeConfig.rateLimitTpm * 0.9)})`);
  }
  return sharedLimiter;
}

// ── Parallel Worker Pool ───────────────────────────────────────────────────

// Legacy constants derived from runtimeConfig (used in functions below)
function getConfig() {
  return {
    TOTAL_FUNDS: runtimeConfig.totalFunds,
    BATCH_SIZE: runtimeConfig.batchSize,
    NUM_WORKERS: runtimeConfig.numWorkers,
    MAX_CONCURRENT: runtimeConfig.maxConcurrent,
    STAGGER_MS: runtimeConfig.staggerMs,
    MAX_RETRIES: runtimeConfig.maxRetries,
    BASE_DELAY_MS: runtimeConfig.baseDelayMs,
  };
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/** Detect transient errors worth retrying */
function isRetryable(err: any): boolean {
  const code = err?.code ?? err?.cause?.code;
  const status = err?.status ?? err?.response?.status;
  const msg = String(err?.message ?? '');
  return (
    code === 'ECONNRESET' ||
    code === 'ETIMEDOUT' ||
    code === 'ECONNREFUSED' ||
    code === 'EPIPE' ||
    status === 429 ||
    (typeof status === 'number' && status >= 500) ||
    msg.includes('Unexpected end of JSON input')
  );
}

async function fetchBatchWithRetry(env: Record<string, string>, batchIndex: number): Promise<unknown[]> {
  const { BATCH_SIZE, MAX_RETRIES, BASE_DELAY_MS } = getConfig();
  let lastErr: unknown;
  for (let attempt = 0; attempt <= MAX_RETRIES; attempt++) {
    try {
      const prompt = batchPrompt(batchIndex, BATCH_SIZE);
      const raw = await callLLM(env, SYSTEM_PROMPT, prompt, runtimeConfig.maxTokens);
      const json = extractJSON(raw);
      const parsed = JSON.parse(json);
      if (!Array.isArray(parsed)) throw new Error(`Batch ${batchIndex}: not an array`);
      return parsed.map((f: any, i: number) => normalizeFund(f, batchIndex * BATCH_SIZE + i));
    } catch (err: any) {
      lastErr = err;
      console.warn(`[LLM] Worker ${batchIndex + 1} attempt ${attempt + 1}/${MAX_RETRIES + 1}: ${err.message}`);
      if (attempt === MAX_RETRIES || !isRetryable(err)) throw err;

      // Respetar Retry-After del proveedor si viene
      const retryAfterSec = Number(err?.response?.headers?.['retry-after']);
      const retryAfterMs = Number.isFinite(retryAfterSec) && retryAfterSec > 0 ? retryAfterSec * 1000 : 0;
      const backoff = retryAfterMs || (BASE_DELAY_MS * 2 ** attempt + Math.random() * BASE_DELAY_MS);
      console.log(`[LLM] Worker ${batchIndex + 1}: retry en ${Math.round(backoff)}ms`);
      await delay(backoff);
    }
  }
  throw lastErr;
}

/**
 * Run worker pool with limited concurrency, stagger and ordered results.
 * Each slot starts deferred by `slot * staggerMs`.
 */
async function runPool<T>(
  count: number,
  worker: (index: number) => Promise<T>,
  maxConcurrency: number,
  staggerMs: number,
): Promise<T[]> {
  const results = new Array<T>(count);
  let next = 0;

  async function runner(slot: number): Promise<void> {
    await delay(slot * staggerMs);
    while (true) {
      const idx = next++;
      if (idx >= count) return;
      results[idx] = await worker(idx);
    }
  }

  await Promise.all(
    Array.from({ length: Math.min(maxConcurrency, count) }, (_, slot) => runner(slot)),
  );
  return results;
}

async function fetchAllFundsParallel(env: Record<string, string>): Promise<unknown[]> {
  const { NUM_WORKERS, MAX_CONCURRENT, BATCH_SIZE, STAGGER_MS } = getConfig();
  console.log(`[LLM] Starting ${NUM_WORKERS} workers (max ${MAX_CONCURRENT} concurrent, ${BATCH_SIZE} funds each)...`);
  const start = Date.now();

  const results = await runPool(
    NUM_WORKERS,
    async (i): Promise<{ index: number; funds: unknown[]; error?: string }> => {
      try {
        const funds = await fetchBatchWithRetry(env, i);
        console.log(`[LLM] Worker ${i + 1}/${NUM_WORKERS} done: ${funds.length} funds`);
        return { index: i, funds };
      } catch (err) {
        console.warn(`[LLM] Worker ${i + 1}/${NUM_WORKERS} failed: ${(err as Error).message}`);
        return { index: i, funds: [], error: (err as Error).message };
      }
    },
    MAX_CONCURRENT,
    STAGGER_MS,
  );
  const allFunds: unknown[] = [];
  const seenIsins = new Set<string>();
  let failed = 0;

  // Merge results, deduplicate by ISIN
  for (const result of results.sort((a, b) => a.index - b.index)) {
    if (result.error) { failed++; continue; }
    for (const fund of result.funds) {
      const isin = (fund as any).isin;
      if (!seenIsins.has(isin)) {
        seenIsins.add(isin);
        allFunds.push(fund);
      }
    }
  }

  // Re-assign ranking positions
  allFunds.forEach((f: any, i) => { f.rankingPosition = i + 1; });

  const elapsed = ((Date.now() - start) / 1000).toFixed(1);
  console.log(`[LLM] All workers done in ${elapsed}s: ${allFunds.length} unique funds (${failed} workers failed)`);

  return allFunds;
}

// ── SSE streaming endpoint ─────────────────────────────────────────────────

async function streamFundsSSE(env: Record<string, string>, res: any) {
  const cfg = getConfig();

  res.writeHead(200, {
    'Content-Type': 'text/event-stream',
    'Cache-Control': 'no-cache',
    'Connection': 'keep-alive',
  });

  const start = Date.now();
  const allFunds: unknown[] = [];
  const seenIsins = new Set<string>();
  let completedWorkers = 0;

  // Send initial status
  res.write(`data: ${JSON.stringify({ type: 'status', message: `Lanzando ${cfg.NUM_WORKERS} workers (max ${cfg.MAX_CONCURRENT} simultáneos)...`, total: cfg.TOTAL_FUNDS })}\n\n`);

  await runPool(
    cfg.NUM_WORKERS,
    async (i) => {
      try {
        const funds = await fetchBatchWithRetry(env, i);
        completedWorkers++;
        const newFunds: unknown[] = [];
        for (const fund of funds) {
          const isin = (fund as any).isin;
          if (!seenIsins.has(isin)) {
            seenIsins.add(isin);
            (fund as any).rankingPosition = allFunds.length + newFunds.length + 1;
            newFunds.push(fund);
          }
        }
        allFunds.push(...newFunds);

        res.write(`data: ${JSON.stringify({
          type: 'batch',
          workerIndex: i,
          workersCompleted: completedWorkers,
          workersTotal: cfg.NUM_WORKERS,
          newFunds,
          totalSoFar: allFunds.length,
        })}\n\n`);

        console.log(`[LLM] Worker ${i + 1}: +${newFunds.length} funds (total: ${allFunds.length})`);
      } catch (err) {
        completedWorkers++;
        console.warn(`[LLM] Worker ${i + 1} failed: ${(err as Error).message}`);
        res.write(`data: ${JSON.stringify({
          type: 'error',
          workerIndex: i,
          workersCompleted: completedWorkers,
          workersTotal: cfg.NUM_WORKERS,
          message: (err as Error).message,
        })}\n\n`);
      }
    },
    cfg.MAX_CONCURRENT,
    cfg.STAGGER_MS,
  );

  const elapsed = ((Date.now() - start) / 1000).toFixed(1);

  // Cache result
  if (allFunds.length > 0) {
    fundCache = { funds: allFunds, timestamp: Date.now() };
  }

  // Send done
  res.write(`data: ${JSON.stringify({
    type: 'done',
    totalFunds: allFunds.length,
    elapsed,
  })}\n\n`);

  res.end();
}

// ── Plugin ─────────────────────────────────────────────────────────────────

export function llmApiPlugin(): Plugin {
  let env: Record<string, string> = {};

  return {
    name: 'economia-llm-api',
    configResolved(config) {
      env = loadEnv(config.mode, config.root, '');
      restoreProviderState(env);
    },
    configureServer(server) {

      // ── SSE streaming endpoint (progressive loading) ──────────────────
      server.middlewares.use('/api/llm/funds/stream', async (_req, res) => {
        const cacheTtl = runtimeConfig.cacheTtlMinutes * 60 * 1000;
        // Return cache if fresh
        if (fundCache && Date.now() - fundCache.timestamp < cacheTtl) {
          const cfg = getConfig();
          console.log(`[LLM] Serving ${fundCache.funds.length} cached funds`);
          res.writeHead(200, {
            'Content-Type': 'text/event-stream',
            'Cache-Control': 'no-cache',
            'Connection': 'keep-alive',
          });
          res.write(`data: ${JSON.stringify({
            type: 'batch',
            workerIndex: 0,
            workersCompleted: cfg.NUM_WORKERS,
            workersTotal: cfg.NUM_WORKERS,
            newFunds: fundCache.funds,
            totalSoFar: fundCache.funds.length,
          })}\n\n`);
          res.write(`data: ${JSON.stringify({
            type: 'done',
            totalFunds: fundCache.funds.length,
            elapsed: '0.0 (cached)',
          })}\n\n`);
          res.end();
          return;
        }

        await streamFundsSSE(env, res);
      });

      // ── Classic JSON endpoint (parallel, returns all at once) ─────────
      server.middlewares.use('/api/llm/funds', async (_req, res) => {
        res.setHeader('Content-Type', 'application/json');

        const cacheTtl = runtimeConfig.cacheTtlMinutes * 60 * 1000;
        // Return cache if fresh
        if (fundCache && Date.now() - fundCache.timestamp < cacheTtl) {
          console.log(`[LLM] Serving ${fundCache.funds.length} cached funds`);
          res.end(JSON.stringify(fundCache.funds));
          return;
        }

        try {
          const funds = await fetchAllFundsParallel(env);
          if (funds.length === 0) throw new Error('No funds returned from any worker');

          fundCache = { funds, timestamp: Date.now() };
          res.end(JSON.stringify(funds));
        } catch (err) {
          console.error('[LLM] Error:', (err as Error).message);
          res.statusCode = 500;
          res.end(JSON.stringify({ error: (err as Error).message }));
        }
      });

      // ── Config endpoint (GET = leer, POST = actualizar) ──────────────
      server.middlewares.use('/api/llm/config', async (req, res) => {
        res.setHeader('Content-Type', 'application/json');

        if (req.method === 'GET') {
          const providers = LLM_PROVIDERS.map((p) => ({
            name: p.name,
            available: p.available(env),
            enabled: p.enabled,
            endpoint: p.name === 'Claude'
              ? (env.CLAUDE_ENDPOINT || '').replace(/\/anthropic.*/, '/...')
              : (env.AZURE_OPENAI_ENDPOINT0 || '').replace(/\/$/, ''),
            model: p.name === 'Claude' ? (env.CLAUDE_MODEL1 || 'claude-opus-4-7') : undefined,
            deployment: p.name !== 'Claude' ? (p.name === 'GPT-5.4' ? 'gpt-5.4' : (env.AZURE_OPENAI_DEPLOYMENT0 || 'gpt-5.5')) : undefined,
          }));
          const cacheInfo = fundCache
            ? {
                funds: fundCache.funds.length,
                ageMinutes: +((Date.now() - fundCache.timestamp) / 60000).toFixed(1),
                ttlMinutes: runtimeConfig.cacheTtlMinutes,
                fresh: (Date.now() - fundCache.timestamp) < runtimeConfig.cacheTtlMinutes * 60000,
              }
            : null;
          res.end(JSON.stringify({ config: { ...runtimeConfig }, providers, cache: cacheInfo, rateLimiter: getSharedLimiter().stats(), providerStats: { ...providerStats } }));
          return;
        }

        if (req.method === 'POST') {
          let body = '';
          req.on('data', (chunk: any) => { body += chunk; });
          req.on('end', () => {
            try {
              const updates = JSON.parse(body);
              const allowed: (keyof typeof runtimeConfig)[] = [
                'totalFunds', 'batchSize', 'numWorkers', 'maxConcurrent',
                'staggerMs', 'maxRetries', 'baseDelayMs', 'maxTokens', 'cacheTtlMinutes',
                'rateLimitTpm',
              ];
              for (const key of allowed) {
                if (key in updates && typeof updates[key] === 'number' && updates[key] > 0) {
                  (runtimeConfig as any)[key] = updates[key];
                }
              }
              // Recalcular workers si cambia batchSize o totalFunds
              if ('batchSize' in updates || 'totalFunds' in updates) {
                runtimeConfig.numWorkers = Math.ceil(runtimeConfig.totalFunds / runtimeConfig.batchSize);
              }
              // Recrear limiter si cambia TPM
              if ('rateLimitTpm' in updates) {
                sharedLimiter = null;
              }
              console.log('[LLM] Config actualizada:', runtimeConfig);
              res.end(JSON.stringify({ ok: true, config: { ...runtimeConfig } }));
            } catch (err) {
              res.statusCode = 400;
              res.end(JSON.stringify({ error: (err as Error).message }));
            }
          });
          return;
        }

        res.statusCode = 405;
        res.end(JSON.stringify({ error: 'Method not allowed' }));
      });

      // ── Providers management endpoint ────────────────────────────────
      server.middlewares.use('/api/llm/providers', async (req, res) => {
        res.setHeader('Content-Type', 'application/json');

        if (req.method === 'PATCH') {
          // Toggle enabled state: { name: string, enabled: boolean }
          let body = '';
          req.on('data', (chunk: any) => { body += chunk; });
          req.on('end', () => {
            try {
              const { name, enabled } = JSON.parse(body);
              const provider = LLM_PROVIDERS.find(p => p.name === name);
              if (!provider) {
                res.statusCode = 404;
                res.end(JSON.stringify({ error: `Provider '${name}' not found` }));
                return;
              }
              provider.enabled = !!enabled;
              console.log(`[LLM] Provider ${name} ${enabled ? 'enabled' : 'disabled'}`);
              saveProviderState();
              res.end(JSON.stringify({ ok: true, name, enabled: provider.enabled }));
            } catch (err) {
              res.statusCode = 400;
              res.end(JSON.stringify({ error: (err as Error).message }));
            }
          });
          return;
        }

        if (req.method === 'POST') {
          // Add new provider: { name, type: 'azure'|'claude', endpoint?, deployment?, model?, apiKey? }
          let body = '';
          req.on('data', (chunk: any) => { body += chunk; });
          req.on('end', () => {
            try {
              const { name, type, endpoint, deployment, model, apiKey } = JSON.parse(body);
              if (!name || !type) {
                res.statusCode = 400;
                res.end(JSON.stringify({ error: 'name and type are required' }));
                return;
              }
              if (LLM_PROVIDERS.find(p => p.name === name)) {
                res.statusCode = 409;
                res.end(JSON.stringify({ error: `Provider '${name}' already exists` }));
                return;
              }

              // Store custom env keys for this provider
              const envPrefix = `CUSTOM_${name.toUpperCase().replace(/[^A-Z0-9]/g, '_')}`;
              if (apiKey) env[`${envPrefix}_KEY`] = apiKey;
              if (endpoint) env[`${envPrefix}_ENDPOINT`] = endpoint;

              const newProvider: LLMProvider = type === 'claude'
                ? {
                    name,
                    call: (_env, sys, usr, max) => {
                      const ep = endpoint || env.CLAUDE_ENDPOINT;
                      const mdl = model || 'claude-opus-4-7';
                      const key = apiKey || env.CLAUDE_API_KEY;
                      if (!key || !ep) throw new Error(`${name} config incompleta`);
                      // Reuse callClaude logic inline
                      return callClaude({ ...env, CLAUDE_API_KEY: key, CLAUDE_ENDPOINT: ep, CLAUDE_MODEL1: mdl }, sys, usr, max);
                    },
                    available: () => !!(apiKey || env.CLAUDE_API_KEY),
                    enabled: true,
                  }
                : {
                    name,
                    call: (_env, sys, usr, max) => {
                      const ep = endpoint || env.AZURE_OPENAI_ENDPOINT0;
                      const dep = deployment || 'gpt-4o';
                      const key = apiKey || env.AZURE_OPENAI_API_KEY0;
                      const ver = env.AZURE_OPENAI_API_VERSION0 || '2024-08-01-preview';
                      if (!key || !ep) throw new Error(`${name} config incompleta`);
                      return callAzureOpenAI({ ...env, AZURE_OPENAI_API_KEY0: key, AZURE_OPENAI_ENDPOINT0: ep, AZURE_OPENAI_DEPLOYMENT0: dep, AZURE_OPENAI_API_VERSION0: ver }, sys, usr, max, dep);
                    },
                    available: () => !!(apiKey || env.AZURE_OPENAI_API_KEY0),
                    enabled: true,
                  };

              LLM_PROVIDERS.push(newProvider);
              // Remove from deleted list if re-adding
              deletedProviders = deletedProviders.filter(n => n !== name);
              console.log(`[LLM] Provider added: ${name} (${type})`);
              saveProviderState();
              res.end(JSON.stringify({ ok: true, name, type }));
            } catch (err) {
              res.statusCode = 400;
              res.end(JSON.stringify({ error: (err as Error).message }));
            }
          });
          return;
        }

        if (req.method === 'DELETE') {
          // Remove provider: { name: string }
          let body = '';
          req.on('data', (chunk: any) => { body += chunk; });
          req.on('end', () => {
            try {
              const { name } = JSON.parse(body);
              const idx = LLM_PROVIDERS.findIndex(p => p.name === name);
              if (idx === -1) {
                res.statusCode = 404;
                res.end(JSON.stringify({ error: `Provider '${name}' not found` }));
                return;
              }
              LLM_PROVIDERS.splice(idx, 1);
              // Track deletion so it persists across restarts
              if (!deletedProviders.includes(name)) {
                deletedProviders.push(name);
              }
              console.log(`[LLM] Provider removed: ${name}`);
              saveProviderState();
              res.end(JSON.stringify({ ok: true, name }));
            } catch (err) {
              res.statusCode = 400;
              res.end(JSON.stringify({ error: (err as Error).message }));
            }
          });
          return;
        }

        res.statusCode = 405;
        res.end(JSON.stringify({ error: 'Use PATCH (toggle), POST (add), DELETE (remove)' }));
      });

      // ── Reload endpoint (invalida caché y relanza workers) ────────────
      server.middlewares.use('/api/llm/reload', async (req, res) => {
        if (req.method !== 'POST') {
          res.statusCode = 405;
          res.setHeader('Content-Type', 'application/json');
          res.end(JSON.stringify({ error: 'POST only' }));
          return;
        }

        console.log('[LLM] Force reload solicitado - invalidando caché y stats');
        fundCache = null;
        resetProviderStats();
        res.setHeader('Content-Type', 'application/json');
        res.end(JSON.stringify({ ok: true, message: 'Caché invalidada. Reconecta al stream para recargar.' }));
      });

      // ── Fund lookup endpoint ──────────────────────────────────────────
      server.middlewares.use('/api/llm/lookup', async (req, res) => {
        res.setHeader('Content-Type', 'application/json');

        const url = new URL(req.url || '', 'http://localhost');
        const query = url.searchParams.get('q')?.trim();

        if (!query) {
          res.statusCode = 400;
          res.end(JSON.stringify({ error: 'Missing ?q= parameter' }));
          return;
        }

        console.log(`[LLM] Lookup: "${query}"`);

        try {
          // Force GPT-5.5 for single fund lookup (most reliable)
          const result = await callAzureOpenAI(env, SYSTEM_PROMPT, lookupPrompt(query), 3000);
          // Track stats for direct call
          const stats = getProviderStats('GPT-5.5');
          stats.calls++;
          stats.promptTokens += result.usage.promptTokens;
          stats.completionTokens += result.usage.completionTokens;
          stats.totalTokens += result.usage.totalTokens;

          const json = extractJSON(result.content);
          const parsed = JSON.parse(json);

          if (parsed.error) {
            res.statusCode = 404;
            res.end(JSON.stringify({ error: parsed.error }));
            return;
          }

          const normalized = normalizeFund(parsed, 0);
          console.log(`[LLM] Found: ${normalized.name} (${normalized.isin})`);
          res.end(JSON.stringify(normalized));
        } catch (err) {
          // Fallback: try the full LLM chain
          console.warn('[LLM] Lookup GPT-5.5 failed, trying chain:', (err as Error).message);
          try {
            const raw = await callLLM(env, SYSTEM_PROMPT, lookupPrompt(query), 3000);
            const json = extractJSON(raw);
            const parsed = JSON.parse(json);

            if (parsed.error) {
              res.statusCode = 404;
              res.end(JSON.stringify({ error: parsed.error }));
              return;
            }

            const normalized = normalizeFund(parsed, 0);
            console.log(`[LLM] Found (fallback): ${normalized.name} (${normalized.isin})`);
            res.end(JSON.stringify(normalized));
          } catch (err2) {
            console.error('[LLM] Lookup error:', (err2 as Error).message);
            res.statusCode = 500;
            res.end(JSON.stringify({ error: (err2 as Error).message }));
          }
        }
      });
    },
  };
}
