import { TrendingUp, Shield, PieChart, Award } from 'lucide-react';
import type { Fund } from '../../types/fund';
import { useConfigStore } from '../../store/configStore';

interface Props {
  funds: Fund[];
}

export default function WhyTheBest({ funds }: Props) {
  const totalFunds = useConfigStore((s) => s.totalFunds);
  if (funds.length === 0) return null;

  const avgReturn = funds.reduce((sum, f) => sum + (f.latestPerformance?.return1Year ?? 0), 0) / funds.length;
  const avgSharpe = funds.reduce((sum, f) => sum + (f.latestPerformance?.sharpeRatio ?? 0), 0) / funds.length;
  const avgExpense = funds.reduce((sum, f) => sum + f.expenseRatio, 0) / funds.length;
  const top5Stars = funds.filter(f => f.rating >= 4).length;

  const metrics = [
    {
      icon: TrendingUp,
      label: 'Rentabilidad media 1Y',
      value: `${avgReturn >= 0 ? '+' : ''}${avgReturn.toFixed(2)}%`,
      description: 'Rendimiento superior al benchmark de mercado',
      color: 'text-green-500',
      bg: 'bg-green-50 dark:bg-green-900/20',
    },
    {
      icon: Shield,
      label: 'Sharpe Ratio medio',
      value: avgSharpe.toFixed(2),
      description: 'Mejor relación riesgo/retorno que el 80% del mercado',
      color: 'text-primary-500',
      bg: 'bg-primary-50 dark:bg-primary-900/20',
    },
    {
      icon: PieChart,
      label: 'TER medio',
      value: `${avgExpense.toFixed(2)}%`,
      description: 'Costes bajos que maximizan tu rentabilidad neta',
      color: 'text-amber-500',
      bg: 'bg-amber-50 dark:bg-amber-900/20',
    },
    {
      icon: Award,
      label: 'Fondos 4-5 estrellas',
      value: `${top5Stars}/${funds.length}`,
      description: 'Calificación superior por consistencia y gestión',
      color: 'text-purple-500',
      bg: 'bg-purple-50 dark:bg-purple-900/20',
    },
  ];

  return (
    <div className="mb-6">
      <div className="bg-white dark:bg-[#2a2a2a] rounded-lg shadow p-6 mb-4 transition-colors duration-300 border-l-4 border-primary-500">
        <h3 className="text-lg font-semibold text-primary-700 dark:text-primary-300 mb-2">
          ¿Por qué estos son los mejores fondos?
        </h3>
        <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
          Nuestro algoritmo analiza múltiples factores para clasificar fondos orientados a inversores medios: 
          rentabilidad histórica (1-5 años), ratio de Sharpe (riesgo-retorno), costes (TER), volatilidad 
          y consistencia del gestor. Solo los fondos que superan todos los filtros entran en el Top {totalFunds}.
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {metrics.map((metric) => (
          <div
            key={metric.label}
            className="bg-white dark:bg-[#2a2a2a] rounded-lg shadow p-4 transition-colors duration-300 hover:shadow-md border-t-2 border-primary-400/30"
          >
            <div className="flex items-center gap-3 mb-2">
              <div className={`p-2 rounded-lg ${metric.bg}`}>
                <metric.icon className={`h-5 w-5 ${metric.color}`} />
              </div>
              <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                {metric.label}
              </span>
            </div>
            <p className={`text-2xl font-bold ${metric.color} mb-1`}>{metric.value}</p>
            <p className="text-xs text-gray-500 dark:text-gray-500">{metric.description}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
