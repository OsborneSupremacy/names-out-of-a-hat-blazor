import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { SendConfirmationModal } from './SendConfirmationModal'

function renderModal(overrides: Partial<Parameters<typeof SendConfirmationModal>[0]> = {}) {
  const props = {
    organizerEmail: 'organizer@example.com',
    senderIpAddress: '203.0.113.7',
    recipientCount: 3,
    isSending: false,
    onCancel: vi.fn(),
    onConfirm: vi.fn(async () => Promise.resolve()),
    ...overrides,
  }

  render(<SendConfirmationModal {...props} />)
  return props
}

/**
 * The terms as read, one string per bullet. Each bullet mixes plain text with <strong>, so its
 * sentence only exists once the children are joined — queryByText matches a single node and would
 * miss a phrase that spans them.
 */
function terms() {
  return screen.getAllByRole('listitem').map((item) => item.textContent ?? '')
}

describe('SendConfirmationModal', () => {
  it('shows the organizer address and the originating IP', () => {
    renderModal()

    expect(screen.getByText('organizer@example.com')).toBeInTheDocument()
    expect(screen.getByText('203.0.113.7')).toBeInTheDocument()
  })

  // The whole point of the dialog: it should not be dismissable by reflex on the way to a button.
  it('keeps send disabled until the acknowledgement is ticked', async () => {
    const user = userEvent.setup()
    renderModal()

    const send = screen.getByRole('button', { name: /send 3 invitations/i })
    expect(send).toBeDisabled()

    await user.click(screen.getByRole('checkbox'))

    expect(send).toBeEnabled()
  })

  it('does not send when the acknowledgement is unticked', async () => {
    const user = userEvent.setup()
    const props = renderModal()

    await user.click(screen.getByRole('button', { name: /send 3 invitations/i }))

    expect(props.onConfirm).not.toHaveBeenCalled()
  })

  it('sends once acknowledged', async () => {
    const user = userEvent.setup()
    const props = renderModal()

    await user.click(screen.getByRole('checkbox'))
    await user.click(screen.getByRole('button', { name: /send 3 invitations/i }))

    expect(props.onConfirm).toHaveBeenCalledOnce()
  })

  it('singularises the count for a lone recipient', () => {
    renderModal({ recipientCount: 1 })

    expect(screen.getByText(/will go to 1 person\./i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /send 1 invitation$/i })).toBeInTheDocument()
  })

  it('omits the IP clause when no address was resolved', () => {
    renderModal({ senderIpAddress: '' })

    expect(terms().some((term) => /, from/i.test(term))).toBe(false)
    expect(screen.getByText('organizer@example.com')).toBeInTheDocument()
  })

  /*
   * The clause names one thing or two depending on whether an IP was resolved, and the sentence
   * that closes it has to agree. It used to say "both are recorded" either way.
   */
  it('counts only what it names when no IP was resolved', () => {
    renderModal({ senderIpAddress: '' })

    expect(terms().some((term) => /both are recorded/i.test(term))).toBe(false)
    expect(terms().some((term) => /it is recorded/i.test(term))).toBe(true)
  })

  it('counts both the address and the IP when one was resolved', () => {
    renderModal()

    expect(terms().some((term) => /both are recorded/i.test(term))).toBe(true)
  })

  it('locks the acknowledgement and both buttons while sending', () => {
    renderModal({ isSending: true })

    expect(screen.getByRole('checkbox')).toBeDisabled()
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /sending/i })).toBeDisabled()
  })
})
