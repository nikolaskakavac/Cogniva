import { apiClient } from './client'
import type { ChatMessage, ConversationDetails, ConversationListItem } from '../types/conversations'

export async function getConversations() {
  const response = await apiClient.get<ConversationListItem[]>('/api/conversations')
  return response.data
}

export async function getConversation(id: string) {
  const response = await apiClient.get<ConversationDetails>(`/api/conversations/${id}`)
  return response.data
}

export async function createConversation(title: string, documentIds: string[]) {
  const response = await apiClient.post<ConversationDetails>('/api/conversations', { title, documentIds })
  return response.data
}

export async function sendMessage(conversationId: string, content: string) {
  const response = await apiClient.post<ChatMessage>(
    `/api/conversations/${conversationId}/messages`,
    { content },
    { timeout: 10 * 60_000 },
  )
  return response.data
}

export async function retryMessage(conversationId: string, messageId: string) {
  const response = await apiClient.post<ChatMessage>(
    `/api/conversations/${conversationId}/messages/${messageId}/retry`,
    undefined,
    { timeout: 10 * 60_000 },
  )
  return response.data
}
