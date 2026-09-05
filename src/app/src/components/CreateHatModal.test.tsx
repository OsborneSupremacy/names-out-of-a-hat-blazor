import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { CreateHatModal } from './CreateHatModal'

function renderModal(overrides: Partial<Parameters<typeof CreateHatModal>[0]> = {}) {
  const props = {
    organizerName: 'Alpha',
    organizerEmail: 'alpha@example.com',
    onClose: vi.fn(),
    onSubmit: vi.fn(async () => Promise.resolve()),
    ...overrides,
  }

  render(<CreateHatModal {...props} />)
  return props
}

describe('CreateHatModal', () => {
  // Nothing to ask somebody we already know, so the form is one field for the returning organizer
  // and two for the new one.
  it('asks only for the exchange name when the organizer is known', () => {
    renderModal()

    expect(screen.getByLabelText(/gift exchange name/i)).toBeInTheDocument()
    expect(screen.queryByLabelText(/your name/i)).not.toBeInTheDocument()
  })

  it('asks for a name when the organizer has none yet', () => {
    renderModal({ organizerName: '' })

    expect(screen.getByLabelText(/your name/i)).toBeInTheDocument()
    expect(screen.getByText('alpha@example.com')).toBeInTheDocument()
  })

  it('submits the exchange name and the organizer name', async () => {
    const user = userEvent.setup()
    const props = renderModal()

    await user.type(screen.getByLabelText(/gift exchange name/i), '  Family Christmas 2026  ')
    await user.click(screen.getByRole('button', { name: /create gift exchange/i }))

    expect(props.onSubmit).toHaveBeenCalledWith('Family Christmas 2026', 'Alpha')
    expect(props.onClose).toHaveBeenCalled()
  })

  it('does not submit an empty name', async () => {
    const user = userEvent.setup()
    const props = renderModal()

    await user.click(screen.getByRole('button', { name: /create gift exchange/i }))

    expect(props.onSubmit).not.toHaveBeenCalled()
    expect(screen.getByText(/gift exchange name cannot be empty/i)).toBeInTheDocument()
  })

  it('does not submit without a name for a first-time organizer', async () => {
    const user = userEvent.setup()
    const props = renderModal({ organizerName: '' })

    await user.type(screen.getByLabelText(/gift exchange name/i), 'Family Christmas 2026')
    await user.click(screen.getByRole('button', { name: /create gift exchange/i }))

    expect(props.onSubmit).not.toHaveBeenCalled()
    expect(screen.getByText(/your name cannot be empty/i)).toBeInTheDocument()
  })

  // What the server says is what the organizer reads. The refusals worth getting in front of them
  // say something they can act on -- the daily limit names a time and a way out of it -- and none
  // of that survives being replaced with a generic failure.
  it("shows the server's own refusal without closing", async () => {
    const user = userEvent.setup()
    const props = renderModal({
      onSubmit: vi.fn(async () => {
        throw new Error(
          'You have started 5 gift exchanges in the past day, which is as many as this application allows. '
            + 'You can start another after 14:32 UTC, or sooner if you delete one you no longer need.'
        )
      }),
    })

    await user.type(screen.getByLabelText(/gift exchange name/i), 'Family Christmas 2026')
    await user.click(screen.getByRole('button', { name: /create gift exchange/i }))

    expect(screen.getByText(/as many as this application allows/i)).toBeInTheDocument()
    expect(screen.getByText(/delete one you no longer need/i)).toBeInTheDocument()
    expect(props.onClose).not.toHaveBeenCalled()
  })

  // A rejection that is not an Error carries no message to show, so the modal has to have one.
  it('falls back to its own message when the failure carries none', async () => {
    const user = userEvent.setup()
    renderModal({ onSubmit: vi.fn(async () => Promise.reject('no message')) })

    await user.type(screen.getByLabelText(/gift exchange name/i), 'Family Christmas 2026')
    await user.click(screen.getByRole('button', { name: /create gift exchange/i }))

    expect(screen.getByText(/failed to create gift exchange/i)).toBeInTheDocument()
  })
})
