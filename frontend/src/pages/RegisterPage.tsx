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
          <h1>Create account</h1>
          <p>Start building your private document workspace.</p>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="form-row">
            <label>
              First name
              <input required maxLength={100} value={firstName} onChange={(event) => setFirstName(event.target.value)} />
            </label>
            <label>
              Last name
              <input required maxLength={100} value={lastName} onChange={(event) => setLastName(event.target.value)} />
            </label>
          </div>
          <label>
            Email
            <input autoComplete="email" type="email" required value={email} onChange={(event) => setEmail(event.target.value)} />
          </label>
          <label>
            Password
            <input autoComplete="new-password" type="password" required minLength={8} value={password} onChange={(event) => setPassword(event.target.value)} />
          </label>
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="button button-primary" type="submit" disabled={submitting}>
            {submitting ? 'Creating account…' : 'Create account'}
          </button>
        </form>
        <p className="auth-switch">Already registered? <Link to="/login">Sign in</Link></p>
      </section>
    </main>
  )
}
