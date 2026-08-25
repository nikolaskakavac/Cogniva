import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getDocuments } from '../api/documents'
import { useAuth } from '../auth/AuthContext'

export function DashboardPage() {
  const { user } = useAuth()
  const [documentCount, setDocumentCount] = useState<number | null>(null)
  const [error, setError] = useState(false)

  useEffect(() => {
    getDocuments()
      .then((documents) => setDocumentCount(documents.length))
      .catch(() => setError(true))
  }, [])

  return (
    <section className="dashboard-placeholder">
      <p className="eyebrow">Lični radni prostor</p>
      <h1>Dobrodošli, {user?.firstName}</h1>
      <p>Vaš Cogniva prostor je spreman za rad sa dokumentima.</p>
      <Link className="dashboard-metric" to="/app/documents">
        <span>Dokumenti</span>
        <strong>{error ? '—' : documentCount ?? '…'}</strong>
        <p>Otpremite i upravljajte dokumentima koje Cogniva koristi kao izvor znanja.</p>
      </Link>
    </section>
  )
}
