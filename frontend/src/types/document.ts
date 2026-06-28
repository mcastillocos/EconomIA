export interface UploadedDocument {
  id: string;
  entityType: string;
  entityId?: string;
  fileName: string;
  fileType: string;
  source?: string;
  filePath: string;
  fileSize: number;
  uploadDate: string;
  status: 'pending' | 'processing' | 'completed' | 'failed';
  summary?: string;
  errorMessage?: string;
}
