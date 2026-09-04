import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { AdvancedOptionsMenu } from './AdvancedOptionsMenu'

function renderMenu(overrides: Partial<Parameters<typeof AdvancedOptionsMenu>[0]> = {}) {
  const props = {
    canReset: true,
    resetUnavailableReason: 'Invitations have gone out, so this can no longer be reset.',
    isExporting: false,
    onExport: vi.fn(),
    onReset: vi.fn(),
    ...overrides,
  }

  render(<AdvancedOptionsMenu {...props} />)
  return props
}

const openMenu = async (user: ReturnType<typeof userEvent.setup>) =>
  user.click(screen.getByRole('button', { name: 'Advanced options' }))

describe('AdvancedOptionsMenu', () => {
  it('keeps its options out of the way until it is opened', () => {
    renderMenu()

    expect(screen.queryByRole('menuitem', { name: /Export Gift Exchange/ })).not.toBeInTheDocument()
  })

  it('offers both options once opened', async () => {
    const user = userEvent.setup()
    renderMenu()

    await openMenu(user)

    expect(screen.getByRole('menuitem', { name: /Export Gift Exchange/ })).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: /Reset/ })).toBeInTheDocument()
  })

  it('exports and closes', async () => {
    const user = userEvent.setup()
    const { onExport } = renderMenu()

    await openMenu(user)
    await user.click(screen.getByRole('menuitem', { name: /Export Gift Exchange/ }))

    expect(onExport).toHaveBeenCalled()
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('resets and closes', async () => {
    const user = userEvent.setup()
    const { onReset } = renderMenu()

    await openMenu(user)
    await user.click(screen.getByRole('menuitem', { name: /^Reset/ }))

    expect(onReset).toHaveBeenCalled()
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  // Exporting always makes sense; it is a read. Only the reset has a point past which it does not.
  it('still offers the export once the exchange can no longer be reset', async () => {
    const user = userEvent.setup()
    renderMenu({ canReset: false })

    await openMenu(user)

    expect(screen.getByRole('menuitem', { name: /Export Gift Exchange/ })).toBeEnabled()
  })

  /**
   * Disabled rather than removed. An option that vanishes reads as one that was never there, and
   * the organizer looking for it deserves to be told why it has gone.
   */
  it('shows the reset disabled, with the reason, once invitations have gone out', async () => {
    const user = userEvent.setup()
    const { onReset, resetUnavailableReason } = renderMenu({ canReset: false })

    await openMenu(user)

    const reset = screen.getByRole('menuitem', { name: /^Reset/ })

    expect(reset).toBeDisabled()
    expect(screen.getByText(resetUnavailableReason)).toBeInTheDocument()

    await user.click(reset)
    expect(onReset).not.toHaveBeenCalled()
  })

  it('says it is exporting while it is', async () => {
    const user = userEvent.setup()
    renderMenu({ isExporting: true })

    await openMenu(user)

    expect(screen.getByRole('menuitem', { name: /Exporting/ })).toBeDisabled()
  })

  // A menu that only closes by choosing something from it is a menu somebody is stuck in.
  it('closes on Escape without choosing anything', async () => {
    const user = userEvent.setup()
    const { onExport, onReset } = renderMenu()

    await openMenu(user)
    await user.keyboard('{Escape}')

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(onExport).not.toHaveBeenCalled()
    expect(onReset).not.toHaveBeenCalled()
  })

  it('closes when something else is clicked', async () => {
    const user = userEvent.setup()
    renderMenu()

    await openMenu(user)
    await user.click(document.body)

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })
})
