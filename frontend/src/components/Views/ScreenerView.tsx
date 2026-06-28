export function ScreenerView() {
  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Screener IA</h2>
      <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-6 text-center">
        <div className="text-4xl mb-3">🔍</div>
        <h3 className="text-lg font-medium text-gray-700 dark:text-gray-200 mb-2">Próximamente</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400 max-w-md mx-auto">
          Filtrado inteligente de fondos y empresas por criterios cuantitativos y cualitativos.
          Ranking, explicaciones y advertencias de datos faltantes.
        </p>
        <p className="text-xs text-gray-400 dark:text-gray-500 mt-4">Disponible en MVP3 — ScreenerAgent</p>
      </div>
    </div>
  );
}
