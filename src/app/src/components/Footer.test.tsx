import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Footer } from './Footer'

describe('Footer', () => {
  it('opens the terms in a dialog', async () => {
    const user = userEvent.setup()

    render(<Footer />)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Terms & Conditions' }))

    expect(screen.getByRole('dialog', { name: 'Terms & Conditions' })).toBeInTheDocument()
  })

  it('opens the contact form in a dialog', async () => {
    const user = userEvent.setup()

    render(<Footer />)

    await user.click(screen.getByRole('button', { name: 'Contact' }))

    expect(screen.getByRole('dialog', { name: 'Get in touch' })).toBeInTheDocument()
  })

  // Buttons, not links: neither opens a URL, and an anchor here would navigate for anybody who
  // middle-clicks it or reads the page with a screen reader.
  it('exposes both as buttons rather than links', () => {
    render(<Footer />)

    expect(screen.queryByRole('link', { name: 'Terms & Conditions' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Contact' })).not.toBeInTheDocument()
  })
})
