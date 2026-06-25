import { useEffect, useState } from 'react';
import { appLog } from '../store/logStore';

type ThemePreference = 'light' | 'dark' | 'system';

export function useTheme() {
  const [preference, setPreference] = useState<ThemePreference>(() => {
    if (typeof window === 'undefined') return 'system';
    return (localStorage.getItem('economia-theme') as ThemePreference) || 'system';
  });

  const [isDark, setIsDark] = useState(() => {
    if (typeof window === 'undefined') return false;
    const stored = localStorage.getItem('economia-theme');
    if (stored === 'dark') return true;
    if (stored === 'light') return false;
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  });

  // Listen to system changes when preference is 'system'
  useEffect(() => {
    if (preference !== 'system') return;

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = (e: MediaQueryListEvent) => setIsDark(e.matches);
    mediaQuery.addEventListener('change', handler);
    setIsDark(mediaQuery.matches);

    return () => mediaQuery.removeEventListener('change', handler);
  }, [preference]);

  // Apply class to document
  useEffect(() => {
    if (isDark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }, [isDark]);

  const toggle = () => {
    const next = isDark ? 'light' : 'dark';
    setPreference(next);
    setIsDark(next === 'dark');
    localStorage.setItem('economia-theme', next);
    appLog.info('Theme', `Tema cambiado a ${next}`);
  };

  const setSystemPreference = () => {
    setPreference('system');
    localStorage.removeItem('economia-theme');
    setIsDark(window.matchMedia('(prefers-color-scheme: dark)').matches);
  };

  return { isDark, toggle, preference, setSystemPreference };
}
