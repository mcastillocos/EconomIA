import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { ClipboardCheck, Plus, ChevronRight, Check, X, Star, FileText, HelpCircle } from 'lucide-react';

interface Template {
  id: string;
  name: string;
  description: string;
  category: string;
  isBuiltIn: boolean;
  itemCount: number;
  sections: string[];
}

interface TemplateDetail {
  id: string;
  name: string;
  description: string;
  category: string;
  items: TemplateItem[];
}

interface TemplateItem {
  id: string;
  text: string;
  section: string;
  order: number;
  itemType: string;
  helpText?: string;
}

interface Instance {
  id: string;
  templateId: string;
  entityType: string;
  entityName: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  answerCount: number;
}

interface InstanceDetail {
  id: string;
  templateId: string;
  entityName: string;
  status: string;
  answers: { templateItemId: string; value: string; comment?: string }[];
}

export default function ChecklistsView() {
  const queryClient = useQueryClient();
  const [selectedTemplate, setSelectedTemplate] = useState<string | null>(null);
  const [selectedInstance, setSelectedInstance] = useState<string | null>(null);
  const [newEntity, setNewEntity] = useState({ name: '', type: 'company' });

  const { data: templates = [] } = useQuery<Template[]>({
    queryKey: ['checklist-templates'],
    queryFn: async () => (await axios.get('/api/checklists/templates')).data,
  });

  const { data: instances = [] } = useQuery<Instance[]>({
    queryKey: ['checklist-instances'],
    queryFn: async () => (await axios.get('/api/checklists/instances')).data,
  });

  const { data: templateDetail } = useQuery<TemplateDetail>({
    queryKey: ['checklist-template', selectedTemplate],
    queryFn: async () => (await axios.get(`/api/checklists/templates/${selectedTemplate}`)).data,
    enabled: !!selectedTemplate && !selectedInstance,
  });

  const { data: instanceDetail } = useQuery<InstanceDetail>({
    queryKey: ['checklist-instance', selectedInstance],
    queryFn: async () => (await axios.get(`/api/checklists/instances/${selectedInstance}`)).data,
    enabled: !!selectedInstance,
  });

  const seedMutation = useMutation({
    mutationFn: () => axios.post('/api/checklists/templates/seed'),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['checklist-templates'] }),
  });

  const createInstance = useMutation({
    mutationFn: (data: { templateId: string; entityType: string; entityName: string }) =>
      axios.post('/api/checklists/instances', data),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['checklist-instances'] });
      setSelectedInstance(res.data.id);
      setNewEntity({ name: '', type: 'company' });
    },
  });

  const answerMutation = useMutation({
    mutationFn: (data: { instanceId: string; templateItemId: string; value: string; comment?: string }) =>
      axios.put(`/api/checklists/instances/${data.instanceId}/answer`, {
        templateItemId: data.templateItemId,
        value: data.value,
        comment: data.comment,
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['checklist-instance', selectedInstance] }),
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => axios.put(`/api/checklists/instances/${id}/complete`, {}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklist-instances'] });
      queryClient.invalidateQueries({ queryKey: ['checklist-instance', selectedInstance] });
    },
  });

  const { data: activeTemplateDetail } = useQuery<TemplateDetail>({
    queryKey: ['checklist-template', instanceDetail?.templateId],
    queryFn: async () => (await axios.get(`/api/checklists/templates/${instanceDetail?.templateId}`)).data,
    enabled: !!instanceDetail?.templateId,
  });

  const getAnswer = (itemId: string) =>
    instanceDetail?.answers?.find(a => a.templateItemId === itemId);

  return (
    <div className="p-4 md:p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ClipboardCheck className="h-6 w-6 text-indigo-500" />
          <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">Checklists de Inversión</h2>
        </div>
        {templates.length === 0 && (
          <button
            onClick={() => seedMutation.mutate()}
            className="flex items-center gap-2 px-3 py-1.5 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            <Plus className="h-4 w-4" />
            Crear templates predefinidos
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Panel izquierdo: Templates + Instancias */}
        <div className="space-y-4">
          <div>
            <h3 className="text-sm font-semibold text-gray-500 dark:text-gray-400 mb-2">Templates</h3>
            <div className="space-y-1">
              {templates.map(t => (
                <button
                  key={t.id}
                  onClick={() => { setSelectedTemplate(t.id); setSelectedInstance(null); }}
                  className={`w-full text-left px-3 py-2 rounded-lg text-sm transition-colors ${
                    selectedTemplate === t.id && !selectedInstance
                      ? 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300'
                      : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="font-medium">{t.name}</span>
                    <span className="text-xs text-gray-400">{t.itemCount} items</span>
                  </div>
                  <span className="text-xs text-gray-400">{t.category}</span>
                </button>
              ))}
            </div>
          </div>

          <div>
            <h3 className="text-sm font-semibold text-gray-500 dark:text-gray-400 mb-2">
              Mis Checklists ({instances.length})
            </h3>
            <div className="space-y-1">
              {instances.map(i => (
                <button
                  key={i.id}
                  onClick={() => { setSelectedInstance(i.id); setSelectedTemplate(i.templateId); }}
                  className={`w-full text-left px-3 py-2 rounded-lg text-sm transition-colors ${
                    selectedInstance === i.id
                      ? 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300'
                      : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="font-medium">{i.entityName}</span>
                    <span className={`text-xs px-1.5 py-0.5 rounded ${
                      i.status === 'completed' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400'
                    }`}>
                      {i.status === 'completed' ? 'Completado' : 'En progreso'}
                    </span>
                  </div>
                  <span className="text-xs text-gray-400">{i.answerCount} respuestas</span>
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Panel derecho: Detalle */}
        <div className="lg:col-span-2 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
          {/* Vista de template (sin instancia seleccionada) */}
          {templateDetail && !selectedInstance && (
            <div className="space-y-4">
              <div>
                <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">{templateDetail.name}</h3>
                <p className="text-sm text-gray-500 dark:text-gray-400">{templateDetail.description}</p>
              </div>

              {/* Formulario para crear instancia */}
              <div className="flex gap-2 items-end border-t border-gray-200 dark:border-gray-700 pt-4">
                <div className="flex-1">
                  <label className="text-xs text-gray-500">Nombre de la entidad</label>
                  <input
                    type="text"
                    value={newEntity.name}
                    onChange={e => setNewEntity(prev => ({ ...prev, name: e.target.value }))}
                    placeholder="Ej: Apple, Amundi MSCI World..."
                    className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100"
                  />
                </div>
                <select
                  value={newEntity.type}
                  onChange={e => setNewEntity(prev => ({ ...prev, type: e.target.value }))}
                  className="px-2 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100"
                >
                  <option value="company">Empresa</option>
                  <option value="fund">Fondo</option>
                </select>
                <button
                  onClick={() => newEntity.name && createInstance.mutate({
                    templateId: templateDetail.id,
                    entityType: newEntity.type,
                    entityName: newEntity.name,
                  })}
                  disabled={!newEntity.name}
                  className="px-3 py-1.5 text-sm bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
                >
                  <ChevronRight className="h-4 w-4" />
                </button>
              </div>

              {/* Preview de items */}
              <div className="space-y-3">
                {Object.entries(groupBy(templateDetail.items, i => i.section)).map(([section, items]) => (
                  <div key={section}>
                    <h4 className="text-sm font-semibold text-gray-600 dark:text-gray-400 mb-1">{section}</h4>
                    {items.map(item => (
                      <div key={item.id} className="flex items-center gap-2 py-1 text-sm text-gray-500 dark:text-gray-400">
                        <div className="h-4 w-4 border border-gray-300 dark:border-gray-600 rounded" />
                        <span>{item.text}</span>
                        {item.helpText && (
                          <HelpCircle className="h-3 w-3 text-gray-400" aria-label={item.helpText} />
                        )}
                      </div>
                    ))}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Vista de instancia (rellenando) */}
          {instanceDetail && activeTemplateDetail && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">{instanceDetail.entityName}</h3>
                  <p className="text-sm text-gray-500">{activeTemplateDetail.name}</p>
                </div>
                {instanceDetail.status !== 'completed' && (
                  <button
                    onClick={() => completeMutation.mutate(instanceDetail.id)}
                    className="flex items-center gap-1 px-3 py-1.5 text-sm bg-green-600 text-white rounded-lg hover:bg-green-700"
                  >
                    <Check className="h-4 w-4" /> Completar
                  </button>
                )}
              </div>

              <div className="space-y-4">
                {Object.entries(groupBy(activeTemplateDetail.items, i => i.section)).map(([section, items]) => (
                  <div key={section} className="border-t border-gray-200 dark:border-gray-700 pt-3">
                    <h4 className="text-sm font-semibold text-indigo-600 dark:text-indigo-400 mb-2">{section}</h4>
                    <div className="space-y-2">
                      {items.map(item => {
                        const answer = getAnswer(item.id);
                        return (
                          <div key={item.id} className="flex items-start gap-3 py-1">
                            {item.itemType === 'boolean' && (
                              <button
                                onClick={() => answerMutation.mutate({
                                  instanceId: instanceDetail.id,
                                  templateItemId: item.id,
                                  value: answer?.value === 'true' ? 'false' : 'true',
                                })}
                                className={`mt-0.5 h-5 w-5 rounded border flex items-center justify-center flex-shrink-0 ${
                                  answer?.value === 'true'
                                    ? 'bg-green-500 border-green-500 text-white'
                                    : answer?.value === 'false'
                                    ? 'bg-red-500 border-red-500 text-white'
                                    : 'border-gray-300 dark:border-gray-600'
                                }`}
                              >
                                {answer?.value === 'true' && <Check className="h-3 w-3" />}
                                {answer?.value === 'false' && <X className="h-3 w-3" />}
                              </button>
                            )}
                            {item.itemType === 'rating' && (
                              <div className="flex gap-0.5 mt-0.5 flex-shrink-0">
                                {[1, 2, 3, 4, 5].map(n => (
                                  <button
                                    key={n}
                                    onClick={() => answerMutation.mutate({
                                      instanceId: instanceDetail.id,
                                      templateItemId: item.id,
                                      value: n.toString(),
                                    })}
                                  >
                                    <Star className={`h-4 w-4 ${
                                      Number(answer?.value) >= n ? 'fill-yellow-400 text-yellow-400' : 'text-gray-300'
                                    }`} />
                                  </button>
                                ))}
                              </div>
                            )}
                            {(item.itemType === 'text' || item.itemType === 'numeric') && (
                              <FileText className="h-4 w-4 mt-0.5 text-gray-400 flex-shrink-0" />
                            )}
                            <div className="flex-1 min-w-0">
                              <span className="text-sm text-gray-700 dark:text-gray-300">{item.text}</span>
                              {item.helpText && (
                                <p className="text-xs text-gray-400 mt-0.5">{item.helpText}</p>
                              )}
                              {(item.itemType === 'text' || item.itemType === 'numeric') && (
                                <input
                                  type={item.itemType === 'numeric' ? 'number' : 'text'}
                                  defaultValue={answer?.value ?? ''}
                                  onBlur={e => e.target.value && answerMutation.mutate({
                                    instanceId: instanceDetail.id,
                                    templateItemId: item.id,
                                    value: e.target.value,
                                  })}
                                  placeholder={item.itemType === 'numeric' ? '0' : 'Escribe aquí...'}
                                  className="mt-1 w-full px-2 py-1 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100"
                                />
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {!templateDetail && !instanceDetail && (
            <div className="text-center py-12 text-gray-400">
              <ClipboardCheck className="h-12 w-12 mx-auto mb-3 opacity-30" />
              <p>Selecciona un template o checklist para empezar</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function groupBy<T>(arr: T[], keyFn: (item: T) => string): Record<string, T[]> {
  return arr.reduce((acc, item) => {
    const key = keyFn(item);
    (acc[key] ??= []).push(item);
    return acc;
  }, {} as Record<string, T[]>);
}
