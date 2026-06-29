import type { Fund } from '../../types/fund';
import { RiskLevel, FundRating } from '../../types/fund';
import { useState, useMemo } from 'react';
import clsx from 'clsx';

interface Props {
  funds: Fund[];
  onSelectFund?: (fund: Fund) => void;
}

const riskLabels: Record<number, string> = {
  [RiskLevel.VeryLow]: 'Muy Bajo',
  [RiskLevel.Low]: 'Bajo',
  [RiskLevel.MediumLow]: 'Medio-Bajo',
  [RiskLevel.Medium]: 'Medio',
  [RiskLevel.MediumHigh]: 'Medio-Alto',
  [RiskLevel.High]: 'Alto',
  [RiskLevel.VeryHigh]: 'Muy Alto',
};

const riskColors: Record<number, string> = {
  [RiskLevel.VeryLow]: 'bg-green-100 text-green-800',
  [RiskLevel.Low]: 'bg-green-50 text-green-700',
  [RiskLevel.MediumLow]: 'bg-yellow-50 text-yellow-700',
  [RiskLevel.Medium]: 'bg-yellow-100 text-yellow-800',
  [RiskLevel.MediumHigh]: 'bg-orange-100 text-orange-800',
  [RiskLevel.High]: 'bg-red-50 text-red-700',
  [RiskLevel.VeryHigh]: 'bg-red-100 text-red-800',
};

function RatingStars({ rating }: { rating: FundRating }) {
  return (
    <span className="text-yellow-500">
      {'★'.repeat(rating)}{'☆'.repeat(5 - rating)}
    </span>
  );
}

export default function FundTable({ funds, onSelectFund }: Props) {
  const [isinFilter, setIsinFilter] = useState('');

  const filtered = useMemo(() => {
    if (!isinFilter.trim()) return funds;
    const q = isinFilter.trim().toUpperCase();
    return funds.filter((f) => f.isin.toUpperCase().includes(q));
  }, [funds, isinFilter]);

  return (
    <div className="bg-white dark:bg-[#2a2a2a] rounded-lg shadow overflow-hidden transition-colors duration-300">
      {/* ISIN Filter */}
      <div className="px-3 py-2 md:px-4 md:py-3 border-b border-gray-200 dark:border-gray-700/50 flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3">
        <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase whitespace-nowrap">
          Filtrar ISIN
        </label>
        <input
          type="text"
          placeholder="Ej: IE00B4L5Y983"
          value={isinFilter}
          onChange={(e) => setIsinFilter(e.target.value)}
          className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#1e1e1e] text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 w-full sm:w-52"
        />
        {isinFilter && (
          <span className="text-xs text-gray-400 dark:text-gray-500">
            {filtered.length} de {funds.length} fondos
          </span>
        )}
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700/50">
          <thead className="bg-primary-50 dark:bg-primary-900/40">
            <tr>
              <th className="px-3 md:px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">#</th>
              <th className="px-3 md:px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Fondo</th>
              <th className="px-3 md:px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden sm:table-cell">ISIN</th>
              <th className="px-3 md:px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Categoría</th>
              <th className="px-3 md:px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase whitespace-nowrap min-w-[120px] hidden lg:table-cell">Riesgo</th>
              <th className="px-3 md:px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden md:table-cell">NAV</th>
              <th className="px-3 md:px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Rent. 1A</th>
              <th className="px-3 md:px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden md:table-cell">Calificación</th>
              <th className="px-3 md:px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden sm:table-cell">TER</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
            {filtered.map((fund, idx) => (
              <tr
                key={`${fund.id}-${idx}`}
                onClick={() => onSelectFund?.(fund)}
                className="hover:bg-blue-50 dark:hover:bg-primary-900/20 transition-colors cursor-pointer"
              >
                <td className="px-3 md:px-4 py-3 text-sm font-medium text-gray-900 dark:text-gray-100">
                  {fund.rankingPosition}
                </td>
                <td className="px-3 md:px-4 py-3">
                  <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{fund.name}</div>
                  <div className="text-xs text-gray-500 dark:text-gray-500">{fund.managementCompany}</div>
                  <div className="text-xs text-gray-400 font-mono sm:hidden">{fund.isin}</div>
                </td>
                <td className="px-3 md:px-4 py-3 text-sm font-mono text-gray-600 dark:text-gray-400 whitespace-nowrap hidden sm:table-cell">
                  {fund.isin}
                </td>
                <td className="px-3 md:px-4 py-3 text-sm text-gray-600 dark:text-gray-400 hidden lg:table-cell">{fund.category}</td>
                <td className="px-3 md:px-4 py-3 whitespace-nowrap hidden lg:table-cell">
                  <span className={clsx('px-2 py-0.5 text-xs rounded-full font-medium', riskColors[fund.riskLevel])}>
                    {riskLabels[fund.riskLevel]}
                  </span>
                </td>
                <td className="px-3 md:px-4 py-3 text-sm text-right font-mono text-gray-900 dark:text-gray-200 hidden md:table-cell">
                  {fund.netAssetValue.toFixed(2)} {fund.currency}
                </td>
                <td className="px-3 md:px-4 py-3 text-sm text-right font-mono">
                  {fund.latestPerformance ? (
                    <span className={clsx(
                      fund.latestPerformance.return1Year >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'
                    )}>
                      {fund.latestPerformance.return1Year >= 0 ? '+' : ''}
                      {fund.latestPerformance.return1Year.toFixed(2)}%
                    </span>
                  ) : '—'}
                </td>
                <td className="px-3 md:px-4 py-3 text-center text-sm hidden md:table-cell">
                  <RatingStars rating={fund.rating} />
                </td>
                <td className="px-3 md:px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400 hidden sm:table-cell">
                  {fund.expenseRatio.toFixed(2)}%
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
