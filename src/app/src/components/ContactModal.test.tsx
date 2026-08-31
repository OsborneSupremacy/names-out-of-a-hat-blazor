import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { ContactModal } from './ContactModal'
import * as api from '../api'

/**
 * The form has one job: a message the sender believes was sent is a message that was published.
 * Every test here is a way that stops being true without anything visibly breaking.
 */
describe('ContactModal', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('sends the selected category with the message', async () => {
    const submit = vi.spyOn(api, 'submitFeedback').mockResolvedValue()
    const user = userEvent.setup()

    render(<ContactModal onClose={vi.fn()} />)

    await user.selectOptions(screen.getByLabelText('What’s this about?'), 'FEATURE_REQUEST')
    await user.type(screen.getByLabelText('What would you like it to do?'), 'Let me reopen a closed hat.')
    await user.click(screen.getByRole('button', { name: 'Send' }))

    await waitFor(() =>
      expect(submit).toHaveBeenCalledWith({
        category: 'FEATURE_REQUEST',
        message: 'Let me reopen a closed hat.',
      })
    )
  })

  // Whitespace is trimmed before the length check as well as before the send, so a box holding
  // only spaces must not look sendable.
  it('keeps Send disabled until something has actually been written', async () => {
    const user = userEvent.setup()

    render(<ContactModal onClose={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Send' })).toBeDisabled()

    await user.type(screen.getByLabelText('What would you like to know?'), '   ')

    expect(screen.getByRole('button', { name: 'Send' })).toBeDisabled()
  })

  // The failure this guards is the worst one available: the sender is thanked, closes the dialog,
  // and never learns the message went nowhere.
  it('reports a failed send rather than thanking the sender', async () => {
    vi.spyOn(api, 'submitFeedback').mockRejectedValue(new Error('We could not send that just now.'))
    const user = userEvent.setup()

    render(<ContactModal onClose={vi.fn()} />)

    await user.type(screen.getByLabelText('What would you like to know?'), 'Anyone there?')
    await user.click(screen.getByRole('button', { name: 'Send' }))

    expect(await screen.findByText('We could not send that just now.')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Thanks' })).not.toBeInTheDocument()
  })

  it('confirms once the message has gone', async () => {
    vi.spyOn(api, 'submitFeedback').mockResolvedValue()
    const user = userEvent.setup()

    render(<ContactModal onClose={vi.fn()} />)

    await user.type(screen.getByLabelText('What would you like to know?'), 'How do I copy a hat?')
    await user.click(screen.getByRole('button', { name: 'Send' }))

    expect(await screen.findByRole('heading', { name: 'Thanks' })).toBeInTheDocument()
  })

  it('offers exactly the three categories the server accepts', () => {
    render(<ContactModal onClose={vi.fn()} />)

    const values = screen
      .getAllByRole('option')
      .map((option) => (option as HTMLOptionElement).value)

    expect(values).toEqual(['QUESTION', 'FEATURE_REQUEST', 'OTHER_FEEDBACK'])
  })
})
