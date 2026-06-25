import { describe, it, expect, beforeEach } from 'vitest';
import { useLogStore, appLog } from '../store/logStore';

describe('logStore', () => {
  beforeEach(() => {
    useLogStore.getState().clear();
  });

  it('should start empty', () => {
    expect(useLogStore.getState().entries).toHaveLength(0);
  });

  it('should add a log entry', () => {
    appLog.info('App', 'Test message');
    const entries = useLogStore.getState().entries;
    expect(entries).toHaveLength(1);
    expect(entries[0].level).toBe('info');
    expect(entries[0].source).toBe('App');
    expect(entries[0].message).toBe('Test message');
    expect(entries[0].timestamp).toBeInstanceOf(Date);
  });

  it('should add entries newest-first', () => {
    appLog.info('App', 'First');
    appLog.warn('LLM', 'Second');
    const entries = useLogStore.getState().entries;
    expect(entries[0].message).toBe('Second');
    expect(entries[1].message).toBe('First');
  });

  it('should support all log levels', () => {
    appLog.info('App', 'info');
    appLog.warn('App', 'warn');
    appLog.error('App', 'error');
    appLog.debug('App', 'debug');
    appLog.success('App', 'success');
    expect(useLogStore.getState().entries).toHaveLength(5);
  });

  it('should support all sources', () => {
    const sources = ['LLM', 'API', 'SignalR', 'App', 'Theme', 'System'] as const;
    sources.forEach((s) => appLog.info(s, `from ${s}`));
    const entries = useLogStore.getState().entries;
    expect(entries).toHaveLength(6);
    expect(new Set(entries.map((e) => e.source))).toEqual(new Set(sources));
  });

  it('should assign incremental IDs', () => {
    appLog.info('App', 'a');
    appLog.info('App', 'b');
    appLog.info('App', 'c');
    const ids = useLogStore.getState().entries.map((e) => e.id);
    expect(ids).toEqual([3, 2, 1]);
  });

  it('should cap entries at maxEntries', () => {
    const max = useLogStore.getState().maxEntries;
    for (let i = 0; i < max + 50; i++) {
      appLog.debug('System', `msg-${i}`);
    }
    expect(useLogStore.getState().entries).toHaveLength(max);
  });

  it('should clear all entries', () => {
    appLog.info('App', 'will be cleared');
    appLog.error('LLM', 'also cleared');
    useLogStore.getState().clear();
    expect(useLogStore.getState().entries).toHaveLength(0);
    expect(useLogStore.getState().nextId).toBe(1);
  });
});
