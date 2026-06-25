import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ConfigView } from '../components/Views/ConfigView';

const mockConfig = {
  config: {
    totalFunds: 100,
    batchSize: 10,
    numWorkers: 10,
    maxConcurrent: 3,
    staggerMs: 200,
    maxRetries: 2,
    baseDelayMs: 800,
    maxTokens: 4000,
    cacheTtlMinutes: 10,
  },
  providers: [
    { name: 'GPT-5.5', available: true },
    { name: 'GPT-5.4', available: true },
    { name: 'Claude', available: false },
  ],
  cache: {
    funds: 88,
    ageMinutes: 3.2,
    ttlMinutes: 10,
    fresh: true,
  },
};

describe('ConfigView', () => {
  const onReload = vi.fn();

  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    globalThis.fetch = vi.fn() as any;
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  function mockFetch(data: any, ok = true) {
    (globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok,
      json: async () => data,
    });
  }

  it('muestra los proveedores LLM con su estado', async () => {
    mockFetch(mockConfig);
    render(<ConfigView onReload={onReload} />);

    await waitFor(() => {
      expect(screen.getByText('GPT-5.5')).toBeInTheDocument();
    });
    expect(screen.getByText('GPT-5.4')).toBeInTheDocument();
    expect(screen.getByText('Claude')).toBeInTheDocument();
  });

  it('muestra el estado del caché', async () => {
    mockFetch(mockConfig);
    render(<ConfigView onReload={onReload} />);

    await waitFor(() => {
      expect(screen.getByText('88')).toBeInTheDocument();
    });
    expect(screen.getByText('3.2 min')).toBeInTheDocument();
    expect(screen.getByText('✅ Fresco')).toBeInTheDocument();
  });

  it('muestra sin caché cuando cache es null', async () => {
    mockFetch({ ...mockConfig, cache: null });
    render(<ConfigView onReload={onReload} />);

    await waitFor(() => {
      expect(screen.getByText(/sin caché/i)).toBeInTheDocument();
    });
  });

  it('muestra los campos de configuración con valores correctos', async () => {
    mockFetch(mockConfig);
    render(<ConfigView onReload={onReload} />);

    await waitFor(() => {
      expect(screen.getByText('Configuración LLM')).toBeInTheDocument();
    });

    const totalInput = screen.getByLabelText('Total fondos') as HTMLInputElement;
    expect(totalInput.value).toBe('100');

    const batchInput = screen.getByLabelText('Batch size') as HTMLInputElement;
    expect(batchInput.value).toBe('10');
  });

  it('llama a POST /api/llm/config al guardar', async () => {
    (globalThis.fetch as ReturnType<typeof vi.fn>)
      .mockResolvedValueOnce({ ok: true, json: async () => mockConfig })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ ok: true, config: mockConfig.config }) });

    render(<ConfigView onReload={onReload} />);

    await waitFor(() => {
      expect(screen.getByText('Guardar')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Guardar'));

    await waitFor(() => {
      expect(screen.getByText('Configuración guardada')).toBeInTheDocument();
    });

    expect(globalThis.fetch).toHaveBeenCalledWith('/api/llm/config', expect.objectContaining({
      method: 'POST',
    }));
  });

  it('llama a POST /api/llm/reload y ejecuta onReload al forzar recarga', async () => {
    (globalThis.fetch as ReturnType<typeof vi.fn>)
      .mockResolvedValueOnce({ ok: true, json: async () => mockConfig })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ ok: true, message: 'OK' }) })
      .mockResolvedValue({ ok: true, json: async () => mockConfig });

    render(<ConfigView onReload={onReload} />);

    await waitFor(() => {
      expect(screen.getByText('Forzar recarga')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Forzar recarga'));

    await waitFor(() => {
      expect(screen.getByText(/caché invalidada/i)).toBeInTheDocument();
    });

    expect(globalThis.fetch).toHaveBeenCalledWith('/api/llm/reload', { method: 'POST' });
    expect(onReload).toHaveBeenCalled();
  });

  it('permite modificar un campo numérico', async () => {
    mockFetch(mockConfig);
    render(<ConfigView onReload={onReload} />);

    await waitFor(() => {
      expect(screen.getByLabelText('Max concurrente')).toBeInTheDocument();
    });

    const input = screen.getByLabelText('Max concurrente') as HTMLInputElement;
    fireEvent.change(input, { target: { value: '5' } });
    expect(input.value).toBe('5');
  });
});
