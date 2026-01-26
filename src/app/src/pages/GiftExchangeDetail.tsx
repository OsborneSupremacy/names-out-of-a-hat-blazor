import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getHat, Hat } from '../api'
import { Header } from '../components/Header'
import { Footer } from '../components/Footer'
import './GiftExchangeDetail.css'

interface GiftExchangeDetailProps {
  userEmail: string
  givenName: string
  onSignOut: () => void
}

export function GiftExchangeDetail({ userEmail, givenName, onSignOut }: GiftExchangeDetailProps) {
  const { hatId } = useParams<{ hatId: string }>()
  const navigate = useNavigate()
  const [hat, setHat] = useState<Hat | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')

  useEffect(() => {
    async function loadHat() {
      if (!hatId) {
        setError('No gift exchange ID provided')
        setLoading(false)
        return
      }

      try {
        const hatData = await getHat(userEmail, hatId, false)
        setHat(hatData)
      } catch (err) {
        console.error('Error loading gift exchange:', err)
        setError('Failed to load gift exchange details')
      } finally {
        setLoading(false)
      }
    }

    loadHat()
  }, [hatId, userEmail])

  return (
    <div className="app-container">
      <Header
        userEmail={userEmail}
        givenName={givenName}
        onSignOut={onSignOut}
      />

      <main className="main-content">
        <div className="content-wrapper">
          <button className="back-button" onClick={() => navigate('/')}>
            ← Back to Gift Exchanges
          </button>

          {loading ? (
            <p>Loading gift exchange...</p>
          ) : error ? (
            <p className="error-message">{error}</p>
          ) : hat ? (
            <div className="hat-detail">
              <div className="hat-header">
                <h2>{hat.name}</h2>
                <div className="hat-status">
                  {hat.organizerVerified && <span className="status-badge verified">Verified</span>}
                  <span className={`status-badge ${hat.recipientsAssigned ? 'assigned' : 'not-assigned'}`}>
                    {hat.recipientsAssigned ? 'Recipients Assigned' : 'Recipients Not Assigned'}
                  </span>
                  <span className={`status-badge ${hat.invitationsQueued ? 'invited' : 'not-invited'}`}>
                    {hat.invitationsQueued ? 'Invitations Sent' : 'Invitations Not Sent'}
                  </span>
                </div>
              </div>

              <div className="hat-info-grid">
                <div className="info-card full-width">
                  <h3>Additional Information</h3>
                  <p>{hat.additionalInformation || <span className="text-muted">None</span>}</p>
                </div>

                <div className="info-card">
                  <h3>Organizer</h3>
                  <p><strong>{hat.organizer.name}</strong></p>
                  <p className="text-muted">{hat.organizer.email}</p>
                </div>

                <div className="info-card">
                  <h3>Price Range</h3>
                  <p>{hat.priceRange || <span className="text-muted">Not set</span>}</p>
                </div>

                <div className="info-card">
                  <h3>Invitations Sent</h3>
                  <p>
                    {hat.invitationsQueued && hat.invitationsQueuedDate &&
                     new Date(hat.invitationsQueuedDate).getFullYear() > 1 ? (
                      new Date(hat.invitationsQueuedDate).toLocaleDateString(undefined, {
                        year: 'numeric',
                        month: 'long',
                        day: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                      })
                    ) : (
                      <span className="text-muted">Not sent yet</span>
                    )}
                  </p>
                </div>
              </div>

              <div className="participants-section">
                <h3>Participants ({hat.participants.length})</h3>
                {hat.participants.length > 0 ? (
                  <ul className="participants-list">
                    {hat.participants.map((participant, index) => (
                      <li key={index} className="participant-item">
                        <div className="participant-info">
                          <strong>{participant.person.name}</strong>
                          <span className="participant-email">{participant.person.email}</span>
                        </div>
                        {participant.pickedRecipient && (
                          <div className="picked-recipient">
                            → {participant.pickedRecipient}
                          </div>
                        )}
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className="text-muted">No participants yet</p>
                )}
              </div>
            </div>
          ) : (
            <p className="error-message">Gift exchange not found</p>
          )}
        </div>
      </main>

      <Footer />
    </div>
  )
}
