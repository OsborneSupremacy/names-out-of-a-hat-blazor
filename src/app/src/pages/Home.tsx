import { useNavigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import { getHats, createHat, HatMetadata } from '../api'
import { Header } from '../components/Header'
import { Footer } from '../components/Footer'
import { CreateHatModal } from '../components/CreateHatModal'

interface HomeProps {
  userEmail: string
  givenName: string
  onSignOut: () => void
}

export function Home({ userEmail, givenName, onSignOut }: HomeProps) {
  const navigate = useNavigate()
  const [hats, setHats] = useState<HatMetadata[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [showCreateModal, setShowCreateModal] = useState(false)

  useEffect(() => {
    async function loadHats() {
      try {
        const response = await getHats(userEmail)
        setHats(response.hats)
      } catch (err) {
        console.error('Error loading gift exchanges:', err)
        setError('Failed to load your gift exchanges')
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

  const handleCreateSubmit = async (hatName: string) => {
    await createHat({
      hatName,
      organizerName: givenName,
      organizerEmail: userEmail,
    })

    // Refresh the hats list
    const updatedHats = await getHats(userEmail)
    setHats(updatedHats.hats)
  }

  const handleHatClick = (hatId: string) => {
    navigate(`/gift-exchange/${hatId}`)
  }

  return (
    <div className="app-container">
      <Header
        userEmail={userEmail}
        givenName={givenName}
        onSignOut={onSignOut}
      />

      <main className="main-content">
        <div className="content-wrapper">
          <h2>Hello {givenName || 'there'}!</h2>
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
                        <span className={`status-pill ${hat.invitationsQueued ? 'sent' : 'incomplete'}`}>
                          {hat.invitationsQueued ? 'Invites Sent' : 'Incomplete'}
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
          organizerName={givenName}
          organizerEmail={userEmail}
          onClose={() => setShowCreateModal(false)}
          onSubmit={handleCreateSubmit}
        />
      )}
    </div>
  )
}
