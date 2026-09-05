import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { DeleteHatModal } from './DeleteHatModal'

function renderModal(overrides: Partial<Parameters<typeof DeleteHatModal>[0]> = {}) {
  const props = {
    hatName: 'Family Christmas 2026',
    participantCount: 5,
    hasBeenShaken: false,
    onClose: vi.fn(),
    onSubmit: vi.fn(async () => Promise.resolve()),
    ...overrides,
  }

  render(<DeleteHatModal {...props} />)
  return props
}

const submit = (user: ReturnType<typeof userEvent.setup>) =>
  user.click(screen.getByRole('button', { name: 'Delete Gift Exchange' }))

describe('DeleteHatModal', () => {
  /**
   * The mirror of what the reset dialog says. There the people survive and the dialog leads with
   * that; here they do not, and saying how many are about to go is the whole point of asking.
   */
  it('says the exchange and everybody in it will be gone', () => {
    renderModal()

    expect(screen.getByText(/throws/)).toHaveTextContent('Family Christmas 2026')
    expect(screen.getByText(/All 5 people/)).toBeInTheDocument()
  })

  it('counts one person as a person rather than as 1 people', () => {
    renderModal({ participantCount: 1 })

    expect(screen.getByText(/The one person/)).toBeInTheDocument()
  })

  // The export is directly above this in the menu they just came from, and it is the only way to
  // keep any of this. Somebody who has to think of that themselves will think of it too late.
  it('points at the export as the way to keep a copy', () => {
    renderModal()

    expect(screen.getByText(/export the gift exchange first/)).toBeInTheDocument()
  })

  // Only worth saying when there is a draw to lose.
  it('mentions the drawn names only once the hat has been shaken', () => {
    const { unmount } = render(
      <DeleteHatModal
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

  it('deletes and closes', async () => {
    const user = userEvent.setup()
    const { onSubmit, onClose } = renderModal()

    await submit(user)

    expect(onSubmit).toHaveBeenCalled()
    expect(onClose).toHaveBeenCalled()
  })

  /**
   * Beside the button that was pressed, and with the dialog still open. The page behind is only
   * left behind when the delete works, so a refusal put there would be a message nobody reads.
   */
  it('keeps a refusal in the dialog', async () => {
    const user = userEvent.setup()
    const { onClose } = renderModal({
      onSubmit: vi.fn(async () => {
        throw new Error('This gift exchange moved on while it was being deleted')
      }),
    })

    await submit(user)

    expect(screen.getByText(/moved on while it was being deleted/)).toBeInTheDocument()
    expect(onClose).not.toHaveBeenCalled()
  })

  it('closes without deleting when cancelled', async () => {
    const user = userEvent.setup()
    const { onSubmit, onClose } = renderModal()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(onClose).toHaveBeenCalled()
  })
})
