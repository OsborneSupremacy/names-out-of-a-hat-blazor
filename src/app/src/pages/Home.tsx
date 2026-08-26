import { useNavigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import { getHats, createHat, HatMetadata } from '../api'
import { Header } from '../components/Header'
import { Footer } from '../components/Footer'
import { CreateHatModal } from '../components/CreateHatModal'

interface HomeProps {
  userEmail: string
  onSignOut: () => void
}

export function Home({ userEmail, onSignOut }: HomeProps) {
  const navigate = useNavigate()
  const [hats, setHats] = useState<HatMetadata[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [organizerName, setOrganizerName] = useState('')

  const formatStatus = (status: string): string => {
    // Convert "IN_PROGRESS" to "In Progress"
    return status
      .split('_')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ')
  }

  useEffect(() => {
    async function loadHats() {
      try {
        const response = await getHats(userEmail)
        setHats(response.hats)
        setOrganizerName(response.organizerName)
      } catch (err) {
        console.error('Error loading gift exchanges:', err)
        setError(err instanceof Error ? err.message : 'Failed to load your gift exchanges')
      } finally {
        setLoading(false)
      }
    }

    if (userEmail) {
      loadHats()
    }
  }, [userEmail])

  const handleCreateNew = () => {
    setShowCreateModal(true)
  }

  const handleCreateSubmit = async (hatName: string, name: string) => {
    await createHat({
      hatName,
      organizerName: name,
      organizerEmail: userEmail,
    })

    // Refresh the hats list
    const updatedHats = await getHats(userEmail)
    setHats(updatedHats.hats)
    setOrganizerName(updatedHats.organizerName)
  }

  const handleHatClick = (hatId: string) => {
    navigate(`/gift-exchange/${hatId}`)
  }

  return (
    <div className="app-container">
      <Header
        userEmail={userEmail}
        givenName={organizerName}
        onSignOut={onSignOut}
        onNameUpdated={setOrganizerName}
      />

      <main className="main-content">
        <div className="content-wrapper">
          <h2>Hello {organizerName || 'there'}!</h2>
          <p>Welcome to Names Out of a Hat!</p>

          {loading ? (
            <p>Loading your gift exchanges...</p>
          ) : error ? (
            <p className="error-message">{error}</p>
          ) : (
            <>
              {hats.length > 0 ? (
                <div className="gift-exchanges-section">
                  <div className="section-header">
                    <h3>Your Gift Exchanges</h3>
                    <button className="primary-button" onClick={handleCreateNew}>
                      Create New Gift Exchange
                    </button>
                  </div>
                  <ul className="gift-exchanges-list">
                    {hats.map((hat) => (
                      <li
                        key={hat.hatId}
                        className="gift-exchange-item"
                        onClick={() => handleHatClick(hat.hatId)}
                      >
                        <div className="gift-exchange-info">
                          <strong>{hat.hatName}</strong>
                        </div>
                        <span className={`status-pill ${hat.status.toLowerCase().replace(/_/g, '-')}`}>
                          {formatStatus(hat.status)}
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : (
                <div className="empty-state">
                  <p>You don't have any Gift Exchanges</p>
                  <button className="primary-button" onClick={handleCreateNew}>
                    Create a Gift Exchange
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </main>

      <Footer />

      {showCreateModal && (
        <CreateHatModal
          organizerName={organizerName}
          organizerEmail={userEmail}
          onClose={() => setShowCreateModal(false)}
          onSubmit={handleCreateSubmit}
        />
      )}
    </div>
  )
}
