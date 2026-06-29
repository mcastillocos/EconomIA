import { useState, useEffect, useCallback } from 'react';
import { documentsApi } from '../../services/documentsApi';
import type { UploadedDocument } from '../../types/document';
import { appLog } from '../../store/logStore';

export function UploadsView() {
  const [documents, setDocuments] = useState<UploadedDocument[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [entityType, setEntityType] = useState('company');
  const [dragOver, setDragOver] = useState(false);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const data = await documentsApi.getAll();
      setDocuments(data);
    } catch (e) {
      appLog.error('App', `Error cargando documentos: ${e}`);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleUpload = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    setUploading(true);
    try {
      for (const file of Array.from(files)) {
        await documentsApi.upload(file, entityType);
        appLog.success('App', `Archivo subido: ${file.name}`);
      }
      load();
    } catch (e) {
      appLog.error('App', `Error subiendo archivo: ${e}`);
    } finally {
      setUploading(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    handleUpload(e.dataTransfer.files);
  };

  const statusBadge = (status: string) => {
    const colors: Record<string, string> = {
      pending: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300',
      processing: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
      completed: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300',
      failed: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300',
    };
    return <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${colors[status] || ''}`}>{status}</span>;
  };

  const formatSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Subidas</h2>

      {/* Drop zone */}
      <div
        onDragOver={e => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        className={`border-2 border-dashed rounded-xl p-8 text-center transition-colors ${
          dragOver ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20' : 'border-gray-300 dark:border-gray-600'
        }`}
      >
        <p className="text-gray-500 dark:text-gray-400 mb-2">
          {uploading ? 'Subiendo...' : 'Arrastra archivos aquí o haz clic para seleccionar'}
        </p>
        <p className="text-xs text-gray-400 dark:text-gray-500 mb-3">CSV, Excel (.xlsx), PDF, TXT</p>
        <div className="flex items-center justify-center gap-3">
          <select value={entityType} onChange={e => setEntityType(e.target.value)} className="input-field text-sm">
            <option value="company">Empresa</option>
            <option value="fund">Fondo</option>
            <option value="sector">Sector</option>
            <option value="portfolio">Cartera</option>
          </select>
          <label className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm cursor-pointer">
            Seleccionar archivos
            <input type="file" multiple accept=".csv,.tsv,.xlsx,.xls,.pdf,.txt" onChange={e => handleUpload(e.target.files)} className="hidden" />
          </label>
        </div>
      </div>

      {/* Documents list */}
      {loading ? (
        <p className="text-gray-500 dark:text-gray-400">Cargando documentos...</p>
      ) : documents.length === 0 ? (
        <p className="text-gray-500 dark:text-gray-400">No hay documentos subidos.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="text-xs uppercase bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300">
              <tr>
                <th className="px-3 py-2">Archivo</th>
                <th className="px-3 py-2">Tipo</th>
                <th className="px-3 py-2">Entidad</th>
                <th className="px-3 py-2">Tamaño</th>
                <th className="px-3 py-2">Estado</th>
                <th className="px-3 py-2">Resumen</th>
                <th className="px-3 py-2">Fecha</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {documents.map(d => (
                <tr key={d.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                  <td className="px-3 py-2 font-medium text-gray-800 dark:text-gray-200 max-w-[200px] truncate">{d.fileName}</td>
                  <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{d.fileType}</td>
                  <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{d.entityType}</td>
                  <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{formatSize(d.fileSize)}</td>
                  <td className="px-3 py-2">{statusBadge(d.status)}</td>
                  <td className="px-3 py-2 text-gray-500 dark:text-gray-400 max-w-[200px] truncate">{d.summary || d.errorMessage || '-'}</td>
                  <td className="px-3 py-2 text-gray-500 dark:text-gray-400 text-xs">{new Date(d.uploadDate).toLocaleDateString('es-ES')}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
