import { Link } from 'react-router-dom'

export function HomePage() {
  return (
    <main className="home">
      <section>
        <p className="eyebrow">Inteligentna analiza dokumenata</p>
        <h1>Cogniva</h1>
        <p className="description">
          Pretvorite dokumente u znanje. Otpremanje, analiza i AI odgovori
          zasnovani na vašim dokumentima biće dostupni u narednim fazama razvoja.
        </p>
        <div className="home-actions">
          <Link className="button button-primary" to="/register">Započni</Link>
          <Link className="button button-secondary" to="/login">Prijavi se</Link>
        </div>
      </section>
    </main>
  )
}
