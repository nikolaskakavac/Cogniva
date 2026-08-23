import { type FormEvent, useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { getApiErrorMessage } from '../utils/getApiErrorMessage'

export function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  if (isAuthenticated) return <Navigate to="/app" replace />

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    if (!email.trim()) {
      setError('Unesite email adresu.')
      return
    }

    if (!/^\S+@\S+\.\S+$/.test(email)) {
      setError('Unesite ispravnu email adresu.')
      return
    }

    if (!password) {
      setError('Unesite lozinku.')
      return
    }

    setSubmitting(true)

    try {
      await login({ email, password })
      const destination = (location.state as { from?: { pathname?: string } } | null)
        ?.from?.pathname ?? '/app'
      navigate(destination, { replace: true })
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-panel">
        <Link className="brand" to="/">Cogniva</Link>
        <div className="auth-heading">
          <h1>Prijavite se</h1>
          <p>Nastavite ka svom ličnom prostoru za rad sa dokumentima.</p>
        </div>
        <form onSubmit={handleSubmit} noValidate>
          <label>
            Email adresa
            <input
              autoComplete="email"
              type="email"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </label>
          <label>
            Lozinka
            <input
              autoComplete="current-password"
              type="password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="button button-primary" type="submit" disabled={submitting}>
            {submitting ? 'Prijavljivanje…' : 'Prijavi se'}
          </button>
        </form>
        <p className="auth-switch">Nemate nalog? <Link to="/register">Registrujte se</Link></p>
      </section>
    </main>
  )
}
