export interface ConversationListItem {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  messageCount: number
  documentNames: string[]
}

export interface ConversationDocument {
  id: string
  name: string
  originalFileName: string
}

export interface MessageSource {
  documentId: string
  documentChunkId: string
  documentName: string
  chunkIndex: number
  pageNumber: number | null
  snippet: string
  relevanceScore: number
}

export interface ChatMessage {
  id: string
  role: 'User' | 'Assistant'
  content: string
  createdAt: string
  sources: MessageSource[]
}

export interface ConversationDetails {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  documents: ConversationDocument[]
  messages: ChatMessage[]
}
