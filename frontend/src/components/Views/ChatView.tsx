import { useState, useRef, useEffect } from 'react';
import axios from 'axios';

interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  provider?: string;
  model?: string;
  tokens?: number;
  timestamp: Date;
}

export function ChatView() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const sendMessage = async () => {
    if (!input.trim() || loading) return;

    const userMsg: Message = {
      id: crypto.randomUUID(),
      role: 'user',
      content: input.trim(),
      timestamp: new Date()
    };

    setMessages(prev => [...prev, userMsg]);
    setInput('');
    setLoading(true);

    try {
      const res = await axios.post('/api/chat', { message: userMsg.content, maxTokens: 2000 });
      const assistantMsg: Message = {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: res.data.message,
        provider: res.data.provider,
        model: res.data.model,
        tokens: res.data.tokensUsed,
        timestamp: new Date()
      };
      setMessages(prev => [...prev, assistantMsg]);
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Error de conexión';
      setMessages(prev => [...prev, {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: `⚠️ Error: ${errorMsg}. Verifica que el backend esté activo y las API keys configuradas.`,
        timestamp: new Date()
      }]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col" style={{ height: 'calc(100vh - 7.5rem)' }}>
      <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-2 flex-shrink-0">Chat IA Financiero</h2>

      {/* Messages area */}
      <div className="flex-1 min-h-0 overflow-y-auto bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-4">
        {messages.length === 0 && (
          <div className="text-center py-12 text-gray-400 dark:text-gray-500">
            <div className="text-4xl mb-3">💬</div>
            <p className="text-sm">Pregunta sobre fondos, empresas, métricas o datos cargados.</p>
            <p className="text-xs mt-1">Solo responde con datos verificados. Nunca inventa cifras.</p>
          </div>
        )}

        {messages.map(msg => (
          <div key={msg.id} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            <div className={`max-w-[80%] rounded-lg px-4 py-2 ${
              msg.role === 'user'
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-200'
            }`}>
              <div className="whitespace-pre-wrap text-sm">{msg.content}</div>
              {msg.provider && (
                <div className="text-[10px] mt-1 opacity-60">
                  {msg.provider}/{msg.model} • {msg.tokens} tokens
                </div>
              )}
            </div>
          </div>
        ))}

        {loading && (
          <div className="flex justify-start">
            <div className="bg-gray-100 dark:bg-gray-800 rounded-lg px-4 py-2">
              <div className="flex space-x-1">
                <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" />
                <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce [animation-delay:0.2s]" />
                <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce [animation-delay:0.4s]" />
              </div>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Input area */}
      <div className="mt-2 flex gap-2 flex-shrink-0">
        <input
          type="text"
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && sendMessage()}
          placeholder="Escribe tu pregunta..."
          className="input-field flex-1"
          disabled={loading}
        />
        <button
          onClick={sendMessage}
          disabled={loading || !input.trim()}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          Enviar
        </button>
      </div>

      {/* Disclaimer */}
      <div className="mt-1.5 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg p-2 text-xs text-amber-800 dark:text-amber-200 flex-shrink-0">
        economIA es una herramienta de apoyo al análisis financiero. No constituye recomendación de inversión.
      </div>
    </div>
  );
}
