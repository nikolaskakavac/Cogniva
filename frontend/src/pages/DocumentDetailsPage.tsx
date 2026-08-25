import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getDocument, processDocument } from '../api/documents'
import type { DocumentDetails } from '../types/documents'
import { documentStatusLabels } from '../types/documents'
import { getApiErrorMessage } from '../utils/getApiErrorMessage'

function formatDate(value: string | null) {
  if (!value) return 'Nije obrađen'
  return new Intl.DateTimeFormat('sr-Latn-RS', { dateStyle: 'long', timeStyle: 'short' }).format(new Date(value))
}

export function DocumentDetailsPage() {
  const { id } = useParams()
  const [document, setDocument] = useState<DocumentDetails | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [processing, setProcessing] = useState(false)

  async function loadDocument() {
    if (!id) return
    setLoading(true)
    setError(null)
    try {
      setDocument(await getDocument(id))
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void loadDocument() }, [id])

  async function handleProcessing() {
    if (!id || processing) return
    setProcessing(true)
    setError(null)
    setNotice('Dokument se obrađuje. Ovo može potrajati…')
    setDocument((current) => current ? { ...current, status: 'Processing' } : current)
    try {
      const updated = await processDocument(id)
      setDocument(updated)
      setNotice(updated.status === 'Ready'
        ? 'Dokument je uspešno obrađen.'
        : 'Obrada dokumenta nije uspela.')
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
      await loadDocument()
    } finally {
      setProcessing(false)
    }
  }

  if (loading) return <p className="page-state">Učitavanje dokumenta…</p>
  if (error || !document) return <div className="empty-state"><h2>Dokument trenutno nije dostupan.</h2><p>{error}</p><button className="button button-secondary" type="button" onClick={() => void loadDocument()}>Pokušaj ponovo</button></div>

  return (
    <section className="document-details">
      <Link className="back-link" to="/app/documents">← Nazad na dokumente</Link>
      <div className="page-heading">
        <div><p className="eyebrow">Dokument</p><h1>{document.name}</h1></div>
        <span className={`status status-${document.status.toLowerCase()}`}>{documentStatusLabels[document.status]}</span>
      </div>
      <dl className="details-list">
        <div><dt>Originalni naziv</dt><dd>{document.originalFileName}</dd></div>
        <div><dt>Tip</dt><dd>{document.fileType}</dd></div>
        <div><dt>Status</dt><dd>{documentStatusLabels[document.status]}</dd></div>
        <div><dt>Datum otpremanja</dt><dd>{formatDate(document.uploadedAt)}</dd></div>
        <div><dt>Datum obrade</dt><dd>{formatDate(document.processedAt)}</dd></div>
        <div><dt>Broj delova teksta</dt><dd>{document.chunkCount}</dd></div>
      </dl>
      <section className="analysis-placeholder">
        <p className="eyebrow">AI analiza</p>
        {document.status === 'Uploaded' && <>
          <h2>Dokument još nije obrađen.</h2>
          <p>Da biste koristili AI analizu, dokument je potrebno prvo obraditi.</p>
          <button className="button button-primary" type="button" onClick={() => void handleProcessing()} disabled={processing}>Obradi dokument</button>
        </>}
        {document.status === 'Processing' && <>
          <h2>Obrada dokumenta je u toku…</h2>
          <p>Sačekajte dok Cogniva izdvaja tekst i priprema dokument za analizu.</p>
          <button className="button button-primary" type="button" disabled>Dokument se obrađuje…</button>
        </>}
        {document.status === 'Ready' && <>
          <h2>Dokument je uspešno obrađen.</h2>
          <p>Dokument je spreman za AI analizu i sadrži {document.chunkCount} delova teksta.</p>
        </>}
        {document.status === 'Failed' && <>
          <h2>Obrada dokumenta nije uspela.</h2>
          <p>{document.processingError ?? 'Pokušajte ponovo ili otpremite drugi dokument.'}</p>
          <button className="button button-primary" type="button" onClick={() => void handleProcessing()} disabled={processing}>{processing ? 'Dokument se obrađuje…' : 'Pokušaj ponovo'}</button>
        </>}
        {notice && <p className={`notice ${document.status === 'Ready' ? 'success' : document.status === 'Failed' ? 'error' : ''}`} role="status">{notice}</p>}
      </section>
    </section>
  )
}
