import { Link } from 'react-router-dom'

export function HomePage() {
  return (
    <main className="home">
      <section>
        <p className="eyebrow">Document intelligence</p>
        <h1>Cogniva</h1>
        <p className="description">
          Turn documents into knowledge. Upload, analysis, and grounded AI
          capabilities will be added in the next development phases.
        </p>
        <div className="home-actions">
          <Link className="button button-primary" to="/register">Get started</Link>
          <Link className="button button-secondary" to="/login">Sign in</Link>
        </div>
      </section>
    </main>
  )
}
