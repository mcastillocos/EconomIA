/**
 * Token-bucket rate limiter por tokens/minuto (TPM).
 * Inspirado en el patrón de CobolForge: reserva presupuesto ANTES de cada
 * llamada, espera si no cabe, y libera si falla.
 * Throttle al 90% de la cuota real para dejar colchón.
 */

interface Reservation {
  id: number;
  tokens: number;
  ts: number;
}

export class TokenBucketLimiter {
  private readonly tpm: number;
  private readonly threshold: number;
  private readonly maxWaitMs: number;
  private window: Reservation[] = [];
  private seq = 0;

  constructor(tokensPerMinute: number, maxWaitMs = 120_000) {
    this.tpm = tokensPerMinute;
    this.threshold = Math.floor(tokensPerMinute * 0.9); // 90% colchón
    this.maxWaitMs = maxWaitMs;
  }

  private prune(now: number) {
    const cutoff = now - 60_000;
    this.window = this.window.filter((r) => r.ts > cutoff);
  }

  private usage(now: number): number {
    this.prune(now);
    return this.window.reduce((s, r) => s + r.tokens, 0);
  }

  /** Espera hasta que quepan `estimatedTokens`; devuelve un id de reserva. */
  async reserve(estimatedTokens: number): Promise<number> {
    const start = Date.now();
    while (true) {
      const now = Date.now();
      const used = this.usage(now);

      // Cabe en el umbral, o la ventana está vacía y la petición es grande (evita deadlock)
      if (used + estimatedTokens <= this.threshold || (used === 0 && estimatedTokens > this.threshold)) {
        const id = ++this.seq;
        this.window.push({ id, tokens: estimatedTokens, ts: now });
        return id;
      }

      if (now - start > this.maxWaitMs) {
        throw new Error(`Rate limiter: timeout tras ${this.maxWaitMs}ms esperando cuota TPM`);
      }

      // Espera hasta que el evento más antiguo salga de la ventana
      const oldest = this.window[0]?.ts ?? now;
      const wait = Math.max(50, Math.min(1_000, oldest + 60_000 - now));
      await new Promise((r) => setTimeout(r, wait));
    }
  }

  /** Ajusta la reserva con el uso real (prompt + completion tokens). */
  confirm(id: number, actualTokens: number) {
    const r = this.window.find((x) => x.id === id);
    if (r) r.tokens = actualTokens;
  }

  /** Libera la reserva si la llamada falló (no consumió cuota). */
  release(id: number) {
    this.window = this.window.filter((x) => x.id !== id);
  }

  /** Info para debug / config endpoint */
  stats() {
    const now = Date.now();
    this.prune(now);
    return {
      tpm: this.tpm,
      threshold: this.threshold,
      currentUsage: this.usage(now),
      activeReservations: this.window.length,
    };
  }
}

/** Estima tokens de un texto: ~4 chars/token (aproximación). */
export function estimateTokens(text: string): number {
  return Math.ceil(text.length / 4);
}
