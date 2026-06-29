import { useEffect } from 'react';
import { useFilterStore } from '../../store/filterStore';
import { fundsApi } from '../../services/api';

export default function CategoryFilter() {
  const category = useFilterStore((s) => s.filters.category);
  const categories = useFilterStore((s) => s.categories);
  const setFilter = useFilterStore((s) => s.setFilter);
  const setCategories = useFilterStore((s) => s.setCategories);

  useEffect(() => {
    if (categories.length === 0) {
      fundsApi.getCategories().then(setCategories).catch(() => {});
    }
  }, [categories.length, setCategories]);

  return (
    <select
      value={category ?? ''}
      onChange={(e) => setFilter('category', e.target.value || null)}
      className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors"
    >
      <option value="">Todas las categorías</option>
      {categories.map((cat) => (
        <option key={cat} value={cat}>
          {cat}
        </option>
      ))}
    </select>
  );
}
