import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { redeemMagicLink } from '../auth'
import './AuthCallback.css'

interface AuthCallbackProps {
  onSignedIn: () => void
}

/**
 * Pulls the sign-in token out of a URL fragment.
 *
 * The fragment is where RequestMagicLinkService now puts it, because a fragment never reaches a
 * server: not ours, not a CDN's access log, not a Referer header. Reading it here is the only way
 * it is ever read.
 */
function readToken(hash: string): string {
  return new URLSearchParams(hash.replace(/^#/, '')).get('token') ?? ''
}

/**
 * Where a sign-in link lands. Redeeming is deliberately behind a button rather than run on load.
 *
 * A login token is single-use, so whoever redeems it first wins and everybody after them is told
 * the link is dead. Mail security scanners fetch links on delivery, and the better ones — Proofpoint
 * and Microsoft's aggressive Safe Links among them — do not merely fetch: they open the page in a
 * real browser and run its JavaScript. A page that redeemed in an effect handed the token to the
 * scanner, which is why sign-in links arriving through a corporate gateway were dead before their
 * owner ever saw them.
 *
 * A click is the cheapest thing a person does that an automated fetch does not. It is not proof of
 * a person — a sandbox driving a browser can synthesise input, and event.isTrusted does not
 * separate that from a real click — so this narrows the problem rather than closing it. The way to
 * close it is a code that never appears in a link at all.
 *
 * Confirmed against Proofpoint in August 2026: a link left untouched through delivery-time scanning
 * was still live when its owner clicked it, so that gateway renders this page without pressing
 * anything on it. That is one vendor's configuration on one day rather than a guarantee, and it is
 * the whole reason the typed-code fallback has not been built. If corporate sign-ins start failing
 * again, that is the thing to build, not a second permitted use of the token — a use count trades
 * away the single-use property to chase an unbounded number of scanner fetches.
 *
 * This is the same shape as the Ask flow, where the emailed link renders a form and only a person
 * can submit it.
 */
export function AuthCallback({ onSignedIn }: AuthCallbackProps) {
  const location = useLocation()
  const navigate = useNavigate()

  // Read once, at the first render, because the effect below is about to take it out of the URL.
  const [token] = useState(() => readToken(location.hash))
  const [error, setError] = useState('')
  const [pending, setPending] = useState(false)

  // A second click while the first is in flight would spend the token and then be told, correctly,
  // that the token was already spent.
  const isRedeeming = useRef(false)

  // Out of the address bar as soon as it has been read. The page can now sit here indefinitely
  // waiting to be clicked, and there is no reason for a live token to be on display while it does.
  useEffect(() => {
    if (window.location.hash) window.history.replaceState({}, '', window.location.pathname)
  }, [])

  async function handleSignIn() {
    if (isRedeeming.current) return
    isRedeeming.current = true
    setPending(true)

    try {
      await redeemMagicLink(token)
      onSignedIn()
      navigate('/', { replace: true })
    } catch (err) {
      isRedeeming.current = false
      setPending(false)
      setError(err instanceof Error ? err.message : 'That sign-in link did not work.')
    }
  }

  const failure = error || (token ? '' : 'That link is missing its sign-in token.')

  return (
    <div className="auth-callback">
      <img
        className="auth-callback-logo"
        src="/logo-horizontal.png"
        alt="Names Out of a Hat"
        width={960}
        height={323}
      />
      {failure ? (
        <div className="auth-callback-card">
          <h2>Sign-in failed</h2>
          <p>{failure}</p>
          <button onClick={() => navigate('/', { replace: true })}>Back to sign in</button>
        </div>
      ) : (
        <div className="auth-callback-card">
          <h2>Finish signing in</h2>
          <p>One more click and you're in.</p>
          <button onClick={handleSignIn} disabled={pending}>
            {pending ? 'Signing you in...' : 'Sign in'}
          </button>
        </div>
      )}
    </div>
  )
}
