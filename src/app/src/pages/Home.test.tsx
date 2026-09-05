import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import { Home } from './Home'
import { HatMetadata } from '../api'

const getHats = vi.fn()

vi.mock('../api', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api')>()),
  getHats: (email: string) => getHats(email),
  createHat: vi.fn()
}))

const hoursAgo = (hours: number) => new Date(Date.now() - hours * 60 * 60 * 1000).toISOString()

function renderHome(hats: HatMetadata[]) {
  getHats.mockResolvedValue({ organizerName: 'Ben', hats })

  render(
    <MemoryRouter>
      <Home userEmail="organizer@example.com" onSignOut={vi.fn()} />
    </MemoryRouter>
  )
}

describe('Home', () => {
  beforeEach(() => {
    getHats.mockReset()
  })

  it('says how long each exchange has been at its status', async () => {
    renderHome([
      {
        hatId: '11111111-1111-1111-1111-111111111111',
        hatName: 'Family Christmas',
        status: 'INVITATIONS_SENT',
        statusUpdatedAt: hoursAgo(3)
      }
    ])

    expect(await screen.findByText('Family Christmas')).toBeInTheDocument()
    expect(screen.getByText('Invitations Sent')).toBeInTheDocument()
    expect(screen.getByText('3 hours ago')).toBeInTheDocument()
  })

  // The age is when the status last changed, so two exchanges at the same status can carry
  // different ones -- which is the whole reason it is shown next to the pill rather than inferred
  // from it.
  it('ages each exchange independently of its status', async () => {
    renderHome([
      {
        hatId: '11111111-1111-1111-1111-111111111111',
        hatName: 'Family Christmas',
        status: 'IN_PROGRESS',
        statusUpdatedAt: hoursAgo(2)
      },
      {
        hatId: '22222222-2222-2222-2222-222222222222',
        hatName: 'Office Draw',
        status: 'IN_PROGRESS',
        statusUpdatedAt: hoursAgo(30 * 24)
      }
    ])

    expect(await screen.findByText('2 hours ago')).toBeInTheDocument()
    expect(screen.getByText('1 month ago')).toBeInTheDocument()
  })

  // The API spells "not known" with the minimum date rather than with null. Nothing should reach
  // the list carrying it, but a row written outside the application could, and "2025 years ago" is
  // not the thing to show an organizer.
  it('shows no age for an exchange whose status has no timestamp', async () => {
    renderHome([
      {
        hatId: '11111111-1111-1111-1111-111111111111',
        hatName: 'Family Christmas',
        status: 'IN_PROGRESS',
        statusUpdatedAt: '0001-01-01T00:00:00+00:00'
      }
    ])

    await waitFor(() => expect(screen.getByText('Family Christmas')).toBeInTheDocument())

    expect(screen.getByText('In Progress')).toBeInTheDocument()
    expect(screen.queryByText(/ago$/)).not.toBeInTheDocument()
  })
})
