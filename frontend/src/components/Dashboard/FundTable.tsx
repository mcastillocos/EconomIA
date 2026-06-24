import type { Fund } from '../../types/fund';
import { RiskLevel, FundRating } from '../../types/fund';
import clsx from 'clsx';

interface Props {
  funds: Fund[];
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

export default function FundTable({ funds }: Props) {
  return (
    <div className="bg-white rounded-lg shadow overflow-hidden">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">#</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Fondo</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Categoría</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Riesgo</th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">NAV</th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">1Y Return</th>
              <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Rating</th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">TER</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {funds.map((fund) => (
              <tr key={fund.id} className="hover:bg-blue-50 transition-colors cursor-pointer">
                <td className="px-4 py-3 text-sm font-medium text-gray-900">
                  {fund.rankingPosition}
                </td>
                <td className="px-4 py-3">
                  <div className="text-sm font-medium text-gray-900">{fund.name}</div>
                  <div className="text-xs text-gray-500">{fund.isin} · {fund.managementCompany}</div>
                </td>
                <td className="px-4 py-3 text-sm text-gray-600">{fund.category}</td>
                <td className="px-4 py-3">
                  <span className={clsx('px-2 py-0.5 text-xs rounded-full font-medium', riskColors[fund.riskLevel])}>
                    {riskLabels[fund.riskLevel]}
                  </span>
                </td>
                <td className="px-4 py-3 text-sm text-right font-mono">
                  {fund.netAssetValue.toFixed(2)} {fund.currency}
                </td>
                <td className="px-4 py-3 text-sm text-right font-mono">
                  {fund.latestPerformance ? (
                    <span className={clsx(
                      fund.latestPerformance.return1Year >= 0 ? 'text-green-600' : 'text-red-600'
                    )}>
                      {fund.latestPerformance.return1Year >= 0 ? '+' : ''}
                      {fund.latestPerformance.return1Year.toFixed(2)}%
                    </span>
                  ) : '—'}
                </td>
                <td className="px-4 py-3 text-center text-sm">
                  <RatingStars rating={fund.rating} />
                </td>
                <td className="px-4 py-3 text-sm text-right text-gray-600">
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
