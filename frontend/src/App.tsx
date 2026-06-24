import { useSignalR } from './hooks/useSignalR';
import { useTopFunds } from './hooks/useFunds';
import { useFundStore } from './store/fundStore';
import { useEffect } from 'react';
import FundTable from './components/Dashboard/FundTable';
import Header from './components/Layout/Header';
import RiskFilter from './components/Filters/RiskFilter';

function App() {
  useSignalR();
  const { data: funds, isLoading, refetch } = useTopFunds();
  const { setFunds, refreshNeeded, setRefreshNeeded } = useFundStore();

  useEffect(() => {
    if (funds) setFunds(funds);
  }, [funds, setFunds]);

  useEffect(() => {
    if (refreshNeeded) {
      refetch();
      setRefreshNeeded(false);
    }
  }, [refreshNeeded, refetch, setRefreshNeeded]);

  return (
    <div className="min-h-screen bg-gray-50">
      <Header />
      <main className="max-w-7xl mx-auto px-4 py-6">
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900">
            Top 100 Fondos de Inversión
          </h2>
          <RiskFilter />
        </div>

        {isLoading ? (
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
          </div>
        ) : (
          <FundTable funds={funds || []} />
        )}
      </main>
    </div>
  );
}

export default App;
