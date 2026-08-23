import { useAuth } from '../auth/AuthContext'

export function DashboardPage() {
  const { user } = useAuth()

  return (
    <section className="dashboard-placeholder">
      <p className="eyebrow">Personal workspace</p>
      <h1>Welcome, {user?.firstName}</h1>
      <p>
        Your Cogniva account is ready. Document management will arrive in the next phase.
      </p>
    </section>
  )
}
