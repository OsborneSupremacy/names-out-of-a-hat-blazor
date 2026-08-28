import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import { AuthCallback } from './AuthCallback'

const redeemMagicLink = vi.fn()

vi.mock('../auth', () => ({
  redeemMagicLink: (token: string) => redeemMagicLink(token)
}))

/**
 * MemoryRouter rather than BrowserRouter so the fragment can be supplied per test without touching
 * window.location. The page reads the hash through useLocation for exactly that reason.
 */
function renderCallback(path: string) {
  const onSignedIn = vi.fn()

  render(
    <MemoryRouter initialEntries={[path]}>
      <AuthCallback onSignedIn={onSignedIn} />
    </MemoryRouter>
  )

  return onSignedIn
}

describe('AuthCallback', () => {
  beforeEach(() => {
    redeemMagicLink.mockReset()
    redeemMagicLink.mockResolvedValue(undefined)
  })

  // The whole point of the page. A mail scanner that renders it gets this far and no further,
  // which is what keeps the token alive for the person the email was addressed to.
  it('does not redeem the token merely because the page loaded', () => {
    renderCallback('/auth#token=opaque-token')

    expect(redeemMagicLink).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument()
  })

  it('redeems the token from the fragment when the button is clicked', async () => {
    const onSignedIn = renderCallback('/auth#token=opaque-token')

    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    await waitFor(() => expect(redeemMagicLink).toHaveBeenCalledWith('opaque-token'))
    expect(onSignedIn).toHaveBeenCalled()
  })

  // The token used to arrive in the query string. A link of that shape is either an old email or
  // something that mangled the fragment, and neither can be signed in.
  it('reports a link that carries no token in its fragment', () => {
    renderCallback('/auth?token=opaque-token')

    expect(screen.getByText('That link is missing its sign-in token.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Sign in' })).not.toBeInTheDocument()
  })

  it('surfaces what went wrong when redemption fails', async () => {
    redeemMagicLink.mockRejectedValue(new Error('That sign-in link has expired.'))

    renderCallback('/auth#token=opaque-token')

    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByText('That sign-in link has expired.')).toBeInTheDocument()
  })
})
