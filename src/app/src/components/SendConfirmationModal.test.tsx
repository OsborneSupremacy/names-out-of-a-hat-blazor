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

    expect(screen.queryByText(/this request comes from/i)).not.toBeInTheDocument()
    expect(screen.getByText('organizer@example.com')).toBeInTheDocument()
  })

  it('locks the acknowledgement and both buttons while sending', () => {
    renderModal({ isSending: true })

    expect(screen.getByRole('checkbox')).toBeDisabled()
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /sending/i })).toBeDisabled()
  })
})
