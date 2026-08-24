import { useState, FormEvent } from 'react'
import { requestMagicLink } from '../auth'
import './SignIn.css'

export function SignIn() {
  const [email, setEmail] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    const trimmedEmail = email.trim()
    if (!trimmedEmail) {
      setError('Please enter your email address')
      return
    }

    setError('')
    setIsSending(true)

    try {
      await requestMagicLink(trimmedEmail)
      setSent(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setIsSending(false)
    }
  }

  return (
    <div className="signin-container">
      <div className="signin-card">
        <h1>🎩 Names Out of a Hat</h1>

        {sent ? (
          <div className="signin-sent">
            <h2>Check your email</h2>
            <p>
              If <strong>{email.trim()}</strong> can receive mail, a sign-in link is on its way. The
              link works once and expires in 15 minutes.
            </p>
            <button className="signin-secondary" onClick={() => setSent(false)}>
              Use a different address
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <p className="signin-intro">
              Enter your email and we'll send you a link to sign in. No password required.
            </p>

            <label htmlFor="email">Email address</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              autoComplete="email"
              autoFocus
              disabled={isSending}
            />

            {error && <div className="signin-error">{error}</div>}

            <button type="submit" className="signin-primary" disabled={isSending}>
              {isSending ? 'Sending...' : 'Email me a sign-in link'}
            </button>
          </form>
        )}
      </div>
    </div>
  )
}
