import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getDashboard } from '../api/dashboard'
import type { DashboardData } from '../types/dashboard'
import { documentStatusLabels } from '../types/documents'
import { getApiErrorMessage } from '../utils/getApiErrorMessage'

function formatDate(value: string) {
  return new Intl.DateTimeFormat('sr-Latn-RS', { dateStyle: 'medium' }).format(new Date(value))
}

export function DashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => { getDashboard().then(setData).catch((requestError) => setError(getApiErrorMessage(requestError))) }, [])

  if (error) return <div className="empty-state"><h2>Kontrolna tabla trenutno nije dostupna.</h2><p>{error}</p><button className="button button-secondary" type="button" onClick={() => window.location.reload()}>Pokušaj ponovo</button></div>
  if (!data) return <p className="page-state">Učitavanje kontrolne table…</p>

  return <section className="dashboard-page">
    <div className="page-heading dashboard-heading"><div><p className="eyebrow">Lični radni prostor</p><h1>Kontrolna tabla</h1><p>Pregled dokumenata i AI razgovora u Cognivi.</p></div><div className="dashboard-actions"><Link className="button button-secondary" to="/app/documents">Otpremi dokument</Link><Link className="button button-primary" to="/app/chat">Novi razgovor</Link></div></div>
    <div className="dashboard-stats">
      <Link to="/app/documents"><span>Dokumenti</span><strong>{data.documentCount}</strong></Link>
      <Link to="/app/documents"><span>Obrađeni dokumenti</span><strong>{data.readyDocumentCount}</strong></Link>
      <Link to="/app/chat"><span>Razgovori</span><strong>{data.conversationCount}</strong></Link>
    </div>
    <div className="dashboard-sections">
      <section><div className="section-heading"><h2>Poslednji dokumenti</h2><Link to="/app/documents">Prikaži sve</Link></div>
        {data.recentDocuments.length === 0 ? <p className="dashboard-empty">Još uvek nema dokumenata. Otpremite prvi dokument da biste počeli.</p> : <div className="recent-list">{data.recentDocuments.map((document) => <Link to={`/app/documents/${document.id}`} key={document.id}><span><strong>{document.originalFileName}</strong><small>{formatDate(document.uploadedAt)}</small></span><em>{documentStatusLabels[document.status]}</em></Link>)}</div>}
      </section>
      <section><div className="section-heading"><h2>Poslednji razgovori</h2><Link to="/app/chat">Prikaži sve</Link></div>
        {data.recentConversations.length === 0 ? <p className="dashboard-empty">Još uvek nema razgovora. Izaberite spreman dokument i pitajte Cognivu.</p> : <div className="recent-list">{data.recentConversations.map((conversation) => <Link to={`/app/chat/${conversation.id}`} key={conversation.id}><span><strong>{conversation.title}</strong><small>{formatDate(conversation.updatedAt)}</small></span><em>{conversation.messageCount} poruka</em></Link>)}</div>}
      </section>
    </div>
  </section>
}
