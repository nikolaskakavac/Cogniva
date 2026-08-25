import { type FormEvent, type KeyboardEvent, useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { createConversation, getConversation, getConversations, retryMessage, sendMessage } from '../api/conversations'
import { getDocuments } from '../api/documents'
import type { ChatMessage, ConversationDetails, ConversationListItem } from '../types/conversations'
import type { DocumentListItem } from '../types/documents'
import { getApiErrorMessage } from '../utils/getApiErrorMessage'

function formatShortDate(value: string) {
  return new Intl.DateTimeFormat('sr-Latn-RS', { dateStyle: 'medium' }).format(new Date(value))
}

export function ChatPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const composerRef = useRef<HTMLTextAreaElement>(null)
  const [conversations, setConversations] = useState<ConversationListItem[]>([])
  const [conversation, setConversation] = useState<ConversationDetails | null>(null)
  const [documents, setDocuments] = useState<DocumentListItem[]>([])
  const [selectedDocumentIds, setSelectedDocumentIds] = useState<string[]>([])
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [creating, setCreating] = useState(false)
  const [sending, setSending] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [slowResponse, setSlowResponse] = useState(false)

  async function loadSidebar() {
    const [conversationItems, documentItems] = await Promise.all([getConversations(), getDocuments()])
    setConversations(conversationItems)
    setDocuments(documentItems)
  }

  useEffect(() => {
    setLoading(true)
    setError(null)
    Promise.all([
      loadSidebar(),
      id ? getConversation(id).then(setConversation) : Promise.resolve(setConversation(null)),
    ]).catch((requestError) => setError(getApiErrorMessage(requestError)))
      .finally(() => setLoading(false))
  }, [id])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [conversation?.messages.length, sending])

  useEffect(() => {
    if (!sending) { setSlowResponse(false); return }
    const timeout = window.setTimeout(() => setSlowResponse(true), 5_000)
    return () => window.clearTimeout(timeout)
  }, [sending])

  useEffect(() => {
    const textarea = composerRef.current
    if (!textarea) return
    textarea.style.height = 'auto'
    textarea.style.height = `${Math.min(textarea.scrollHeight, 160)}px`
  }, [content])

  const readyDocuments = documents.filter((document) => document.status === 'Ready')

  function toggleDocument(documentId: string) {
    setSelectedDocumentIds((current) => current.includes(documentId)
      ? current.filter((item) => item !== documentId)
      : [...current, documentId])
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    if (selectedDocumentIds.length === 0 || creating) return
    setCreating(true)
    setError(null)
    try {
      const created = await createConversation(title.trim(), selectedDocumentIds)
      setTitle('')
      setSelectedDocumentIds([])
      await loadSidebar()
      navigate(`/app/chat/${created.id}`)
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setCreating(false)
    }
  }

  async function handleSend() {
    const question = content.trim()
    if (!id || !conversation || !question || sending) return

    const optimisticMessage: ChatMessage = {
      id: `pending-${Date.now()}`,
      role: 'User',
      content: question,
      createdAt: new Date().toISOString(),
      sources: [],
    }
    setContent('')
    setSending(true)
    setError(null)
    setConversation((current) => current
      ? { ...current, messages: [...current.messages, optimisticMessage] }
      : current)

    try {
      const assistantMessage = await sendMessage(id, question)
      const refreshed = await getConversation(id)
      setConversation(refreshed.messages.some((message) => message.id === assistantMessage.id)
        ? refreshed
        : { ...refreshed, messages: [...refreshed.messages, assistantMessage] })
      await loadSidebar()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
      setConversation(await getConversation(id).catch(() => conversation))
    } finally {
      setSending(false)
    }
  }

  async function handleRetry(messageId: string) {
    if (!id || sending) return
    setSending(true)
    setError(null)
    try {
      await retryMessage(id, messageId)
      setConversation(await getConversation(id))
      await loadSidebar()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setSending(false)
    }
  }

  function handleComposerKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      void handleSend()
    }
  }

  return (
    <section className="chat-workspace">
      <aside className="chat-sidebar">
        <div className="chat-sidebar-heading">
          <div><p className="eyebrow">AI radni prostor</p><h1>Razgovori</h1></div>
          <button className="button button-primary" type="button" onClick={() => navigate('/app/chat')}>Novi razgovor</button>
        </div>
        <div className="conversation-list">
          {conversations.length === 0 && <p className="muted-copy">Još uvek nemate razgovore.</p>}
          {conversations.map((item) => (
            <Link className={`conversation-list-item${id === item.id ? ' active' : ''}`} to={`/app/chat/${item.id}`} key={item.id}>
              <strong>{item.title}</strong>
              <span>{item.messageCount} poruka · {formatShortDate(item.updatedAt)}</span>
              <small>{item.documentNames.join(', ')}</small>
            </Link>
          ))}
        </div>
      </aside>

      <div className="chat-main">
        {error && <div className="notice error" role="alert"><span>{error}</span><button type="button" onClick={() => setError(null)}>Zatvori</button></div>}
        {loading ? <p className="page-state">Učitavanje razgovora…</p> : !id ? (
          <form className="new-conversation" onSubmit={handleCreate}>
            <p className="eyebrow">Novi razgovor</p>
            <h2>Izaberite izvore znanja</h2>
            <p>Cogniva će odgovarati samo na osnovu dokumenata koje povežete sa razgovorom.</p>
            {readyDocuments.length === 0 ? (
              <div className="empty-state compact">
                <h3>Nema spremnih dokumenata.</h3>
                <p>Najpre obradite najmanje jedan dokument da biste započeli AI razgovor.</p>
                <Link className="button button-primary" to="/app/documents">Idi na dokumente</Link>
              </div>
            ) : <>
              <label className="field-label">Naziv razgovora <span>(opciono)</span>
                <input value={title} onChange={(event) => setTitle(event.target.value)} maxLength={255} placeholder="Na primer: Analiza ugovora" />
              </label>
              <div className="document-picker">
                {readyDocuments.map((document) => (
                  <label className={`document-choice${selectedDocumentIds.includes(document.id) ? ' selected' : ''}`} key={document.id}>
                    <input type="checkbox" checked={selectedDocumentIds.includes(document.id)} onChange={() => toggleDocument(document.id)} />
                    <span><strong>{document.originalFileName}</strong><small>Spreman za AI analizu</small></span>
                  </label>
                ))}
              </div>
              <button className="button button-primary" type="submit" disabled={creating || selectedDocumentIds.length === 0}>
                {creating ? 'Kreiranje razgovora…' : 'Započni razgovor'}
              </button>
            </>}
          </form>
        ) : conversation ? <>
          <header className="chat-header">
            <div><p className="eyebrow">Razgovor</p><h2>{conversation.title}</h2></div>
            <div className="chat-documents">{conversation.documents.map((document) => <Link to={`/app/documents/${document.id}`} key={document.id}>{document.originalFileName}</Link>)}</div>
          </header>
          <div className="message-thread" aria-live="polite">
            {conversation.messages.length === 0 && <div className="chat-welcome"><h3>Pitajte Cognivu</h3><p>Postavite pitanje o povezanim dokumentima. Odgovor će sadržati korišćene izvore.</p></div>}
            {conversation.messages.map((message, index) => (
              <article className={`chat-message ${message.role === 'User' ? 'user' : 'assistant'}`} key={message.id}>
                <span className="message-author">{message.role === 'User' ? 'Vi' : 'Cogniva'}</span>
                <p>{message.content}</p>
                {message.role === 'User' && index === conversation.messages.length - 1 && !sending && <button className="retry-button" type="button" onClick={() => void handleRetry(message.id)}>Pokušaj ponovo</button>}
                {message.sources.length > 0 && <div className="message-sources"><strong>Izvori</strong><div>{message.sources.map((source) => (
                  <Link to={`/app/documents/${source.documentId}`} key={source.documentChunkId}>
                    <span>{source.documentName}</span>
                    <small>{source.pageNumber ? `Stranica ${source.pageNumber}` : `Deo ${source.chunkIndex + 1}`}</small>
                    <p>{source.snippet}</p>
                  </Link>
                ))}</div></div>}
              </article>
            ))}
            {sending && <div className="chat-thinking"><div><span /><span /><span /> Cogniva priprema odgovor…</div>{slowResponse && <small>Lokalnom AI modelu može biti potrebno malo više vremena.</small>}</div>}
            <div ref={messagesEndRef} />
          </div>
          <div className="chat-composer">
            <textarea aria-label="Poruka za Cognivu" ref={composerRef} value={content} onChange={(event) => setContent(event.target.value)} onKeyDown={handleComposerKeyDown} disabled={sending} maxLength={4000} rows={1} placeholder="Pitajte nešto o izabranim dokumentima…" />
            <button className="button button-primary" type="button" onClick={() => void handleSend()} disabled={sending || !content.trim()}>{sending ? 'Odgovaranje…' : 'Pošalji'}</button>
            <small>Enter šalje poruku · Shift+Enter dodaje novi red</small>
          </div>
        </> : <div className="empty-state"><h2>Razgovor nije pronađen.</h2><Link className="button button-primary" to="/app/chat">Novi razgovor</Link></div>}
      </div>
    </section>
  )
}
