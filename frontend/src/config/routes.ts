import type { ViewId } from '../components/Layout/Sidebar';

export const ROUTES: Record<ViewId, string> = {
  overview: '/',
  global: '/fondos',
  datos: '/fondos/datos',
  graficas: '/fondos/graficas',
  misfondos: '/fondos/mis-fondos',
  companies: '/analisis/empresas',
  watchlists: '/analisis/carteras',
  uploads: '/analisis/uploads',
  metrics: '/analisis/metricas',
  reports: '/ia/informes',
  briefing: '/ia/briefing',
  news: '/ia/noticias',
  screener: '/ia/screener',
  chat: '/ia/chat',
  agents: '/ia/agentes',
  workflows: '/ia/workflows',
  checklists: '/ia/checklists',
  logs: '/sistema/logs',
  stats: '/sistema/estadisticas',
  config: '/sistema/config',
};

const pathToView = new Map(
  Object.entries(ROUTES).map(([view, path]) => [path, view as ViewId])
);

export function getViewFromPath(pathname: string): ViewId {
  return pathToView.get(pathname) ?? 'overview';
}

export function getPathFromView(view: ViewId): string {
  return ROUTES[view] ?? '/';
}
