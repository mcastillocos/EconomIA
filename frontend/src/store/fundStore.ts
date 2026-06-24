import { create } from 'zustand';
import type { Fund } from '../types/fund';

interface FundStore {
  funds: Fund[];
  selectedFund: Fund | null;
  refreshNeeded: boolean;
  setFunds: (funds: Fund[]) => void;
  setSelectedFund: (fund: Fund | null) => void;
  updateFundPrice: (fundId: string, price: number, currency: string) => void;
  setRefreshNeeded: (needed: boolean) => void;
}

export const useFundStore = create<FundStore>((set) => ({
  funds: [],
  selectedFund: null,
  refreshNeeded: false,

  setFunds: (funds) => set({ funds }),

  setSelectedFund: (fund) => set({ selectedFund: fund }),

  updateFundPrice: (fundId, price, _currency) =>
    set((state) => ({
      funds: state.funds.map((f) =>
        f.id === fundId
          ? { ...f, netAssetValue: price, lastUpdated: new Date().toISOString() }
          : f
      ),
    })),

  setRefreshNeeded: (needed) => set({ refreshNeeded: needed }),
}));
