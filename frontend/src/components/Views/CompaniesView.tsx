import { useState, useEffect, useCallback } from 'react';
import { companiesApi } from '../../services/companiesApi';
import type { Company, CreateCompanyRequest } from '../../types/company';
import { appLog } from '../../store/logStore';

export function CompaniesView() {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CreateCompanyRequest>({ name: '' });

  const loadCompanies = useCallback(async () => {
    try {
      setLoading(true);
      const data = await companiesApi.getAll();
      setCompanies(data);
    } catch (e) {
      appLog.error('App', `Error cargando empresas: ${e}`);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadCompanies(); }, [loadCompanies]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingId) {
        await companiesApi.update(editingId, form);
        appLog.success('App', `Empresa actualizada: ${form.name}`);
      } else {
        await companiesApi.create(form);
        appLog.success('App', `Empresa creada: ${form.name}`);
      }
      setShowForm(false);
      setEditingId(null);
      setForm({ name: '' });
      loadCompanies();
    } catch (e) {
      appLog.error('App', `Error guardando empresa: ${e}`);
    }
  };

  const handleEdit = (company: Company) => {
    setForm({
      name: company.name,
      ticker: company.ticker,
      isin: company.isin,
      market: company.market,
      country: company.country,
      sector: company.sector,
      industry: company.industry,
      currency: company.currency,
      competitors: company.competitors,
      relevantUrls: company.relevantUrls,
      preferredSource: company.preferredSource,
      notes: company.notes,
    });
    setEditingId(company.id);
    setShowForm(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('¿Eliminar esta empresa?')) return;
    await companiesApi.delete(id);
    loadCompanies();
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Empresas</h2>
        <button
          onClick={() => { setShowForm(true); setEditingId(null); setForm({ name: '' }); }}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium"
        >
          + Nueva Empresa
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <input placeholder="Nombre *" required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} className="input-field" />
            <input placeholder="Ticker" value={form.ticker || ''} onChange={e => setForm({ ...form, ticker: e.target.value })} className="input-field" />
            <input placeholder="ISIN" value={form.isin || ''} onChange={e => setForm({ ...form, isin: e.target.value })} className="input-field" />
            <input placeholder="Mercado" value={form.market || ''} onChange={e => setForm({ ...form, market: e.target.value })} className="input-field" />
            <input placeholder="País" value={form.country || ''} onChange={e => setForm({ ...form, country: e.target.value })} className="input-field" />
            <input placeholder="Sector" value={form.sector || ''} onChange={e => setForm({ ...form, sector: e.target.value })} className="input-field" />
            <input placeholder="Industria" value={form.industry || ''} onChange={e => setForm({ ...form, industry: e.target.value })} className="input-field" />
            <input placeholder="Divisa" value={form.currency || ''} onChange={e => setForm({ ...form, currency: e.target.value })} className="input-field" />
            <input placeholder="Fuente preferida" value={form.preferredSource || ''} onChange={e => setForm({ ...form, preferredSource: e.target.value })} className="input-field" />
          </div>
          <textarea placeholder="Competidores" value={form.competitors || ''} onChange={e => setForm({ ...form, competitors: e.target.value })} className="input-field w-full" rows={2} />
          <textarea placeholder="URLs relevantes" value={form.relevantUrls || ''} onChange={e => setForm({ ...form, relevantUrls: e.target.value })} className="input-field w-full" rows={2} />
          <textarea placeholder="Notas" value={form.notes || ''} onChange={e => setForm({ ...form, notes: e.target.value })} className="input-field w-full" rows={2} />
          <div className="flex gap-2">
            <button type="submit" className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm">
              {editingId ? 'Actualizar' : 'Crear'}
            </button>
            <button type="button" onClick={() => setShowForm(false)} className="px-4 py-2 bg-gray-300 dark:bg-gray-600 text-gray-800 dark:text-gray-200 rounded-lg text-sm">
              Cancelar
            </button>
          </div>
        </form>
      )}

      {loading ? (
        <p className="text-gray-500 dark:text-gray-400">Cargando empresas...</p>
      ) : companies.length === 0 ? (
        <p className="text-gray-500 dark:text-gray-400">No hay empresas registradas. Crea la primera.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="text-xs uppercase bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300">
              <tr>
                <th className="px-3 py-2">Nombre</th>
                <th className="px-3 py-2">Ticker</th>
                <th className="px-3 py-2">Sector</th>
                <th className="px-3 py-2">País</th>
                <th className="px-3 py-2">Mercado</th>
                <th className="px-3 py-2">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {companies.map(c => (
                <tr key={c.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                  <td className="px-3 py-2 font-medium text-gray-800 dark:text-gray-200">{c.name}</td>
                  <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{c.ticker || '-'}</td>
                  <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{c.sector || '-'}</td>
                  <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{c.country || '-'}</td>
                  <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{c.market || '-'}</td>
                  <td className="px-3 py-2 space-x-2">
                    <button onClick={() => handleEdit(c)} className="text-blue-600 hover:underline text-xs">Editar</button>
                    <button onClick={() => handleDelete(c.id)} className="text-red-600 hover:underline text-xs">Eliminar</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
