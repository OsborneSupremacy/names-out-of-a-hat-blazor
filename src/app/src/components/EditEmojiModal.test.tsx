import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EditEmojiModal } from './EditEmojiModal'
import { PERSON_EMOJI } from '../personEmoji'

describe('EditEmojiModal', () => {
  const renderModal = (overrides: Partial<Parameters<typeof EditEmojiModal>[0]> = {}) => {
    const props = {
      participantName: 'Alice',
      currentEmoji: '😀',
      isSaving: false,
      error: '',
      onCancel: vi.fn(),
      onConfirm: vi.fn().mockResolvedValue(undefined),
      ...overrides,
    }

    render(<EditEmojiModal {...props} />)

    return props
  }

  it('offers every face the application has', () => {
    renderModal()

    for (const emoji of PERSON_EMOJI) {
      expect(screen.getByRole('radio', { name: `Mark Alice with ${emoji}` })).toBeInTheDocument()
    }
  })

  // Opening on what they already wear is what makes this an edit rather than a fresh choice: an
  // organizer who opens it to look should be able to close it without having lost anything.
  it('opens with the face they already wear selected', () => {
    renderModal({ currentEmoji: '🤖' })

    expect(screen.getByRole('radio', { name: 'Mark Alice with 🤖' })).toBeChecked()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('saves the face that was picked', async () => {
    const user = userEvent.setup()
    const { onConfirm } = renderModal({ currentEmoji: '😀' })

    await user.click(screen.getByRole('radio', { name: 'Mark Alice with 🥳' }))
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(onConfirm).toHaveBeenCalledWith('🥳')
  })

  // Selecting is not saving. The grid moves the highlight; nothing leaves the browser until Save,
  // which is what lets somebody try a few before settling.
  it('does not save on selection alone', async () => {
    const user = userEvent.setup()
    const { onConfirm } = renderModal()

    await user.click(screen.getByRole('radio', { name: 'Mark Alice with 🥳' }))

    expect(onConfirm).not.toHaveBeenCalled()
    expect(screen.getByRole('radio', { name: 'Mark Alice with 🥳' })).toBeChecked()
  })

  it('shows what went wrong and stays open', () => {
    renderModal({ error: 'Failed to update the emoji' })

    expect(screen.getByText('Failed to update the emoji')).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'Choose Alice’s emoji' })).toBeInTheDocument()
  })

  it('closes on Escape', async () => {
    const user = userEvent.setup()
    const { onCancel } = renderModal()

    await user.keyboard('{Escape}')

    expect(onCancel).toHaveBeenCalled()
  })

  // While a save is in flight the choice is fixed: a second click that changed the selection would
  // leave the dialog showing something other than what is being written.
  it('locks the grid while saving', () => {
    renderModal({ isSaving: true })

    expect(screen.getByRole('radio', { name: 'Mark Alice with 🥳' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
  })
})
