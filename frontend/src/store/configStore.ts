import { create } from 'zustand';

interface ConfigStore {
  totalFunds: number;
  setTotalFunds: (n: number) => void;
}

export const useConfigStore = create<ConfigStore>()((set) => ({
  totalFunds: 100,
  setTotalFunds: (n) => set({ totalFunds: n }),
}));
