import { apiClient } from './client'
import type {
  DocumentDetails,
  DocumentListItem,
  UploadDocumentResponse,
} from '../types/documents'

export async function getDocuments() {
  const response = await apiClient.get<DocumentListItem[]>('/api/documents')
  return response.data
}

export async function getDocument(id: string) {
  const response = await apiClient.get<DocumentDetails>(`/api/documents/${id}`)
  return response.data
}

export async function uploadDocument(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  const response = await apiClient.post<UploadDocumentResponse>('/api/documents/upload', formData, {
    timeout: 60_000,
  })
  return response.data
}

export async function deleteDocument(id: string) {
  await apiClient.delete(`/api/documents/${id}`)
}

export async function processDocument(id: string) {
  const response = await apiClient.post<DocumentDetails>(`/api/documents/${id}/process`, undefined, {
    timeout: 10 * 60_000,
  })
  return response.data
}

export async function summarizeDocument(id: string) {
  const response = await apiClient.post<DocumentDetails>(`/api/documents/${id}/summarize`, undefined, {
    timeout: 30 * 60_000,
  })
  return response.data
}
