import { RiskLevel } from '../../types/fund';
import { useFilterStore } from '../../store/filterStore';

const riskOptions = [
  { value: 0, label: 'Todos los riesgos' },
  { value: RiskLevel.VeryLow, label: '1 — Muy Bajo' },
  { value: RiskLevel.Low, label: '2 — Bajo' },
  { value: RiskLevel.MediumLow, label: '3 — Medio-Bajo' },
  { value: RiskLevel.Medium, label: '4 — Medio' },
  { value: RiskLevel.MediumHigh, label: '5 — Medio-Alto' },
  { value: RiskLevel.High, label: '6 — Alto' },
  { value: RiskLevel.VeryHigh, label: '7 — Muy Alto' },
];

export default function RiskFilter() {
  const riskLevel = useFilterStore((s) => s.filters.riskLevel);
  const setFilter = useFilterStore((s) => s.setFilter);

  return (
    <select
      value={riskLevel ?? 0}
      onChange={(e) => {
        const val = Number(e.target.value);
        setFilter('riskLevel', val === 0 ? null : (val as RiskLevel));
      }}
      className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#2a2a2a] text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors"
    >
      {riskOptions.map((opt) => (
        <option key={opt.value} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
  );
}
