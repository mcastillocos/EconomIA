import { TrendingUp } from 'lucide-react';

export default function Header() {
  return (
    <header className="bg-white border-b border-gray-200 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <TrendingUp className="h-8 w-8 text-primary-600" />
          <h1 className="text-xl font-bold text-gray-900">
            Econom<span className="text-primary-600">IA</span>
          </h1>
        </div>
        <div className="flex items-center gap-4">
          <span className="inline-flex items-center gap-1.5 text-sm text-green-700 bg-green-50 px-3 py-1 rounded-full">
            <span className="h-2 w-2 rounded-full bg-green-500 animate-pulse"></span>
            En vivo
          </span>
        </div>
      </div>
    </header>
  );
}
