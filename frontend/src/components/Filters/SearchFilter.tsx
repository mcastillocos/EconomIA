import { useState, useCallback } from 'react';
import { useFilterStore } from '../../store/filterStore';

export default function SearchFilter() {
  const search = useFilterStore((s) => s.filters.search);
  const setFilter = useFilterStore((s) => s.setFilter);
  const [local, setLocal] = useState(search ?? '');

  const handleSubmit = useCallback(() => {
    setFilter('search', local.trim() || null);
  }, [local, setFilter]);

  return (
    <div className="relative flex-1 min-w-[200px]">
      <input
        type="text"
        value={local}
        onChange={(e) => setLocal(e.target.value)}
        onKeyDown={(e) => e.key === 'Enter' && handleSubmit()}
        onBlur={handleSubmit}
        placeholder="Buscar por nombre, ISIN o gestora..."
        className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-900 dark:text-gray-100 pl-9 pr-3 py-2 text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors"
      />
      <svg
        className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
        />
      </svg>
    </div>
  );
}
