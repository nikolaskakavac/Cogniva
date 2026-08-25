export type DocumentStatus = 'Uploaded' | 'Processing' | 'Ready' | 'Failed'

export interface DocumentListItem {
  id: string
  name: string
  originalFileName: string
  fileType: string
  status: DocumentStatus
  uploadedAt: string
  processedAt: string | null
}

export interface DocumentDetails extends DocumentListItem {
  summary: string | null
  processingError: string | null
  chunkCount: number
}

export interface UploadDocumentResponse {
  id: string
  name: string
  originalFileName: string
  fileType: string
  status: DocumentStatus
  uploadedAt: string
}

export const documentStatusLabels: Record<DocumentStatus, string> = {
  Uploaded: 'Otpremljen',
  Processing: 'Obrada u toku',
  Ready: 'Spreman',
  Failed: 'Neuspešno',
}
