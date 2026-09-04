import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { ShakeHatModal } from './ShakeHatModal'

function renderModal(overrides: Partial<Parameters<typeof ShakeHatModal>[0]> = {}) {
  const props = {
    isReshake: false,
    participantCount: 10,
    onClose: vi.fn(),
    onSubmit: vi.fn(async () => Promise.resolve()),
    ...overrides,
  }

  render(<ShakeHatModal {...props} />)
  return props
}

describe('ShakeHatModal', () => {
  it('opens on the least constrained draw type', () => {
    renderModal()

    expect(screen.getByRole('radio', { name: /Anything goes/ })).toBeChecked()
  })

  it('shakes with the draw type that was chosen', async () => {
    const user = userEvent.setup()
    const { onSubmit } = renderModal()

    await user.click(screen.getByRole('radio', { name: /Single cycle/ }))
    await user.click(screen.getByRole('button', { name: 'Shake the Hat!' }))

    expect(onSubmit).toHaveBeenCalledWith('SINGLE_CYCLE')
  })

  // The reassurance the dialog exists to give. An organizer who has spent time setting up who may
  // draw whom needs to see that a new setting has not replaced that work.
  it('says the exclusions apply whatever is chosen', () => {
    renderModal()

    expect(
      screen.getByText(/Whoever each person is allowed to draw still applies/)
    ).toBeInTheDocument()
  })

  it('warns about satisfiability only once a constrained draw type is chosen', async () => {
    const user = userEvent.setup()
    renderModal()

    expect(screen.queryByText(/may not be possible at all/)).not.toBeInTheDocument()

    await user.click(screen.getByRole('radio', { name: /No mutual pairs/ }))
    expect(screen.getByText(/may not be possible at all/)).toBeInTheDocument()

    await user.click(screen.getByRole('radio', { name: /Anything goes/ }))
    expect(screen.queryByText(/may not be possible at all/)).not.toBeInTheDocument()
  })

  describe('the technical popover', () => {
    it('stays shut until it is asked for', () => {
      renderModal()

      expect(screen.queryByText(/Hamiltonian cycle/)).not.toBeInTheDocument()
    })

    it('opens on the option it belongs to without selecting it', async () => {
      const user = userEvent.setup()
      renderModal()

      await user.click(screen.getByRole('button', { name: /What "Single cycle" means/ }))

      expect(screen.getByText(/Hamiltonian cycle/)).toBeInTheDocument()
      // The trigger sits outside the label on purpose: asking what an option means is not the same
      // as choosing it.
      expect(screen.getByRole('radio', { name: /Single cycle/ })).not.toBeChecked()
      expect(screen.getByRole('radio', { name: /Anything goes/ })).toBeChecked()
    })

    it('shows one at a time', async () => {
      const user = userEvent.setup()
      renderModal()

      await user.click(screen.getByRole('button', { name: /What "Single cycle" means/ }))
      await user.click(screen.getByRole('button', { name: /What "No mutual pairs" means/ }))

      expect(screen.getByText(/no 2-cycle/)).toBeInTheDocument()
      expect(screen.queryByText(/Hamiltonian cycle/)).not.toBeInTheDocument()
    })

    it('closes on Escape', async () => {
      const user = userEvent.setup()
      renderModal()

      await user.click(screen.getByRole('button', { name: /What "Single cycle" means/ }))
      await user.keyboard('{Escape}')

      expect(screen.queryByText(/Hamiltonian cycle/)).not.toBeInTheDocument()
    })
  })

  // Names are already out, so this dialog is also where the previous draw is thrown away. It used
  // to be a browser confirm() before the options existed.
  it('says what a re-shake costs, and only when it is one', () => {
    const { onClose } = renderModal({ isReshake: true })

    expect(screen.getByRole('heading', { name: 'Shake the Hat Again' })).toBeInTheDocument()
    expect(screen.getByText(/throws that draw away/)).toBeInTheDocument()
    expect(onClose).not.toHaveBeenCalled()
  })

  it('leaves a first shake without the re-shake warning', () => {
    renderModal()

    expect(screen.queryByText(/throws that draw away/)).not.toBeInTheDocument()
  })

  // A draw the server could not satisfy is the failure the organizer can act on immediately, by
  // loosening the rule they just picked. It has to land beside the options, not behind the dialog.
  it('keeps a failed shake in the dialog and shows why', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn(async () => {
      throw new Error('We could not find a valid distribution')
    })
    const { onClose } = renderModal({ onSubmit })

    await user.click(screen.getByRole('radio', { name: /Single cycle/ }))
    await user.click(screen.getByRole('button', { name: 'Shake the Hat!' }))

    expect(screen.getByText('We could not find a valid distribution')).toBeInTheDocument()
    expect(onClose).not.toHaveBeenCalled()
  })

  it('closes once the names are out', async () => {
    const user = userEvent.setup()
    const { onClose } = renderModal()

    await user.click(screen.getByRole('button', { name: 'Shake the Hat!' }))

    expect(onClose).toHaveBeenCalled()
  })
})
