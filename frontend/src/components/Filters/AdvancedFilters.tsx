import { FundRating } from '../../types/fund';
import { useFilterStore } from '../../store/filterStore';
import type { FundSortBy } from '../../store/filterStore';

const ratingOptions = [
  { value: 0, label: 'Cualquier rating' },
  { value: FundRating.ThreeStars, label: '★★★ o más' },
  { value: FundRating.FourStars, label: '★★★★ o más' },
  { value: FundRating.FiveStars, label: '★★★★★' },
];

const sortOptions: { value: FundSortBy; label: string }[] = [
  { value: 'Ranking', label: 'Ranking' },
  { value: 'Name', label: 'Nombre' },
  { value: 'Return1Year', label: 'Rentabilidad 1A' },
  { value: 'ExpenseRatio', label: 'TER (Costes)' },
  { value: 'Rating', label: 'Rating' },
  { value: 'Volatility', label: 'Volatilidad' },
  { value: 'NetAssetValue', label: 'Valor liquidativo' },
];

export default function AdvancedFilters() {
  const filters = useFilterStore((s) => s.filters);
  const setFilter = useFilterStore((s) => s.setFilter);
  const resetFilters = useFilterStore((s) => s.resetFilters);
  const hasActive = useFilterStore((s) => s.hasActiveFilters());

  return (
    <div className="flex flex-wrap items-center gap-2 text-sm">
      {/* Rating mínimo */}
      <select
        value={filters.minRating ?? 0}
        onChange={(e) => {
          const v = Number(e.target.value);
          setFilter('minRating', v === 0 ? null : (v as FundRating));
        }}
        className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:ring-2 focus:ring-primary-500 transition-colors"
      >
        {ratingOptions.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>

      {/* TER máximo */}
      <input
        type="number"
        step="0.1"
        min="0"
        max="5"
        placeholder="TER máx. %"
        value={filters.maxExpenseRatio ?? ''}
        onChange={(e) => setFilter('maxExpenseRatio', e.target.value ? Number(e.target.value) : null)}
        className="w-28 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:ring-2 focus:ring-primary-500 transition-colors"
      />

      {/* Rentabilidad mínima 1A */}
      <input
        type="number"
        step="1"
        placeholder="Rent. 1A mín. %"
        value={filters.minReturn1Year ?? ''}
        onChange={(e) => setFilter('minReturn1Year', e.target.value ? Number(e.target.value) : null)}
        className="w-32 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:ring-2 focus:ring-primary-500 transition-colors"
      />

      {/* Ordenar por */}
      <div className="flex items-center gap-1">
        <select
          value={filters.sortBy}
          onChange={(e) => setFilter('sortBy', e.target.value as FundSortBy)}
          className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:ring-2 focus:ring-primary-500 transition-colors"
        >
          {sortOptions.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        <button
          onClick={() => setFilter('sortDesc', !filters.sortDesc)}
          className="p-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
          title={filters.sortDesc ? 'Descendente' : 'Ascendente'}
        >
          {filters.sortDesc ? '↓' : '↑'}
        </button>
      </div>

      {/* Limpiar filtros */}
      {hasActive && (
        <button
          onClick={resetFilters}
          className="px-3 py-2 rounded-lg text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 border border-red-200 dark:border-red-800 transition-colors"
        >
          Limpiar filtros
        </button>
      )}
    </div>
  );
}
