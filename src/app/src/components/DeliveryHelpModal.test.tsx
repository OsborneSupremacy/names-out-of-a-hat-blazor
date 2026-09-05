import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DeliveryHelpModal } from './DeliveryHelpModal'

describe('DeliveryHelpModal', () => {
  const renderModal = (onClose = () => {}) =>
    render(<DeliveryHelpModal organizerName="Ben" onClose={onClose} />)

  // The whole reason the dialog exists. An organizer opens it holding a table that says
  // "Delivered" and a participant who says nothing arrived, and the first thing it has to do is
  // tell them that both can be true rather than reassure them that one of the two is mistaken.
  it('says what Delivered does and does not claim', () => {
    renderModal()

    expect(screen.getByRole('dialog', { name: 'About these email statuses' })).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'What “Delivered” means' })
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', {
        name: 'So “Delivered” and “I never got an email” can both be true',
      })
    ).toBeInTheDocument()
  })

  // The one thing an organizer can hand somebody that finds the message wherever it was filed.
  // A dialog that explained the problem without giving them this would be an apology.
  it('gives the address to search a mailbox for', () => {
    renderModal()

    expect(screen.getByText(/donotreply@mail\.namesoutofahat\.com/)).toBeInTheDocument()
  })

  // The subject line is the other search term, and it starts with whoever organized the exchange
  // rather than with anything this application would recognise.
  it('names the organizer, since the subject line starts with them', () => {
    renderModal()

    expect(screen.getByText(/Ben has added you to/)).toBeInTheDocument()
  })

  // Correcting the address is the only resend this application has, so the dialog points at it —
  // and has to be honest that sending the same message to the same address again is not offered.
  it('points at Edit Address rather than at a resend button that does not exist', () => {
    renderModal()

    expect(screen.getByText('Edit Address')).toBeInTheDocument()
    expect(
      screen.getByText(/no button for sending the same message to the same address/)
    ).toBeInTheDocument()
  })

  // "No confirmation yet" is the status most easily misread as a failure, and misreading it sends
  // an organizer to pester somebody who is holding their invitation.
  it('explains the empty status as nothing heard rather than as a failure', () => {
    renderModal()

    expect(screen.getByText('No confirmation yet')).toBeInTheDocument()
    expect(screen.getByText(/it never means the message failed/)).toBeInTheDocument()
  })

  it('closes on the button', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()

    renderModal(onClose)
    await user.click(screen.getByRole('button', { name: 'Got it' }))

    expect(onClose).toHaveBeenCalled()
  })

  it('closes on Escape', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()

    renderModal(onClose)
    await user.keyboard('{Escape}')

    expect(onClose).toHaveBeenCalled()
  })
})
