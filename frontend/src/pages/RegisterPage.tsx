import { type FormEvent, useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { getApiErrorMessage } from '../utils/getApiErrorMessage'

export function RegisterPage() {
  const { isAuthenticated, register } = useAuth()
  const navigate = useNavigate()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  if (isAuthenticated) return <Navigate to="/app" replace />

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    if (!firstName.trim()) {
      setError('Unesite ime.')
      return
    }

    if (!lastName.trim()) {
      setError('Unesite prezime.')
      return
    }

    if (!email.trim() || !/^\S+@\S+\.\S+$/.test(email)) {
      setError('Unesite ispravnu email adresu.')
      return
    }

    if (password.length < 8) {
      setError('Lozinka mora imati najmanje 8 karaktera.')
      return
    }

    setSubmitting(true)

    try {
      await register({ firstName, lastName, email, password })
      navigate('/app', { replace: true })
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
          <h1>Kreirajte nalog</h1>
          <p>Napravite svoj lični prostor za rad sa dokumentima.</p>
        </div>
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-row">
            <label>
              Ime
              <input required maxLength={100} value={firstName} onChange={(event) => setFirstName(event.target.value)} />
            </label>
            <label>
              Prezime
              <input required maxLength={100} value={lastName} onChange={(event) => setLastName(event.target.value)} />
            </label>
          </div>
          <label>
            Email adresa
            <input autoComplete="email" type="email" required value={email} onChange={(event) => setEmail(event.target.value)} />
          </label>
          <label>
            Lozinka
            <input autoComplete="new-password" type="password" required minLength={8} value={password} onChange={(event) => setPassword(event.target.value)} />
          </label>
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="button button-primary" type="submit" disabled={submitting}>
            {submitting ? 'Kreiranje naloga…' : 'Kreiraj nalog'}
          </button>
        </form>
        <p className="auth-switch">Već imate nalog? <Link to="/login">Prijavite se</Link></p>
      </section>
    </main>
  )
}
