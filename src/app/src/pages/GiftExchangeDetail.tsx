import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getHat, editHat, addParticipant, removeParticipant, editParticipant, Hat } from '../api'
import { Header } from '../components/Header'
import { Footer } from '../components/Footer'
import { AddParticipantModal } from '../components/AddParticipantModal'
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
  const [isEditing, setIsEditing] = useState(false)
  const [editedName, setEditedName] = useState('')
  const [editedAdditionalInfo, setEditedAdditionalInfo] = useState('')
  const [editedPriceRange, setEditedPriceRange] = useState('')
  const [saving, setSaving] = useState(false)
  const [showAddParticipantModal, setShowAddParticipantModal] = useState(false)
  const [removingParticipant, setRemovingParticipant] = useState<string | null>(null)
  const [expandedParticipant, setExpandedParticipant] = useState<string | null>(null)
  const [editingEligibleFor, setEditingEligibleFor] = useState<string | null>(null)
  const [tempEligibleRecipients, setTempEligibleRecipients] = useState<string[]>([])

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
        setEditedName(hatData.name)
        setEditedAdditionalInfo(hatData.additionalInformation)
        setEditedPriceRange(hatData.priceRange)
      } catch (err) {
        console.error('Error loading gift exchange:', err)
        setError('Failed to load gift exchange details')
      } finally {
        setLoading(false)
      }
    }

    loadHat()
  }, [hatId, userEmail])

  const handleEdit = () => {
    setIsEditing(true)
  }

  const handleCancel = () => {
    if (hat) {
      setEditedName(hat.name)
      setEditedAdditionalInfo(hat.additionalInformation)
      setEditedPriceRange(hat.priceRange)
    }
    setIsEditing(false)
  }

  const handleSave = async () => {
    if (!hat || !hatId) return

    setSaving(true)
    try {
      await editHat({
        hatId,
        organizerEmail: userEmail,
        name: editedName,
        additionalInformation: editedAdditionalInfo,
        priceRange: editedPriceRange,
      })

      // Reload the hat data
      const updatedHat = await getHat(userEmail, hatId, false)
      setHat(updatedHat)
      setIsEditing(false)
    } catch (err) {
      console.error('Error saving changes:', err)
      setError('Failed to save changes')
    } finally {
      setSaving(false)
    }
  }

  const handleAddParticipant = async (name: string, email: string) => {
    if (!hatId) return

    await addParticipant({
      organizerEmail: userEmail,
      hatId,
      name,
      email,
    })

    // Reload the hat data
    const updatedHat = await getHat(userEmail, hatId, false)
    setHat(updatedHat)
  }

  const handleRemoveParticipant = async (participantEmail: string) => {
    if (!hatId) return

    const confirmed = window.confirm(
      `Are you sure you want to remove this participant?\n\nEmail: ${participantEmail}`
    )

    if (!confirmed) return

    setRemovingParticipant(participantEmail)
    try {
      await removeParticipant({
        organizerEmail: userEmail,
        hatId,
        email: participantEmail,
      })

      // Reload the hat data
      const updatedHat = await getHat(userEmail, hatId, false)
      setHat(updatedHat)
    } catch (err) {
      console.error('Error removing participant:', err)
      setError('Failed to remove participant')
    } finally {
      setRemovingParticipant(null)
    }
  }

  const handleEditEligibleRecipients = (participantEmail: string, currentEligible: string[]) => {
    setEditingEligibleFor(participantEmail)
    setTempEligibleRecipients(currentEligible)
    setExpandedParticipant(participantEmail)
  }

  const handleToggleEligible = (recipientName: string) => {
    setTempEligibleRecipients(prev =>
      prev.includes(recipientName)
        ? prev.filter(e => e !== recipientName)
        : [...prev, recipientName]
    )
  }

  const handleSaveEligibleRecipients = async (participantEmail: string) => {
    if (!hatId) return

    try {
      await editParticipant({
        organizerEmail: userEmail,
        hatId,
        email: participantEmail,
        eligibleRecipients: tempEligibleRecipients,
      })

      // Reload the hat data
      const updatedHat = await getHat(userEmail, hatId, false)
      setHat(updatedHat)
      setEditingEligibleFor(null)
    } catch (err) {
      console.error('Error updating eligible recipients:', err)
      setError('Failed to update eligible recipients')
    }
  }

  const handleCancelEditEligible = () => {
    setEditingEligibleFor(null)
    setTempEligibleRecipients([])
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
                {isEditing ? (
                  <input
                    type="text"
                    className="edit-name-input"
                    value={editedName}
                    onChange={(e) => setEditedName(e.target.value)}
                    disabled={saving}
                  />
                ) : (
                  <h2>{hat.name}</h2>
                )}
                <div className="hat-actions">
                  {isEditing ? (
                    <>
                      <button
                        className="secondary-button"
                        onClick={handleCancel}
                        disabled={saving}
                      >
                        Cancel
                      </button>
                      <button
                        className="primary-button"
                        onClick={handleSave}
                        disabled={saving}
                      >
                        {saving ? 'Saving...' : 'Save Changes'}
                      </button>
                    </>
                  ) : (
                    <button className="primary-button" onClick={handleEdit}>
                      Edit
                    </button>
                  )}
                </div>
              </div>

              <div className="hat-status-badges">
                {hat.organizerVerified && <span className="status-badge verified">Verified</span>}
                <span className={`status-badge ${hat.recipientsAssigned ? 'assigned' : 'not-assigned'}`}>
                  {hat.recipientsAssigned ? 'Recipients Assigned' : 'Recipients Not Assigned'}
                </span>
                <span className={`status-badge ${hat.invitationsQueued ? 'invited' : 'not-invited'}`}>
                  {hat.invitationsQueued ? 'Invitations Sent' : 'Invitations Not Sent'}
                </span>
              </div>

              <div className="hat-info-grid">
                <div className="info-card full-width">
                  <h3>Additional Information</h3>
                  {isEditing ? (
                    <textarea
                      className="edit-textarea"
                      value={editedAdditionalInfo}
                      onChange={(e) => setEditedAdditionalInfo(e.target.value)}
                      rows={4}
                      disabled={saving}
                    />
                  ) : (
                    <p>{hat.additionalInformation || <span className="text-muted">None</span>}</p>
                  )}
                </div>

                <div className="info-card">
                  <h3>Organizer</h3>
                  <p><strong>{hat.organizer.name}</strong></p>
                  <p className="text-muted">{hat.organizer.email}</p>
                </div>

                <div className="info-card">
                  <h3>Price Range</h3>
                  {isEditing ? (
                    <input
                      type="text"
                      className="edit-input"
                      value={editedPriceRange}
                      onChange={(e) => setEditedPriceRange(e.target.value)}
                      placeholder="e.g., $20-$50"
                      disabled={saving}
                    />
                  ) : (
                    <p>{hat.priceRange || <span className="text-muted">Not set</span>}</p>
                  )}
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
                <div className="section-header">
                  <h3>Participants ({hat.participants.length})</h3>
                  <button
                    className="primary-button"
                    onClick={() => setShowAddParticipantModal(true)}
                  >
                    Add Participant
                  </button>
                </div>
                {hat.participants.length > 0 ? (
                  <ul className="participants-list">
                    {hat.participants.map((participant, index) => {
                      const isOrganizer = participant.person.email === hat.organizer.email
                      const isExpanded = expandedParticipant === participant.person.email
                      const isEditingThis = editingEligibleFor === participant.person.email
                      const otherParticipants = hat.participants.filter(p => p.person.email !== participant.person.email)

                      return (
                        <li key={index} className="participant-item-wrapper">
                          <div className="participant-item">
                            <div className="participant-info">
                              <strong>{participant.person.name}</strong>
                              <span className="participant-email">{participant.person.email}</span>
                            </div>
                            <div className="participant-actions">
                              {participant.pickedRecipient && (
                                <div className="picked-recipient">
                                  → {participant.pickedRecipient}
                                </div>
                              )}
                              <button
                                className="manage-button"
                                onClick={() => setExpandedParticipant(isExpanded ? null : participant.person.email)}
                                title="Manage eligible recipients"
                              >
                                {isExpanded ? '▼' : '▶'} Eligible Recipients
                              </button>
                              {!isOrganizer && (
                                <button
                                  className="remove-button"
                                  onClick={() => handleRemoveParticipant(participant.person.email)}
                                  disabled={removingParticipant === participant.person.email}
                                  title="Remove participant"
                                >
                                  {removingParticipant === participant.person.email ? 'Removing...' : '×'}
                                </button>
                              )}
                            </div>
                          </div>

                          {isExpanded && (
                            <div className="eligible-recipients-section">
                              <p className="section-description">
                                Select which participants {participant.person.name} can be assigned to give a gift to:
                              </p>

                              {otherParticipants.length > 0 ? (
                                <>
                                  <div className="recipients-list">
                                    {otherParticipants.map((otherParticipant) => {
                                      const eligible = isEditingThis
                                        ? tempEligibleRecipients.includes(otherParticipant.person.name)
                                        : participant.eligibleRecipients.includes(otherParticipant.person.name)

                                      return (
                                        <label key={otherParticipant.person.email} className="recipient-checkbox">
                                          <input
                                            type="checkbox"
                                            checked={eligible}
                                            onChange={() => handleToggleEligible(otherParticipant.person.name)}
                                            disabled={!isEditingThis}
                                          />
                                          <span>{otherParticipant.person.name}</span>
                                          <span className="recipient-email">{otherParticipant.person.email}</span>
                                        </label>
                                      )
                                    })}
                                  </div>

                                  <div className="eligible-actions">
                                    {isEditingThis ? (
                                      <>
                                        <button
                                          className="primary-button"
                                          onClick={() => handleSaveEligibleRecipients(participant.person.email)}
                                        >
                                          Save
                                        </button>
                                        <button
                                          className="secondary-button"
                                          onClick={handleCancelEditEligible}
                                        >
                                          Cancel
                                        </button>
                                      </>
                                    ) : (
                                      <button
                                        className="secondary-button"
                                        onClick={() => handleEditEligibleRecipients(
                                          participant.person.email,
                                          participant.eligibleRecipients
                                        )}
                                      >
                                        Edit
                                      </button>
                                    )}
                                  </div>
                                </>
                              ) : (
                                <p className="text-muted">No other participants to assign</p>
                              )}
                            </div>
                          )}
                        </li>
                      )
                    })}
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

      {showAddParticipantModal && (
        <AddParticipantModal
          onClose={() => setShowAddParticipantModal(false)}
          onSubmit={handleAddParticipant}
        />
      )}
    </div>
  )
}
