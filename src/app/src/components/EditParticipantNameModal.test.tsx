import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EditParticipantNameModal } from './EditParticipantNameModal'

describe('EditParticipantNameModal', () => {
  const renderModal = (
    overrides: Partial<Parameters<typeof EditParticipantNameModal>[0]> = {}
  ) => {
    const props = {
      currentName: 'Alice',
      currentEmail: 'alice@example.com',
      isSaving: false,
      error: '',
      onCancel: vi.fn(),
      onConfirm: vi.fn().mockResolvedValue(undefined),
      ...overrides,
    }

    render(<EditParticipantNameModal {...props} />)

    return props
  }

  // Opening on the name they already have is what makes this an edit rather than a blank box: an
  // organizer fixing one letter should not have to retype the rest.
  it('opens on the name they already have, with nothing to save', () => {
    renderModal()

    expect(screen.getByLabelText('Name')).toHaveValue('Alice')
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('saves the new name', async () => {
    const user = userEvent.setup()
    const { onConfirm } = renderModal()

    await user.clear(screen.getByLabelText('Name'))
    await user.type(screen.getByLabelText('Name'), 'Alicia')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(onConfirm).toHaveBeenCalledWith('Alicia')
  })

  it('trims what was typed before saving it', async () => {
    const user = userEvent.setup()
    const { onConfirm } = renderModal()

    await user.clear(screen.getByLabelText('Name'))
    await user.type(screen.getByLabelText('Name'), '  Alicia  ')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(onConfirm).toHaveBeenCalledWith('Alicia')
  })

  it('will not save an empty name', async () => {
    const user = userEvent.setup()
    renderModal()

    await user.clear(screen.getByLabelText('Name'))

    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  /**
   * The reach is the thing an organizer is least likely to expect, and the dialog is the last
   * moment they can decide against it.
   */
  it('says the change is felt outside this gift exchange', () => {
    renderModal()

    expect(
      screen.getByText(/every gift exchange they take part in/i)
    ).toBeInTheDocument()
  })

  /**
   * The refusals this endpoint returns are explanations rather than glitches — somebody already
   * goes by that name, or the name is not this organizer's to change — so the dialog has to be
   * able to show one while the box that caused it is still on screen.
   */
  it('shows a refusal from the server without closing', () => {
    renderModal({
      error:
        'Alice was added to a gift exchange by somebody else, and a name belongs to the person rather than to one exchange — so this one is not yours to change.',
    })

    expect(screen.getByText(/not yours to change/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Name')).toBeInTheDocument()
  })

  it('locks the dialog while a save is in flight', () => {
    renderModal({ isSaving: true })

    expect(screen.getByLabelText('Name')).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('closes without saving when cancelled', async () => {
    const user = userEvent.setup()
    const { onCancel, onConfirm } = renderModal()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
    expect(onConfirm).not.toHaveBeenCalled()
  })
})
