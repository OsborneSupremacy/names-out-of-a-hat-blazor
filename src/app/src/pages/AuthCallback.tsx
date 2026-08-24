import { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { redeemMagicLink } from '../auth'
import './AuthCallback.css'

interface AuthCallbackProps {
  onSignedIn: () => void
}

export function AuthCallback({ onSignedIn }: AuthCallbackProps) {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [error, setError] = useState('')

  // StrictMode runs effects twice in development. The token is single-use, so a second redemption
  // would always fail and show a spurious error.
  const hasRedeemed = useRef(false)

  useEffect(() => {
    if (hasRedeemed.current) return
    hasRedeemed.current = true

    const token = searchParams.get('token')

    if (!token) {
      setError('That link is missing its sign-in token.')
      return
    }

    async function redeem() {
      try {
        await redeemMagicLink(token!)

        // Keep the token out of history and out of any Referer header the page might send.
        window.history.replaceState({}, '', '/')

        onSignedIn()
        navigate('/', { replace: true })
      } catch (err) {
        setError(err instanceof Error ? err.message : 'That sign-in link did not work.')
      }
    }

    redeem()
  }, [searchParams, navigate, onSignedIn])

  return (
    <div className="auth-callback">
      {error ? (
        <div className="auth-callback-card">
          <h2>Sign-in failed</h2>
          <p>{error}</p>
          <button onClick={() => navigate('/', { replace: true })}>Back to sign in</button>
        </div>
      ) : (
        <p>Signing you in...</p>
      )}
    </div>
  )
}
