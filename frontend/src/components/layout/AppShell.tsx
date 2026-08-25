import { NavLink, Outlet, useNavigate } from 'react-router-dom'
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
        <NavLink className="brand" to="/app">Cogniva</NavLink>
        <nav className="app-nav" aria-label="Glavna navigacija">
          <NavLink end to="/app">Kontrolna tabla</NavLink>
          <NavLink to="/app/documents">Dokumenti</NavLink>
        </nav>
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
