import { type ChangeEvent, type DragEvent, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { deleteDocument, getDocuments, uploadDocument } from '../api/documents'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import type { DocumentListItem } from '../types/documents'
import { documentStatusLabels } from '../types/documents'
import { getApiErrorMessage } from '../utils/getApiErrorMessage'

const maximumFileSize = 20 * 1024 * 1024

function formatDate(value: string) {
  return new Intl.DateTimeFormat('sr-Latn-RS', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function DocumentsPage() {
  const inputRef = useRef<HTMLInputElement>(null)
  const [documents, setDocuments] = useState<DocumentListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [dragging, setDragging] = useState(false)
  const [documentToDelete, setDocumentToDelete] = useState<DocumentListItem | null>(null)
  const [deleting, setDeleting] = useState(false)

  async function loadDocuments() {
    setLoading(true)
    setError(null)
    try {
      setDocuments(await getDocuments())
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadDocuments()
  }, [])

  async function handleFile(file?: File) {
    if (!file) return
    setNotice(null)
    setError(null)
    const extension = file.name.toLowerCase().slice(file.name.lastIndexOf('.'))
    if (!['.pdf', '.docx'].includes(extension)) {
      setError('Podržani su samo PDF i DOCX dokumenti.')
      return
    }
    if (file.size === 0) {
      setError('Izabrani dokument je prazan.')
      return
    }
    if (file.size > maximumFileSize) {
      setError('Dokument ne može biti veći od 20 MB.')
      return
    }

    setUploading(true)
    try {
      await uploadDocument(file)
      setNotice('Dokument je uspešno otpremljen.')
      await loadDocuments()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setUploading(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  function handleInput(event: ChangeEvent<HTMLInputElement>) {
    void handleFile(event.target.files?.[0])
  }

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setDragging(false)
    void handleFile(event.dataTransfer.files[0])
  }

  async function confirmDelete() {
    if (!documentToDelete) return
    setDeleting(true)
    setError(null)
    try {
      await deleteDocument(documentToDelete.id)
      setDocuments((current) => current.filter((item) => item.id !== documentToDelete.id))
      setNotice('Dokument je obrisan.')
      setDocumentToDelete(null)
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setDeleting(false)
    }
  }

  return (
    <section className="documents-page">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Biblioteka</p>
          <h1>Dokumenti</h1>
          <p>Upravljajte dokumentima koje želite da analizirate pomoću Cognive.</p>
        </div>
        <button className="button button-primary" type="button" onClick={() => inputRef.current?.click()} disabled={uploading}>
          Otpremi dokument
        </button>
      </div>

      <input ref={inputRef} className="visually-hidden" type="file" accept=".pdf,.docx" onChange={handleInput} />
      <div
        className={`upload-zone${dragging ? ' is-dragging' : ''}`}
        onDragEnter={(event) => { event.preventDefault(); setDragging(true) }}
        onDragOver={(event) => event.preventDefault()}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
      >
        <strong>{uploading ? 'Otpremanje dokumenta…' : 'Prevucite PDF ili DOCX dokument ovde'}</strong>
        <span>ili</span>
        <button className="text-button" type="button" onClick={() => inputRef.current?.click()} disabled={uploading}>
          Izaberite dokument
        </button>
        <small>PDF ili DOCX, maksimalno 20 MB</small>
      </div>

      {notice && <p className="notice success" role="status">{notice}</p>}
      {error && <div className="notice error" role="alert"><span>{error}</span><button type="button" onClick={() => void loadDocuments()}>Pokušaj ponovo</button></div>}

      {loading ? (
        <p className="page-state">Učitavanje dokumenata…</p>
      ) : documents.length === 0 ? (
        <div className="empty-state">
          <h2>Još uvek nemate dokumente.</h2>
          <p>Otpremite prvi PDF ili DOCX dokument kako biste počeli.</p>
          <button className="button button-primary" type="button" onClick={() => inputRef.current?.click()}>
            Otpremi prvi dokument
          </button>
        </div>
      ) : (
        <div className="documents-table-wrap">
          <table className="documents-table">
            <thead><tr><th>Naziv</th><th>Tip</th><th>Status</th><th>Datum otpremanja</th><th>Akcije</th></tr></thead>
            <tbody>
              {documents.map((document) => (
                <tr key={document.id}>
                  <td><strong>{document.name}</strong><small>{document.originalFileName}</small></td>
                  <td>{document.fileType}</td>
                  <td><span className={`status status-${document.status.toLowerCase()}`}>{documentStatusLabels[document.status]}</span></td>
                  <td>{formatDate(document.uploadedAt)}</td>
                  <td><div className="row-actions"><Link to={`/app/documents/${document.id}`}>Otvori</Link><button type="button" onClick={() => setDocumentToDelete(document)}>Obriši</button></div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <ConfirmDialog open={Boolean(documentToDelete)} title="Obriši dokument?" description="Ova radnja je nepovratna." confirming={deleting} onCancel={() => setDocumentToDelete(null)} onConfirm={() => void confirmDelete()} />
    </section>
  )
}
