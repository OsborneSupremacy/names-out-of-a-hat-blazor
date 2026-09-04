import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { ResetHatModal } from './ResetHatModal'

function renderModal(overrides: Partial<Parameters<typeof ResetHatModal>[0]> = {}) {
  const props = {
    hatName: 'Family Christmas 2026',
    participantCount: 5,
    hasBeenShaken: false,
    onClose: vi.fn(),
    onSubmit: vi.fn(async () => Promise.resolve()),
    ...overrides,
  }

  render(<ResetHatModal {...props} />)
  return props
}

const submit = (user: ReturnType<typeof userEvent.setup>) =>
  user.click(screen.getByRole('button', { name: 'Reset Gift Exchange' }))

describe('ResetHatModal', () => {
  /**
   * The part somebody about to press this most needs to hear. "Reset" on its own sounds like it
   * might mean "Delete", and the people in the exchange are what took work to type in.
   */
  it('says the exchange and everybody in it survive', () => {
    renderModal()

    expect(screen.getByText(/Nothing is deleted/)).toBeInTheDocument()
    expect(screen.getByText(/5 people/)).toBeInTheDocument()
  })

  it('says what will be lost', () => {
    renderModal()

    expect(screen.getByText(/allowed to draw everybody else again/)).toBeInTheDocument()
    expect(screen.getByText(/rules you set about who can draw whom will be gone/)).toBeInTheDocument()
  })

  // Only worth saying when there is a draw to throw away.
  it('mentions the drawn names only once the hat has been shaken', () => {
    const { unmount } = render(
      <ResetHatModal
        hatName="Family Christmas 2026"
        participantCount={5}
        hasBeenShaken={false}
        onClose={vi.fn()}
        onSubmit={vi.fn(async () => Promise.resolve())}
      />
    )

    expect(screen.queryByText(/names that have been drawn/)).not.toBeInTheDocument()
    unmount()

    renderModal({ hasBeenShaken: true })
    expect(screen.getByText(/names that have been drawn/)).toBeInTheDocument()
  })

  it('resets and closes', async () => {
    const user = userEvent.setup()
    const { onSubmit, onClose } = renderModal()

    await submit(user)

    expect(onSubmit).toHaveBeenCalled()
    expect(onClose).toHaveBeenCalled()
  })

  /**
   * Beside the button that was pressed, and with the dialog still open. A reset refused because
   * invitations went out in another tab is exactly the message an organizer has to read where they
   * are already looking.
   */
  it('keeps a refusal in the dialog', async () => {
    const user = userEvent.setup()
    const { onClose } = renderModal({
      onSubmit: vi.fn(async () => {
        throw new Error('This gift exchange moved on while it was being reset')
      }),
    })

    await submit(user)

    expect(screen.getByText(/moved on while it was being reset/)).toBeInTheDocument()
    expect(onClose).not.toHaveBeenCalled()
  })

  it('closes without resetting when cancelled', async () => {
    const user = userEvent.setup()
    const { onSubmit, onClose } = renderModal()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(onClose).toHaveBeenCalled()
  })
})
