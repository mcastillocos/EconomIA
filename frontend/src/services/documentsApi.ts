import axios from 'axios';
import type { UploadedDocument } from '../types/document';

const api = axios.create({
  baseURL: '/api',
});

export const documentsApi = {
  getAll: async (): Promise<UploadedDocument[]> => {
    const { data } = await api.get('/documents');
    return data;
  },

  getById: async (id: string): Promise<UploadedDocument> => {
    const { data } = await api.get(`/documents/${id}`);
    return data;
  },

  upload: async (file: File, entityType: string, entityId?: string, source?: string): Promise<UploadedDocument> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('entityType', entityType);
    if (entityId) formData.append('entityId', entityId);
    if (source) formData.append('source', source);

    const { data } = await api.post('/documents/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return data;
  },

  reprocess: async (id: string): Promise<void> => {
    await api.post(`/documents/${id}/reprocess`);
  },
};
