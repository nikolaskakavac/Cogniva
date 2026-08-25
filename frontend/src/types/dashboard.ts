import type { ConversationListItem } from './conversations'
import type { DocumentListItem } from './documents'

export interface DashboardData {
  documentCount: number
  readyDocumentCount: number
  conversationCount: number
  recentDocuments: DocumentListItem[]
  recentConversations: ConversationListItem[]
}
