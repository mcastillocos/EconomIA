import { useState, useEffect, useCallback } from 'react';
import { watchlistsApi } from '../../services/watchlistsApi';
import type { Watchlist, CreateWatchlistRequest } from '../../types/watchlist';
import { appLog } from '../../store/logStore';

export function WatchlistsView() {
  const [watchlists, setWatchlists] = useState<Watchlist[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateWatchlistRequest>({ name: '' });

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const data = await watchlistsApi.getAll();
      setWatchlists(data);
    } catch (e) {
      appLog.error('App', `Error cargando watchlists: ${e}`);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await watchlistsApi.create(form);
      appLog.success('App', `Watchlist creada: ${form.name}`);
      setShowForm(false);
      setForm({ name: '' });
      load();
    } catch (e) {
      appLog.error('App', `Error creando watchlist: ${e}`);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('¿Eliminar esta watchlist?')) return;
    await watchlistsApi.delete(id);
    load();
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Carteras / Seguimiento</h2>
        <button
          onClick={() => setShowForm(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium"
        >
          + Nueva Watchlist
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3">
          <input placeholder="Nombre *" required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} className="input-field w-full" />
          <textarea placeholder="Descripción" value={form.description || ''} onChange={e => setForm({ ...form, description: e.target.value })} className="input-field w-full" rows={2} />
          <div className="flex gap-2">
            <button type="submit" className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm">Crear</button>
            <button type="button" onClick={() => setShowForm(false)} className="px-4 py-2 bg-gray-300 dark:bg-gray-600 rounded-lg text-sm">Cancelar</button>
          </div>
        </form>
      )}

      {loading ? (
        <p className="text-gray-500 dark:text-gray-400">Cargando...</p>
      ) : watchlists.length === 0 ? (
        <p className="text-gray-500 dark:text-gray-400">No hay watchlists creadas.</p>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {watchlists.map(w => (
            <div key={w.id} className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4">
              <div className="flex justify-between items-start mb-2">
                <h3 className="font-semibold text-gray-800 dark:text-gray-100">{w.name}</h3>
                <button onClick={() => handleDelete(w.id)} className="text-red-500 hover:text-red-700 text-xs">Eliminar</button>
              </div>
              {w.description && <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">{w.description}</p>}
              <div className="text-xs text-gray-400 dark:text-gray-500">
                {w.items.length} elementos · Creada {new Date(w.createdAt).toLocaleDateString('es-ES')}
              </div>
              {w.items.length > 0 && (
                <ul className="mt-2 space-y-1">
                  {w.items.slice(0, 5).map(item => (
                    <li key={item.id} className="text-xs text-gray-600 dark:text-gray-300 flex items-center gap-1">
                      <span className={`w-2 h-2 rounded-full ${item.positionType === 'real' ? 'bg-green-500' : 'bg-yellow-500'}`} />
                      {item.entityType} · {item.entityId.slice(0, 8)}...
                    </li>
                  ))}
                  {w.items.length > 5 && <li className="text-xs text-gray-400">+{w.items.length - 5} más</li>}
                </ul>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
