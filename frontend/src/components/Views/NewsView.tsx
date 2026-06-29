import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import axios from 'axios';
import { Rss, ExternalLink, Clock, Filter, RefreshCw } from 'lucide-react';

interface NewsItem {
  title: string;
  summary: string;
  url: string;
  publishedAt: string;
  source: string;
  categories: string[];
}

export default function NewsView() {
  const [filterTerms, setFilterTerms] = useState('');

  const { data: news = [], isLoading, refetch, isFetching } = useQuery<NewsItem[]>({
    queryKey: ['news', filterTerms],
    queryFn: async () => {
      if (filterTerms.trim()) {
        const terms = filterTerms.split(',').map(t => t.trim()).filter(Boolean);
        const res = await axios.post('/api/connectors/news/filter', { terms, maxPerFeed: 15 });
        return res.data;
      }
      const res = await axios.get('/api/connectors/news', { params: { maxPerFeed: 15 } });
      return res.data;
    },
    refetchInterval: 5 * 60 * 1000, // Auto-refresh cada 5 min
    staleTime: 2 * 60 * 1000,
  });

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    if (diffMins < 60) return `hace ${diffMins}min`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `hace ${diffHours}h`;
    return date.toLocaleDateString('es-ES', { day: '2-digit', month: 'short' });
  };

  return (
    <div className="p-4 md:p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Rss className="h-6 w-6 text-orange-500" />
          <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">Noticias Financieras</h2>
          <span className="text-sm text-gray-500 dark:text-gray-400">
            {news.length} noticias
          </span>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          className="flex items-center gap-2 px-3 py-1.5 text-sm bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50"
        >
          <RefreshCw className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
          Actualizar
        </button>
      </div>

      {/* Filtro por términos */}
      <div className="flex items-center gap-2">
        <Filter className="h-4 w-4 text-gray-400" />
        <input
          type="text"
          value={filterTerms}
          onChange={(e) => setFilterTerms(e.target.value)}
          placeholder="Filtrar por empresas/temas (separar por comas): Apple, Tesla, Fed..."
          className="flex-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-400"
        />
      </div>

      {/* Lista de noticias */}
      {isLoading ? (
        <div className="space-y-3">
          {[...Array(8)].map((_, i) => (
            <div key={i} className="animate-pulse p-4 bg-gray-100 dark:bg-gray-800 rounded-lg">
              <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4 mb-2" />
              <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-full" />
            </div>
          ))}
        </div>
      ) : news.length === 0 ? (
        <div className="text-center py-12 text-gray-500 dark:text-gray-400">
          <Rss className="h-12 w-12 mx-auto mb-3 opacity-30" />
          <p>No se encontraron noticias. Los feeds RSS pueden no estar disponibles.</p>
        </div>
      ) : (
        <div className="space-y-2">
          {news.map((item, idx) => (
            <article
              key={idx}
              className="p-4 bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-lg hover:shadow-md transition-shadow"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <a
                    href={item.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-sm font-semibold text-gray-900 dark:text-gray-100 hover:text-primary-600 dark:hover:text-primary-400 line-clamp-2"
                  >
                    {item.title}
                    <ExternalLink className="inline h-3 w-3 ml-1 opacity-50" />
                  </a>
                  {item.summary && (
                    <p className="mt-1 text-xs text-gray-500 dark:text-gray-400 line-clamp-2"
                       dangerouslySetInnerHTML={{ __html: stripHtml(item.summary) }}
                    />
                  )}
                  <div className="mt-2 flex items-center gap-3 text-xs text-gray-400">
                    <span className="font-medium text-primary-600 dark:text-primary-400">{item.source}</span>
                    <span className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {formatDate(item.publishedAt)}
                    </span>
                    {item.categories.length > 0 && (
                      <div className="flex gap-1">
                        {item.categories.slice(0, 3).map((cat, i) => (
                          <span key={i} className="px-1.5 py-0.5 bg-gray-100 dark:bg-gray-700 rounded text-[10px]">
                            {cat}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}

function stripHtml(html: string): string {
  const div = document.createElement('div');
  div.innerHTML = html;
  return div.textContent || div.innerText || '';
}
