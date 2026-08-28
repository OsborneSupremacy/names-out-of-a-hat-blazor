import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import { Header } from './Header'

/**
 * Wrapped in a router because the wordmark is a Link, which throws outside one. MemoryRouter rather
 * than BrowserRouter so the tests do not touch window.history.
 */
function renderHeader(givenName: string | null, initialPath = '/') {
  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Header
        userEmail="osborne.ben@gmail.com"
        givenName={givenName}
        onSignOut={vi.fn()}
        onNameUpdated={vi.fn()}
      />
    </MemoryRouter>
  )
}

describe('Header', () => {
  // Falling back to the email initial while the name loads is not a placeholder, it is a second
  // answer — which is what made the avatar flick from "O" to "B" on every page load.
  it('shows no initial while the name is still unknown', () => {
    renderHeader(null)

    expect(screen.getByLabelText('Profile menu')).toHaveTextContent(/^\s*$/)
  })

  it('shows the name initial once the name is known', () => {
    renderHeader('Ben')

    expect(screen.getByLabelText('Profile menu')).toHaveTextContent('B')
  })

  it('falls back to the email initial for a user who genuinely has no name', () => {
    renderHeader('')

    expect(screen.getByLabelText('Profile menu')).toHaveTextContent('O')
  })

  it('points the wordmark at the home page', () => {
    renderHeader('Ben')

    expect(screen.getByRole('link', { name: 'Names Out of a Hat' })).toHaveAttribute('href', '/')
  })

  // Deliberate: a masthead that is a link on every page except one is a masthead that moves out
  // from under the keyboard.
  it('leaves the wordmark a link on the home page itself', () => {
    renderHeader('Ben', '/')

    expect(screen.getByRole('link', { name: 'Names Out of a Hat' })).toBeInTheDocument()
  })
})
