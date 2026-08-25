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
      <aside className="app-sidebar">
        <NavLink className="brand" to="/app">Cogniva</NavLink>
        <nav className="app-nav" aria-label="Glavna navigacija">
          <NavLink end to="/app"><span aria-hidden="true">01</span>Kontrolna tabla</NavLink>
          <NavLink to="/app/documents"><span aria-hidden="true">02</span>Dokumenti</NavLink>
          <NavLink to="/app/chat"><span aria-hidden="true">03</span>Razgovori</NavLink>
        </nav>
        <div className="account-actions">
          <span className="account-avatar" aria-hidden="true">{user ? `${user.firstName[0]}${user.lastName[0]}` : 'C'}</span>
          <span className="account-copy"><strong>{user ? `${user.firstName} ${user.lastName}` : 'Korisnik'}</strong><small>{user?.email}</small></span>
          <button className="button button-ghost" type="button" onClick={handleLogout}>
            Odjavi se
          </button>
        </div>
      </aside>
      <main className="app-content">
        <Outlet />
      </main>
    </div>
  )
}
