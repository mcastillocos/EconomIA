import { Download } from 'lucide-react';
import { useState } from 'react';

interface ExportButtonProps {
  endpoint: string;
  label?: string;
  className?: string;
  formats?: ('pdf' | 'excel' | 'md')[];
}

export function ExportButton({ endpoint, label = 'Exportar', className = '', formats = ['pdf', 'excel', 'md'] }: ExportButtonProps) {
  const [loading, setLoading] = useState(false);
  const [showMenu, setShowMenu] = useState(false);

  const handleExport = async (format: 'pdf' | 'excel' | 'md') => {
    setLoading(true);
    setShowMenu(false);
    try {
      const separator = endpoint.includes('?') ? '&' : '?';
      const res = await fetch(`${endpoint}${separator}format=${format}`);
      if (!res.ok) throw new Error('Error en exportación');
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      const disposition = res.headers.get('content-disposition');
      const filename = disposition?.match(/filename="?([^"]+)"?/)?.[1] ?? `export.${format === 'excel' ? 'xlsx' : format}`;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (e) {
      console.error('Export error:', e);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={`relative ${className}`}>
      <button
        onClick={() => setShowMenu(!showMenu)}
        disabled={loading}
        className="flex items-center gap-1.5 px-3 py-1.5 text-sm bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 transition-colors"
      >
        <Download className="h-4 w-4" />
        {loading ? 'Exportando...' : label}
      </button>
      {showMenu && (
        <div className="absolute right-0 top-full mt-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg z-50 overflow-hidden">
          {formats.includes('pdf') && (
            <button
              onClick={() => handleExport('pdf')}
              className="w-full px-4 py-2 text-sm text-left hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
            >
              📄 PDF
            </button>
          )}
          {formats.includes('excel') && (
            <button
              onClick={() => handleExport('excel')}
              className="w-full px-4 py-2 text-sm text-left hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
            >
              📊 Excel
            </button>
          )}
          {formats.includes('md') && (
            <button
              onClick={() => handleExport('md')}
              className="w-full px-4 py-2 text-sm text-left hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
            >
              📝 Markdown
            </button>
          )}
        </div>
      )}
    </div>
  );
}
