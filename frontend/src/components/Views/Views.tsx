import { useState } from 'react';
import type { Fund } from '../../types/fund';
import FundTable from '../Dashboard/FundTable';
import FundDetailModal from '../Dashboard/FundDetailModal';
import WhyTheBest from '../Dashboard/WhyTheBest';
import FilterBar from '../Filters/FilterBar';
import Pagination from '../Filters/Pagination';
import PerformanceCharts from '../Charts/PerformanceCharts';
import { useConfigStore } from '../../store/configStore';
import { useFilterStore } from '../../store/filterStore';
import { useFilteredFunds } from '../../hooks/useFilteredFunds';

interface Props {
  funds: Fund[];
  isLoading: boolean;
}

export function GlobalView({ funds, isLoading }: Props) {
  const [selectedFund, setSelectedFund] = useState<Fund | null>(null);
  const totalFunds = useConfigStore((s) => s.totalFunds);
  const hasActiveFilters = useFilterStore((s) => s.hasActiveFilters());
  const { data: filtered, isLoading: isFiltering } = useFilteredFunds();

  const displayFunds = hasActiveFilters && filtered ? filtered.items : funds;
  const loading = hasActiveFilters ? isFiltering : isLoading;

  return (
    <>
      <div className="mb-4 md:mb-6">
        <h2 className="text-xl md:text-2xl font-bold text-gray-900 dark:text-gray-100 mb-3">
          Top {totalFunds} Fondos de Inversión
        </h2>
        <FilterBar />
      </div>

      <WhyTheBest funds={displayFunds} />
      <PerformanceCharts funds={displayFunds} />
      {loading ? <LoadingSpinner /> : <FundTable funds={displayFunds} onSelectFund={setSelectedFund} />}
      {hasActiveFilters && filtered && (
        <Pagination totalCount={filtered.totalCount} totalPages={filtered.totalPages} />
      )}
      {selectedFund && <FundDetailModal fund={selectedFund} onClose={() => setSelectedFund(null)} />}
    </>
  );
}

export function DatosView({ funds, isLoading }: Props) {
  const [selectedFund, setSelectedFund] = useState<Fund | null>(null);
  const hasActiveFilters = useFilterStore((s) => s.hasActiveFilters());
  const { data: filtered, isLoading: isFiltering } = useFilteredFunds();

  const displayFunds = hasActiveFilters && filtered ? filtered.items : funds;
  const loading = hasActiveFilters ? isFiltering : isLoading;

  return (
    <>
      <div className="mb-4 md:mb-6">
        <h2 className="text-xl md:text-2xl font-bold text-gray-900 dark:text-gray-100 mb-3">
          Datos — Tabla de Fondos
        </h2>
        <FilterBar />
      </div>

      <WhyTheBest funds={displayFunds} />
      {loading ? <LoadingSpinner /> : <FundTable funds={displayFunds} onSelectFund={setSelectedFund} />}
      {hasActiveFilters && filtered && (
        <Pagination totalCount={filtered.totalCount} totalPages={filtered.totalPages} />
      )}
      {selectedFund && <FundDetailModal fund={selectedFund} onClose={() => setSelectedFund(null)} />}
    </>
  );
}

export function GraficasView({ funds }: { funds: Fund[] }) {
  return (
    <>
      <div className="mb-4 md:mb-6">
        <h2 className="text-xl md:text-2xl font-bold text-gray-900 dark:text-gray-100">
          Gráficas de Rendimiento
        </h2>
        <p className="text-xs sm:text-sm text-gray-500 dark:text-gray-400 mt-1">
          Análisis visual del comportamiento de los fondos seleccionados
        </p>
      </div>

      <PerformanceCharts funds={funds} />
    </>
  );
}

function LoadingSpinner() {
  return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500"></div>
    </div>
  );
}
