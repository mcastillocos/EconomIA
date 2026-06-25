import { RiskLevel } from '../../types/fund';

const riskOptions = [
  { value: 0, label: 'Todos' },
  { value: RiskLevel.VeryLow, label: 'Muy Bajo' },
  { value: RiskLevel.Low, label: 'Bajo' },
  { value: RiskLevel.MediumLow, label: 'Medio-Bajo' },
  { value: RiskLevel.Medium, label: 'Medio' },
  { value: RiskLevel.MediumHigh, label: 'Medio-Alto' },
  { value: RiskLevel.High, label: 'Alto' },
  { value: RiskLevel.VeryHigh, label: 'Muy Alto' },
];

export default function RiskFilter() {
  return (
    <select className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors">
      {riskOptions.map((opt) => (
        <option key={opt.value} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
  );
}
