import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { TokenBucketLimiter, estimateTokens } from '../../server/tokenBucketLimiter';

describe('TokenBucketLimiter', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('permite reservas dentro del threshold (90%)', async () => {
    const limiter = new TokenBucketLimiter(1000); // threshold = 900
    const id = await limiter.reserve(500);
    expect(id).toBe(1);

    const stats = limiter.stats();
    expect(stats.tpm).toBe(1000);
    expect(stats.threshold).toBe(900);
    expect(stats.currentUsage).toBe(500);
    expect(stats.activeReservations).toBe(1);
  });

  it('permite múltiples reservas que suman < threshold', async () => {
    const limiter = new TokenBucketLimiter(1000); // threshold = 900
    await limiter.reserve(300);
    await limiter.reserve(300);
    await limiter.reserve(200);

    const stats = limiter.stats();
    expect(stats.currentUsage).toBe(800);
    expect(stats.activeReservations).toBe(3);
  });

  it('confirm ajusta los tokens reales de la reserva', async () => {
    const limiter = new TokenBucketLimiter(1000);
    const id = await limiter.reserve(500);

    expect(limiter.stats().currentUsage).toBe(500);

    limiter.confirm(id, 200); // usó menos
    expect(limiter.stats().currentUsage).toBe(200);
  });

  it('release libera la reserva completa', async () => {
    const limiter = new TokenBucketLimiter(1000);
    const id = await limiter.reserve(500);

    expect(limiter.stats().currentUsage).toBe(500);

    limiter.release(id);
    expect(limiter.stats().currentUsage).toBe(0);
    expect(limiter.stats().activeReservations).toBe(0);
  });

  it('permite petición grande si la ventana está vacía (evita deadlock)', async () => {
    const limiter = new TokenBucketLimiter(100); // threshold = 90
    // 200 > 90 pero la ventana está vacía → pasa
    const id = await limiter.reserve(200);
    expect(id).toBe(1);
  });

  it('lanza timeout si espera demasiado', async () => {
    const limiter = new TokenBucketLimiter(100, 500); // maxWait = 500ms

    // Llena la ventana
    await limiter.reserve(90);

    // Esta debería hacer timeout intentando reservar más
    await expect(limiter.reserve(20)).rejects.toThrow(/timeout/i);
  });

  it('stats devuelve info correcta', () => {
    const limiter = new TokenBucketLimiter(500_000);
    const stats = limiter.stats();

    expect(stats.tpm).toBe(500_000);
    expect(stats.threshold).toBe(450_000);
    expect(stats.currentUsage).toBe(0);
    expect(stats.activeReservations).toBe(0);
  });
});

describe('estimateTokens', () => {
  it('estima ~4 chars por token', () => {
    expect(estimateTokens('hola')).toBe(1);
    expect(estimateTokens('hola mundo')).toBe(3);
    expect(estimateTokens('a'.repeat(100))).toBe(25);
  });

  it('devuelve al menos 1 para texto corto', () => {
    expect(estimateTokens('hi')).toBe(1);
    expect(estimateTokens('')).toBe(0);
  });
});
