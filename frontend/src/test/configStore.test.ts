import { describe, it, expect } from 'vitest';
import { useConfigStore } from '../store/configStore';

describe('configStore', () => {
  it('tiene totalFunds=100 por defecto', () => {
    expect(useConfigStore.getState().totalFunds).toBe(100);
  });

  it('setTotalFunds actualiza el valor', () => {
    useConfigStore.getState().setTotalFunds(50);
    expect(useConfigStore.getState().totalFunds).toBe(50);

    useConfigStore.getState().setTotalFunds(200);
    expect(useConfigStore.getState().totalFunds).toBe(200);

    // Reset
    useConfigStore.getState().setTotalFunds(100);
  });
});
