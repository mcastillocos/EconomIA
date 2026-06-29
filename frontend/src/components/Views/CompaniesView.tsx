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

  // Estado para añadir con IA
  const [showAiAdd, setShowAiAdd] = useState(false);
  const [aiQuery, setAiQuery] = useState('');
  const [aiLoading, setAiLoading] = useState(false);
  const [aiError, setAiError] = useState<string | null>(null);
  const [aiPreview, setAiPreview] = useState<CreateCompanyRequest | null>(null);
  const [aiSaving, setAiSaving] = useState(false);

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

  const handleAiAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!aiQuery.trim()) return;
    setAiLoading(true);
    setAiError(null);
    setAiPreview(null);
    try {
      const result = await companiesApi.aiLookup(aiQuery.trim());
      setAiPreview(result);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
        || 'Error buscando con IA. Comprueba que el servicio LLM esté configurado.';
      setAiError(msg);
      appLog.error('App', `Error IA: ${msg}`);
    } finally {
      setAiLoading(false);
    }
  };

  const handleAiConfirm = async () => {
    if (!aiPreview) return;
    setAiSaving(true);
    try {
      await companiesApi.create(aiPreview);
      appLog.success('App', `Empresa añadida con IA: ${aiPreview.name}`);
      setShowAiAdd(false);
      setAiPreview(null);
      setAiQuery('');
      loadCompanies();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error || 'Error guardando empresa.';
      setAiError(msg);
    } finally {
      setAiSaving(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Empresas</h2>
        <div className="flex gap-2">
          <button
            onClick={() => { setShowAiAdd(true); setAiError(null); setAiQuery(''); }}
            className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 text-sm font-medium flex items-center gap-1.5"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" /></svg>
            + Añadir con IA
          </button>
          <button
            onClick={() => { setShowForm(true); setEditingId(null); setForm({ name: '' }); }}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium"
          >
            + Nueva Empresa
          </button>
        </div>
      </div>

      {/* Añadir con IA */}
      {showAiAdd && (
        <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-700 rounded-xl p-4 space-y-3">
          <div className="flex items-center gap-2">
            <svg className="w-5 h-5 text-purple-600 dark:text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" /></svg>
            <h3 className="font-semibold text-purple-800 dark:text-purple-200">Añadir empresa con IA</h3>
          </div>
          <p className="text-sm text-purple-700 dark:text-purple-300">
            Escribe el nombre o ticker y la IA rellenará todos los datos automáticamente.
          </p>
          <form onSubmit={handleAiAdd} className="flex gap-2">
            <input
              autoFocus
              placeholder="Ej: Apple, MSFT, Inditex, Tesla..."
              value={aiQuery}
              onChange={e => setAiQuery(e.target.value)}
              className="input-field flex-1"
              disabled={aiLoading || !!aiPreview}
            />
            {!aiPreview && (
              <button
                type="submit"
                disabled={aiLoading || !aiQuery.trim()}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed min-w-[100px]"
              >
                {aiLoading ? (
                  <span className="flex items-center gap-1.5">
                    <svg className="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" /></svg>
                    Buscando...
                  </span>
                ) : 'Buscar'}
              </button>
            )}
            <button
              type="button"
              onClick={() => { setShowAiAdd(false); setAiPreview(null); setAiQuery(''); }}
              className="px-3 py-2 bg-gray-300 dark:bg-gray-600 text-gray-800 dark:text-gray-200 rounded-lg text-sm"
            >
              Cancelar
            </button>
          </form>

          {/* Previsualización de datos */}
          {aiPreview && (
            <div className="space-y-3">
              <div className="bg-white dark:bg-[#2a2a2a] rounded-lg border border-purple-200 dark:border-purple-700 p-4">
                <div className="flex items-center gap-2 mb-3">
                  <svg className="w-4 h-4 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" /></svg>
                  <span className="text-sm font-semibold text-purple-700 dark:text-purple-300">Datos encontrados por IA</span>
                </div>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-x-4 gap-y-2 text-sm">
                  <div><span className="text-gray-500 dark:text-gray-400">Nombre:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.name}</span></div>
                  {aiPreview.ticker && <div><span className="text-gray-500 dark:text-gray-400">Ticker:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.ticker}</span></div>}
                  {aiPreview.isin && <div><span className="text-gray-500 dark:text-gray-400">ISIN:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.isin}</span></div>}
                  {aiPreview.market && <div><span className="text-gray-500 dark:text-gray-400">Mercado:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.market}</span></div>}
                  {aiPreview.country && <div><span className="text-gray-500 dark:text-gray-400">País:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.country}</span></div>}
                  {aiPreview.sector && <div><span className="text-gray-500 dark:text-gray-400">Sector:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.sector}</span></div>}
                  {aiPreview.industry && <div><span className="text-gray-500 dark:text-gray-400">Industria:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.industry}</span></div>}
                  {aiPreview.currency && <div><span className="text-gray-500 dark:text-gray-400">Divisa:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.currency}</span></div>}
                  {aiPreview.preferredSource && <div><span className="text-gray-500 dark:text-gray-400">Fuente:</span> <span className="font-medium text-gray-800 dark:text-gray-200">{aiPreview.preferredSource}</span></div>}
                </div>
                {aiPreview.competitors && (
                  <div className="mt-2 text-sm"><span className="text-gray-500 dark:text-gray-400">Competidores:</span> <span className="text-gray-700 dark:text-gray-300">{aiPreview.competitors}</span></div>
                )}
                {aiPreview.notes && (
                  <div className="mt-2 text-sm"><span className="text-gray-500 dark:text-gray-400">Notas:</span> <span className="text-gray-700 dark:text-gray-300">{aiPreview.notes}</span></div>
                )}
              </div>
              <div className="flex gap-2">
                <button
                  onClick={handleAiConfirm}
                  disabled={aiSaving}
                  className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm font-medium disabled:opacity-50"
                >
                  {aiSaving ? 'Guardando...' : 'Aceptar y añadir'}
                </button>
                <button
                  onClick={() => { setAiPreview(null); setAiQuery(''); }}
                  className="px-4 py-2 bg-gray-300 dark:bg-gray-600 text-gray-800 dark:text-gray-200 rounded-lg text-sm"
                >
                  Descartar
                </button>
              </div>
            </div>
          )}
          {aiError && (
            <div className="text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 p-2 rounded">
              {aiError}
            </div>
          )}
        </div>
      )}

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
