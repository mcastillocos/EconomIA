import { useState } from 'react';
import type { Fund } from '../../types/fund';
import FundTable from '../Dashboard/FundTable';
import FundDetailModal from '../Dashboard/FundDetailModal';
import WhyTheBest from '../Dashboard/WhyTheBest';
import RiskFilter from '../Filters/RiskFilter';
import PerformanceCharts from '../Charts/PerformanceCharts';
import { useConfigStore } from '../../store/configStore';

interface Props {
  funds: Fund[];
  isLoading: boolean;
}

export function GlobalView({ funds, isLoading }: Props) {
  const [selectedFund, setSelectedFund] = useState<Fund | null>(null);
  const totalFunds = useConfigStore((s) => s.totalFunds);

  return (
    <>
      <div className="mb-6 flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
          Top {totalFunds} Fondos de Inversión
        </h2>
        <RiskFilter />
      </div>

      <WhyTheBest funds={funds} />
      <PerformanceCharts funds={funds} />
      {isLoading ? <LoadingSpinner /> : <FundTable funds={funds} onSelectFund={setSelectedFund} />}
      {selectedFund && <FundDetailModal fund={selectedFund} onClose={() => setSelectedFund(null)} />}
    </>
  );
}

export function DatosView({ funds, isLoading }: Props) {
  const [selectedFund, setSelectedFund] = useState<Fund | null>(null);

  return (
    <>
      <div className="mb-6 flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
          Datos — Tabla de Fondos
        </h2>
        <RiskFilter />
      </div>

      <WhyTheBest funds={funds} />
      {isLoading ? <LoadingSpinner /> : <FundTable funds={funds} onSelectFund={setSelectedFund} />}
      {selectedFund && <FundDetailModal fund={selectedFund} onClose={() => setSelectedFund(null)} />}
    </>
  );
}

export function GraficasView({ funds }: { funds: Fund[] }) {
  return (
    <>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
          Gráficas de Rendimiento
        </h2>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
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
