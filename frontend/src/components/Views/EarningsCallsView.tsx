import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { Mic, FileText, Upload, Play, RefreshCw, TrendingUp, TrendingDown, Minus, Clock, AlertCircle, Square, Circle } from 'lucide-react';

interface EarningsCallSummary {
  id: string;
  companyName: string;
  ticker?: string;
  fiscalYear: number;
  fiscalQuarter: number;
  callDate: string;
  status: string;
  sentiment?: string;
  durationSeconds?: number;
  language?: string;
  hasTranscript: boolean;
  hasSummary: boolean;
  createdAt: string;
}

interface EarningsCallDetail {
  id: string;
  companyName: string;
  ticker?: string;
  fiscalYear: number;
  fiscalQuarter: number;
  callDate: string;
  status: string;
  transcriptText?: string;
  summary?: string;
  guidance?: string;
  keyMetrics?: string;
  sentiment?: string;
  durationSeconds?: number;
  language?: string;
  errorMessage?: string;
}

export default function EarningsCallsView() {
  const queryClient = useQueryClient();
  const [selectedCall, setSelectedCall] = useState<string | null>(null);
  const [uploadMode, setUploadMode] = useState<'audio' | 'transcript' | 'microphone' | null>(null);

  // Form state
  const [form, setForm] = useState({
    companyName: '',
    ticker: '',
    fiscalYear: new Date().getFullYear(),
    fiscalQuarter: Math.ceil((new Date().getMonth() + 1) / 3),
    callDate: new Date().toISOString().slice(0, 10),
    transcript: '',
    language: 'es',
  });
  const [audioFile, setAudioFile] = useState<File | null>(null);
  const [isRecording, setIsRecording] = useState(false);
  const [recordingTime, setRecordingTime] = useState(0);
  const [mediaRecorder, setMediaRecorder] = useState<MediaRecorder | null>(null);

  const { data: calls = [] } = useQuery<EarningsCallSummary[]>({
    queryKey: ['earnings-calls'],
    queryFn: async () => (await axios.get('/api/earnings-calls')).data,
  });

  const { data: detail } = useQuery<EarningsCallDetail>({
    queryKey: ['earnings-call', selectedCall],
    queryFn: async () => (await axios.get(`/api/earnings-calls/${selectedCall}`)).data,
    enabled: !!selectedCall,
  });

  const uploadAudioMutation = useMutation({
    mutationFn: async () => {
      if (!audioFile) return;
      const fd = new FormData();
      fd.append('file', audioFile);
      fd.append('companyName', form.companyName);
      fd.append('fiscalYear', form.fiscalYear.toString());
      fd.append('fiscalQuarter', form.fiscalQuarter.toString());
      fd.append('ticker', form.ticker);
      fd.append('callDate', form.callDate);
      return (await axios.post('/api/earnings-calls/upload-audio', fd)).data;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['earnings-calls'] });
      setSelectedCall(data.id);
      setUploadMode(null);
      resetForm();
    },
  });

  const uploadTranscriptMutation = useMutation({
    mutationFn: async () => {
      return (await axios.post('/api/earnings-calls/upload-transcript', {
        companyName: form.companyName,
        ticker: form.ticker,
        fiscalYear: form.fiscalYear,
        fiscalQuarter: form.fiscalQuarter,
        callDate: form.callDate,
        transcript: form.transcript,
        language: form.language,
      })).data;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['earnings-calls'] });
      setSelectedCall(data.id);
      setUploadMode(null);
      resetForm();
    },
  });

  const reanalyzeMutation = useMutation({
    mutationFn: (id: string) => axios.post(`/api/earnings-calls/${id}/reanalyze`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['earnings-call', selectedCall] }),
  });

  const resetForm = () => {
    setForm({ companyName: '', ticker: '', fiscalYear: new Date().getFullYear(), fiscalQuarter: Math.ceil((new Date().getMonth() + 1) / 3), callDate: new Date().toISOString().slice(0, 10), transcript: '', language: 'es' });
    setAudioFile(null);
    setIsRecording(false);
    setRecordingTime(0);
  };

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const recorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });
      const chunks: BlobPart[] = [];
      recorder.ondataavailable = (e) => chunks.push(e.data);
      recorder.onstop = () => {
        stream.getTracks().forEach(t => t.stop());
        const blob = new Blob(chunks, { type: 'audio/webm' });
        const file = new File([blob], `grabacion_${Date.now()}.webm`, { type: 'audio/webm' });
        setAudioFile(file);
      };
      recorder.start();
      setMediaRecorder(recorder);
      setIsRecording(true);
      setRecordingTime(0);
      const interval = setInterval(() => {
        setRecordingTime(t => {
          if (t >= 3600) { recorder.stop(); clearInterval(interval); return t; }
          return t + 1;
        });
      }, 1000);
      (recorder as unknown as { _interval: NodeJS.Timeout })._interval = interval;
    } catch {
      alert('No se pudo acceder al micrófono. Verifica los permisos del navegador.');
    }
  };

  const stopRecording = () => {
    if (mediaRecorder && mediaRecorder.state === 'recording') {
      mediaRecorder.stop();
      const interval = (mediaRecorder as unknown as { _interval: NodeJS.Timeout })._interval;
      if (interval) clearInterval(interval);
    }
    setIsRecording(false);
    setMediaRecorder(null);
  };

  const sentimentIcon = (s?: string) => {
    if (s === 'positive') return <TrendingUp className="h-4 w-4 text-green-500" />;
    if (s === 'negative') return <TrendingDown className="h-4 w-4 text-red-500" />;
    return <Minus className="h-4 w-4 text-gray-400" />;
  };

  const statusBadge = (status: string) => {
    const colors: Record<string, string> = {
      pending: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
      transcribing: 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400',
      analyzing: 'bg-yellow-100 text-yellow-600 dark:bg-yellow-900/30 dark:text-yellow-400',
      completed: 'bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400',
      failed: 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400',
    };
    const labels: Record<string, string> = {
      pending: 'Pendiente',
      transcribing: 'Transcribiendo',
      analyzing: 'Analizando',
      completed: 'Completado',
      failed: 'Error',
    };
    return <span className={`text-xs px-2 py-0.5 rounded-full ${colors[status] ?? colors.pending}`}>{labels[status] ?? status}</span>;
  };

  return (
    <div className="p-4 md:p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Mic className="h-6 w-6 text-purple-500" />
          <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">Llamadas de Resultados</h2>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => { setUploadMode('microphone'); setSelectedCall(null); }}
            className="flex items-center gap-1 px-3 py-1.5 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700"
          >
            <Mic className="h-4 w-4" /> Micrófono
          </button>
          <button
            onClick={() => { setUploadMode('audio'); setSelectedCall(null); }}
            className="flex items-center gap-1 px-3 py-1.5 text-sm bg-purple-600 text-white rounded-lg hover:bg-purple-700"
          >
            <Upload className="h-4 w-4" /> Audio
          </button>
          <button
            onClick={() => { setUploadMode('transcript'); setSelectedCall(null); }}
            className="flex items-center gap-1 px-3 py-1.5 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            <FileText className="h-4 w-4" /> Transcripción
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Lista */}
        <div className="space-y-1 max-h-[70vh] overflow-y-auto">
          {calls.length === 0 && !uploadMode && (
            <div className="text-center py-8 text-gray-400">
              <Mic className="h-10 w-10 mx-auto mb-2 opacity-30" />
              <p className="text-sm">No hay llamadas de resultados aún</p>
              <p className="text-xs">Sube un audio o graba desde el micrófono</p>
            </div>
          )}
          {calls.map(c => (
            <button
              key={c.id}
              onClick={() => { setSelectedCall(c.id); setUploadMode(null); }}
              className={`w-full text-left px-3 py-2 rounded-lg text-sm transition-colors ${
                selectedCall === c.id
                  ? 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300'
                  : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300'
              }`}
            >
              <div className="flex items-center justify-between">
                <span className="font-medium">{c.companyName} {c.ticker && `(${c.ticker})`}</span>
                {sentimentIcon(c.sentiment)}
              </div>
              <div className="flex items-center gap-2 mt-0.5">
                <span className="text-xs text-gray-400">Q{c.fiscalQuarter} {c.fiscalYear}</span>
                {statusBadge(c.status)}
                {c.durationSeconds && (
                  <span className="text-xs text-gray-400 flex items-center gap-0.5">
                    <Clock className="h-3 w-3" /> {Math.round(c.durationSeconds / 60)}min
                  </span>
                )}
              </div>
            </button>
          ))}
        </div>

        {/* Panel derecho */}
        <div className="lg:col-span-2 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 max-h-[70vh] overflow-y-auto">
          {/* Formulario de grabación por micrófono */}
          {uploadMode === 'microphone' && (
            <div className="space-y-4">
              <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">Grabar desde Micrófono</h3>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-gray-500">Empresa*</label>
                  <input type="text" value={form.companyName} onChange={e => setForm(f => ({ ...f, companyName: e.target.value }))}
                    placeholder="Apple" className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Ticker</label>
                  <input type="text" value={form.ticker} onChange={e => setForm(f => ({ ...f, ticker: e.target.value }))}
                    placeholder="AAPL" className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Año fiscal*</label>
                  <input type="number" value={form.fiscalYear} onChange={e => setForm(f => ({ ...f, fiscalYear: +e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Trimestre*</label>
                  <select value={form.fiscalQuarter} onChange={e => setForm(f => ({ ...f, fiscalQuarter: +e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                    <option value={1}>Q1</option><option value={2}>Q2</option><option value={3}>Q3</option><option value={4}>Q4</option>
                  </select>
                </div>
                <div>
                  <label className="text-xs text-gray-500">Fecha de la llamada</label>
                  <input type="date" value={form.callDate} onChange={e => setForm(f => ({ ...f, callDate: e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
              </div>

              {/* Controles de grabación */}
              <div className="flex flex-col items-center gap-4 py-6 bg-gray-50 dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700">
                {!isRecording && !audioFile && (
                  <button onClick={startRecording}
                    className="flex items-center gap-2 px-6 py-3 bg-red-600 text-white rounded-full hover:bg-red-700 text-sm font-medium shadow-lg hover:shadow-xl transition-all">
                    <Circle className="h-5 w-5 fill-current" /> Iniciar Grabación
                  </button>
                )}
                {isRecording && (
                  <>
                    <div className="flex items-center gap-2">
                      <span className="h-3 w-3 bg-red-500 rounded-full animate-pulse" />
                      <span className="text-sm font-medium text-red-600 dark:text-red-400">Grabando...</span>
                      <span className="text-sm text-gray-500 font-mono">
                        {Math.floor(recordingTime / 60).toString().padStart(2, '0')}:{(recordingTime % 60).toString().padStart(2, '0')}
                      </span>
                    </div>
                    <button onClick={stopRecording}
                      className="flex items-center gap-2 px-6 py-3 bg-gray-800 text-white rounded-full hover:bg-gray-900 text-sm font-medium shadow-lg">
                      <Square className="h-4 w-4 fill-current" /> Detener
                    </button>
                  </>
                )}
                {audioFile && !isRecording && (
                  <div className="text-center space-y-2">
                    <p className="text-sm text-green-600 dark:text-green-400 font-medium">
                      ✓ Grabación lista: {audioFile.name} ({Math.round(audioFile.size / 1024)} KB)
                    </p>
                    <button onClick={() => { setAudioFile(null); setRecordingTime(0); }}
                      className="text-xs text-gray-500 hover:text-red-500 underline">
                      Descartar y grabar de nuevo
                    </button>
                  </div>
                )}
                <p className="text-xs text-gray-400 mt-2">Máx. 60 min · Formato WebM</p>
              </div>

              <button
                onClick={() => uploadAudioMutation.mutate()}
                disabled={!form.companyName || !audioFile || uploadAudioMutation.isPending}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50 flex items-center gap-2"
              >
                {uploadAudioMutation.isPending ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Play className="h-4 w-4" />}
                Transcribir y Analizar
              </button>
            </div>
          )}

          {/* Formulario de subida audio */}
          {uploadMode === 'audio' && (
            <div className="space-y-4">
              <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">Subir Audio de Llamada de Resultados</h3>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-gray-500">Empresa*</label>
                  <input type="text" value={form.companyName} onChange={e => setForm(f => ({ ...f, companyName: e.target.value }))}
                    placeholder="Apple" className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Ticker</label>
                  <input type="text" value={form.ticker} onChange={e => setForm(f => ({ ...f, ticker: e.target.value }))}
                    placeholder="AAPL" className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Año fiscal*</label>
                  <input type="number" value={form.fiscalYear} onChange={e => setForm(f => ({ ...f, fiscalYear: +e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Trimestre*</label>
                  <select value={form.fiscalQuarter} onChange={e => setForm(f => ({ ...f, fiscalQuarter: +e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                    <option value={1}>Q1</option><option value={2}>Q2</option><option value={3}>Q3</option><option value={4}>Q4</option>
                  </select>
                </div>
                <div>
                  <label className="text-xs text-gray-500">Fecha de la call</label>
                  <input type="date" value={form.callDate} onChange={e => setForm(f => ({ ...f, callDate: e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Archivo audio* (mp3/wav/m4a)</label>
                  <input type="file" accept=".mp3,.wav,.m4a,.mp4,.webm,.ogg,.flac"
                    onChange={e => setAudioFile(e.target.files?.[0] ?? null)}
                    className="w-full text-sm text-gray-500" />
                </div>
              </div>
              <button
                onClick={() => uploadAudioMutation.mutate()}
                disabled={!form.companyName || !audioFile || uploadAudioMutation.isPending}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50 flex items-center gap-2"
              >
                {uploadAudioMutation.isPending ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Play className="h-4 w-4" />}
                Transcribir y Analizar
              </button>
            </div>
          )}

          {/* Formulario de subida transcript */}
          {uploadMode === 'transcript' && (
            <div className="space-y-4">
              <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">Pegar Transcripción de Llamada de Resultados</h3>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-gray-500">Empresa*</label>
                  <input type="text" value={form.companyName} onChange={e => setForm(f => ({ ...f, companyName: e.target.value }))}
                    placeholder="Apple" className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Ticker</label>
                  <input type="text" value={form.ticker} onChange={e => setForm(f => ({ ...f, ticker: e.target.value }))}
                    placeholder="AAPL" className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Año fiscal*</label>
                  <input type="number" value={form.fiscalYear} onChange={e => setForm(f => ({ ...f, fiscalYear: +e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Trimestre*</label>
                  <select value={form.fiscalQuarter} onChange={e => setForm(f => ({ ...f, fiscalQuarter: +e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                    <option value={1}>Q1</option><option value={2}>Q2</option><option value={3}>Q3</option><option value={4}>Q4</option>
                  </select>
                </div>
                <div>
                  <label className="text-xs text-gray-500">Fecha</label>
                  <input type="date" value={form.callDate} onChange={e => setForm(f => ({ ...f, callDate: e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Idioma</label>
                  <select value={form.language} onChange={e => setForm(f => ({ ...f, language: e.target.value }))}
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                    <option value="es">Español</option><option value="en">English</option>
                  </select>
                </div>
              </div>
              <div>
                <label className="text-xs text-gray-500">Transcript*</label>
                <textarea
                  value={form.transcript}
                  onChange={e => setForm(f => ({ ...f, transcript: e.target.value }))}
                  rows={10}
                  placeholder="Pega aquí el texto de la transcripción de la llamada de resultados..."
                  className="w-full px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 resize-y"
                />
              </div>
              <button
                onClick={() => uploadTranscriptMutation.mutate()}
                disabled={!form.companyName || !form.transcript || uploadTranscriptMutation.isPending}
                className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 flex items-center gap-2"
              >
                {uploadTranscriptMutation.isPending ? <RefreshCw className="h-4 w-4 animate-spin" /> : <FileText className="h-4 w-4" />}
                Analizar Transcripción
              </button>
            </div>
          )}

          {/* Detalle de una call */}
          {detail && !uploadMode && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">
                    {detail.companyName} {detail.ticker && `(${detail.ticker})`}
                  </h3>
                  <p className="text-sm text-gray-500">
                    Q{detail.fiscalQuarter} {detail.fiscalYear} • {new Date(detail.callDate).toLocaleDateString('es-ES')}
                    {detail.durationSeconds && ` • ${Math.round(detail.durationSeconds / 60)} min`}
                    {detail.language && ` • ${detail.language.toUpperCase()}`}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  {statusBadge(detail.status)}
                  {sentimentIcon(detail.sentiment)}
                  {detail.status === 'completed' && (
                    <button onClick={() => reanalyzeMutation.mutate(detail.id)}
                      className="p-1 text-gray-400 hover:text-purple-600" title="Re-analizar">
                      <RefreshCw className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </div>

              {detail.status === 'failed' && detail.errorMessage && (
                <div className="flex items-start gap-2 p-3 bg-red-50 dark:bg-red-900/20 rounded-lg">
                  <AlertCircle className="h-4 w-4 text-red-500 mt-0.5 flex-shrink-0" />
                  <p className="text-sm text-red-600 dark:text-red-400">{detail.errorMessage}</p>
                </div>
              )}

              {detail.summary && (
                <div>
                  <h4 className="text-sm font-semibold text-purple-600 dark:text-purple-400 mb-1">Resumen</h4>
                  <p className="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{detail.summary}</p>
                </div>
              )}

              {detail.guidance && (
                <div>
                  <h4 className="text-sm font-semibold text-blue-600 dark:text-blue-400 mb-1">Previsiones</h4>
                  <p className="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{detail.guidance}</p>
                </div>
              )}

              {detail.keyMetrics && (
                <div>
                  <h4 className="text-sm font-semibold text-green-600 dark:text-green-400 mb-1">Métricas Clave</h4>
                  <p className="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{detail.keyMetrics}</p>
                </div>
              )}

              {detail.transcriptText && (
                <div>
                  <h4 className="text-sm font-semibold text-gray-600 dark:text-gray-400 mb-1">
                    Transcripción ({detail.transcriptText.length.toLocaleString()} caracteres)
                  </h4>
                  <div className="max-h-60 overflow-y-auto p-3 bg-gray-50 dark:bg-gray-900 rounded border border-gray-200 dark:border-gray-700">
                    <p className="text-xs text-gray-600 dark:text-gray-400 whitespace-pre-wrap font-mono">{detail.transcriptText}</p>
                  </div>
                </div>
              )}

              {detail.status === 'transcribing' && (
                <div className="flex items-center gap-2 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                  <RefreshCw className="h-5 w-5 text-blue-500 animate-spin" />
                  <p className="text-sm text-blue-600 dark:text-blue-400">Transcribiendo audio con Whisper...</p>
                </div>
              )}
              {detail.status === 'analyzing' && (
                <div className="flex items-center gap-2 p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                  <RefreshCw className="h-5 w-5 text-yellow-500 animate-spin" />
                  <p className="text-sm text-yellow-600 dark:text-yellow-400">Analizando transcript con IA...</p>
                </div>
              )}
            </div>
          )}

          {!detail && !uploadMode && (
            <div className="text-center py-12 text-gray-400">
              <Mic className="h-12 w-12 mx-auto mb-3 opacity-30" />
              <p>Selecciona una llamada o sube una nueva</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
