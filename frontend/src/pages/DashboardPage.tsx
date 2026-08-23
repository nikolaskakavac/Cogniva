import { useAuth } from '../auth/AuthContext'

export function DashboardPage() {
  const { user } = useAuth()

  return (
    <section className="dashboard-placeholder">
      <p className="eyebrow">Lični radni prostor</p>
      <h1>Dobrodošli, {user?.firstName}</h1>
      <p>
        Vaš Cogniva nalog je spreman. Upravljanje dokumentima biće dostupno u narednoj fazi.
      </p>
    </section>
  )
}
