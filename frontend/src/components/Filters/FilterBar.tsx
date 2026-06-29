import { useState } from 'react';
import RiskFilter from './RiskFilter';
import CategoryFilter from './CategoryFilter';
import SearchFilter from './SearchFilter';
import AdvancedFilters from './AdvancedFilters';
import { useFilterStore } from '../../store/filterStore';

export default function FilterBar() {
  const [showAdvanced, setShowAdvanced] = useState(false);
  const hasActive = useFilterStore((s) => s.hasActiveFilters());

  return (
    <div className="space-y-3">
      {/* Fila principal */}
      <div className="flex flex-wrap items-center gap-2">
        <SearchFilter />
        <RiskFilter />
        <CategoryFilter />
        <button
          onClick={() => setShowAdvanced(!showAdvanced)}
          className={`px-3 py-2 rounded-lg text-sm border transition-colors ${
            showAdvanced || hasActive
              ? 'border-primary-500 text-primary-600 dark:text-primary-400 bg-primary-50 dark:bg-primary-900/20'
              : 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
          }`}
        >
          {showAdvanced ? '▲ Menos filtros' : '▼ Más filtros'}
          {hasActive && !showAdvanced && (
            <span className="ml-1.5 inline-flex items-center justify-center w-2 h-2 rounded-full bg-primary-500" />
          )}
        </button>
      </div>

      {/* Filtros avanzados */}
      {showAdvanced && (
        <div className="pt-2 border-t border-gray-200 dark:border-gray-700">
          <AdvancedFilters />
        </div>
      )}
    </div>
  );
}
