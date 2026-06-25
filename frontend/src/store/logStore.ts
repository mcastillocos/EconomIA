import { create } from 'zustand';

export type LogLevel = 'info' | 'warn' | 'error' | 'debug' | 'success';
export type LogSource = 'LLM' | 'API' | 'SignalR' | 'App' | 'Theme' | 'System';

export interface LogEntry {
  id: number;
  timestamp: Date;
  level: LogLevel;
  source: LogSource;
  message: string;
}

interface LogStore {
  entries: LogEntry[];
  nextId: number;
  maxEntries: number;
  add: (level: LogLevel, source: LogSource, message: string) => void;
  clear: () => void;
}

export const useLogStore = create<LogStore>((set) => ({
  entries: [],
  nextId: 1,
  maxEntries: 500,

  add: (level, source, message) =>
    set((state) => {
      const entry: LogEntry = {
        id: state.nextId,
        timestamp: new Date(),
        level,
        source,
        message,
      };
      const entries = [entry, ...state.entries].slice(0, state.maxEntries);
      return { entries, nextId: state.nextId + 1 };
    }),

  clear: () => set({ entries: [], nextId: 1 }),
}));

/** Shorthand logger that writes to the store */
export const appLog = {
  info: (source: LogSource, msg: string) => useLogStore.getState().add('info', source, msg),
  warn: (source: LogSource, msg: string) => useLogStore.getState().add('warn', source, msg),
  error: (source: LogSource, msg: string) => useLogStore.getState().add('error', source, msg),
  debug: (source: LogSource, msg: string) => useLogStore.getState().add('debug', source, msg),
  success: (source: LogSource, msg: string) => useLogStore.getState().add('success', source, msg),
};
