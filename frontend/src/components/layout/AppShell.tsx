import { Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'

export function AppShell() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <a className="brand" href="/app">Cogniva</a>
        <div className="account-actions">
          <span>{user?.email}</span>
          <button className="button button-secondary" type="button" onClick={handleLogout}>
            Odjavi se
          </button>
        </div>
      </header>
      <main className="app-content">
        <Outlet />
      </main>
    </div>
  )
}
