import { render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import { Header } from './Header'

function renderHeader(givenName: string | null) {
  render(
    <Header
      userEmail="osborne.ben@gmail.com"
      givenName={givenName}
      onSignOut={vi.fn()}
      onNameUpdated={vi.fn()}
    />
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
})
