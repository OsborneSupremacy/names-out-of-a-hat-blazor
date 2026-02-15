import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getHat, editHat, addParticipant, removeParticipant, editParticipant, deleteHat, validateHat, assignRecipients, sendInvitations, closeHat, Hat } from '../api'
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
  const [isAssigning, setIsAssigning] = useState(false)
  const [validationErrors, setValidationErrors] = useState<string[]>([])
  const [isSendingInvitations, setIsSendingInvitations] = useState(false)
  const [isClosing, setIsClosing] = useState(false)

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

  const handleDeleteHat = async () => {
    if (!hatId || !hat) return

    const confirmed = window.confirm(
      `Are you sure you want to delete "${hat.name}"?\n\nThis action cannot be undone.`
    )

    if (!confirmed) return

    try {
      await deleteHat({
        organizerEmail: userEmail,
        hatId,
      })

      // Navigate back to home after successful deletion
      navigate('/')
    } catch (err) {
      console.error('Error deleting gift exchange:', err)
      setError('Failed to delete gift exchange')
    }
  }

  const handleSendInvitations = async () => {
    if (!hatId || !hat) return

    const confirmed = window.confirm(
      'This will send invitations to the gift exchange participants. Once you\'ve done this, this gift exchange will no longer be editable. Are you sure you want to proceed?'
    )

    if (!confirmed) return

    setIsSendingInvitations(true)
    setError('')

    try {
      await sendInvitations({
        organizerEmail: userEmail,
        hatId,
      })

      // Reload the hat data to reflect the updated invitationsQueued status
      const updatedHat = await getHat(userEmail, hatId, false)
      setHat(updatedHat)
    } catch (err) {
      console.error('Error sending invitations:', err)
      setError(err instanceof Error ? err.message : 'Failed to send invitations')
    } finally {
      setIsSendingInvitations(false)
    }
  }

  const handleValidate = async () => {
    if (!hatId || !hat) return

    setIsAssigning(true)
    setValidationErrors([])
    setError('')

    try {
      const validationResult = await validateHat({
        organizerEmail: userEmail,
        hatId,
      })

      if (!validationResult.success) {
        setValidationErrors(validationResult.errors)
      } else {
        // Validation successful - reload to get updated status
        const updatedHat = await getHat(userEmail, hatId, false)
        setHat(updatedHat)
        setValidationErrors([])
      }
    } catch (err) {
      console.error('Error validating gift exchange:', err)
      setError(err instanceof Error ? err.message : 'Failed to validate gift exchange')
    } finally {
      setIsAssigning(false)
    }
  }

  const handleCloseHat = async () => {
    if (!hatId || !hat) return

    const confirmed = window.confirm(
      'Are you sure you want to close the gift exchange? The picked names for all recipients will be revealed. You should only do this once the exchange has actually happened.'
    )
    if (!confirmed) return

    setIsClosing(true)
    setError('')

    try {
      await closeHat({
        organizerEmail: userEmail,
        hatId,
      })

      // Reload the hat data
      const updatedHat = await getHat(userEmail, hatId, false)
      setHat(updatedHat)
    } catch (err) {
      console.error('Error closing gift exchange:', err)
      setError(err instanceof Error ? err.message : 'Failed to close gift exchange')
    } finally {
      setIsClosing(false)
    }
  }

  const handleShakeHat = async () => {
    if (!hatId || !hat) return

    // If recipients are already assigned, confirm before re-shaking
    if (hat.recipientsAssigned) {
      const confirmed = window.confirm(
        'The hat has already been shaken and all participants have a name picked. Are you sure you want to shake the hat again?'
      )
      if (!confirmed) return
    }

    setIsAssigning(true)
    setValidationErrors([])
    setError('')

    try {
      // Assign recipients (validation not needed - hat is already validated if status is NAMES_ASSIGNED)
      await assignRecipients({
        organizerEmail: userEmail,
        hatId,
      })

      // Reload the hat data
      const updatedHat = await getHat(userEmail, hatId, false)
      setHat(updatedHat)
    } catch (err) {
      console.error('Error shaking hat:', err)
      setError(err instanceof Error ? err.message : 'Failed to shake the hat')
    } finally {
      setIsAssigning(false)
    }
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
                {!hat.invitationsQueued && (
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
                )}
              </div>

              <div className="status-progression">
                <div className="status-steps">
                  <div className={`status-step ${hat.status === 'IN_PROGRESS' ? 'active' : ''}`}>
                    <div className="status-step-indicator"></div>
                    <div className="status-step-label">In Progress</div>
                  </div>
                  <div className="status-step-connector"></div>
                  <div className={`status-step ${hat.status === 'READY_FOR_ASSIGNMENT' ? 'active' : ''}`}>
                    <div className="status-step-indicator"></div>
                    <div className="status-step-label">Ready For Assignment</div>
                  </div>
                  <div className="status-step-connector"></div>
                  <div className={`status-step ${hat.status === 'NAMES_ASSIGNED' ? 'active' : ''}`}>
                    <div className="status-step-indicator"></div>
                    <div className="status-step-label">Names Assigned</div>
                  </div>
                  <div className="status-step-connector"></div>
                  <div className={`status-step ${hat.status === 'INVITATIONS_SENT' ? 'active' : ''}`}>
                    <div className="status-step-indicator"></div>
                    <div className="status-step-label">Invitations Sent</div>
                  </div>
                  <div className="status-step-connector"></div>
                  <div className={`status-step ${hat.status === 'CLOSED' ? 'active' : ''}`}>
                    <div className="status-step-indicator"></div>
                    <div className="status-step-label">Closed</div>
                  </div>
                </div>
              </div>

              <div className="status-action-section">
                {hat.status === 'IN_PROGRESS' && (
                  <div className="action-container">
                    <button
                      className="action-button validate-button"
                      onClick={handleValidate}
                      disabled={hat.participants.length < 3 || isAssigning}
                    >
                      {isAssigning ? 'Validating...' : 'Validate Gift Exchange'}
                    </button>
                    {hat.participants.length < 3 && (
                      <p className="action-hint">Add at least 3 participants to validate</p>
                    )}
                    {validationErrors.length > 0 && (
                      <div className="validation-errors">
                        <h4>Validation failed:</h4>
                        <ul>
                          {validationErrors.map((error, index) => (
                            <li key={index}>{error}</li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                )}

                {hat.status === 'READY_FOR_ASSIGNMENT' && (
                  <div className="action-container">
                    <button
                      className="action-button shake-button"
                      onClick={handleShakeHat}
                      disabled={isAssigning}
                    >
                      {isAssigning ? 'Shaking...' : 'Shake the Hat!'}
                    </button>
                    <p className="action-hint">Assign gift recipients to participants</p>
                    {validationErrors.length > 0 && (
                      <div className="validation-errors">
                        <h4>Cannot shake the hat:</h4>
                        <ul>
                          {validationErrors.map((error, index) => (
                            <li key={index}>{error}</li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                )}

                {hat.status === 'NAMES_ASSIGNED' && (
                  <div className="action-container">
                    <button
                      className="action-button send-button"
                      onClick={handleSendInvitations}
                      disabled={isSendingInvitations}
                    >
                      {isSendingInvitations ? 'Sending Invitations...' : 'Send Invitations'}
                    </button>
                    <button
                      className="action-button-secondary shake-again-button"
                      onClick={handleShakeHat}
                      disabled={isAssigning}
                    >
                      {isAssigning ? 'Shaking...' : 'Shake the Hat Again'}
                    </button>
                    <p className="action-hint">Send invitations to all participants, or re-shuffle assignments</p>
                  </div>
                )}

                {hat.status === 'INVITATIONS_SENT' && (
                  <div className="action-container">
                    <button
                      className="action-button action-close-button"
                      onClick={handleCloseHat}
                      disabled={isClosing}
                    >
                      {isClosing ? 'Closing...' : 'Close Gift Exchange'}
                    </button>
                    <p className="action-hint">Mark this gift exchange as completed. Once you do this, you will be able to view the picked recipients of all participants.</p>
                  </div>
                )}

                {hat.status === 'CLOSED' && (
                  <div className="action-container">
                    <p className="action-complete">This gift exchange is closed</p>
                  </div>
                )}
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
                  {!hat.invitationsQueued && (
                    <button
                      className="primary-button"
                      onClick={() => setShowAddParticipantModal(true)}
                    >
                      Add Participant
                    </button>
                  )}
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
                              <strong>
                                {participant.person.name}
                                {hat.recipientsAssigned && participant.pickedRecipient && (
                                  <span className="assigned-tag" title={`${participant.person.name} has picked a name.`}>🏷️</span>
                                )}
                                {isOrganizer && <span className="organizer-badge">Organizer</span>}
                              </strong>
                              <span className="participant-email">{participant.person.email}</span>
                            </div>
                            {!hat.invitationsQueued && (
                              <div className="participant-actions">
                                <button
                                  className="manage-button"
                                  onClick={() => setExpandedParticipant(isExpanded ? null : participant.person.email)}
                                  title="Manage eligible recipients"
                                >
                                  {isExpanded ? '▼' : '▶'} Eligible Recipients ({participant.eligibleRecipients.length} / {otherParticipants.length})
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
                            )}
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
                                          disabled={tempEligibleRecipients.length === 0}
                                          title={tempEligibleRecipients.length === 0 ? 'At least one recipient must be selected' : ''}
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

              {!hat.invitationsQueued && (
                <div className="delete-section">
                  <button
                    className="danger-button"
                    onClick={handleDeleteHat}
                  >
                    Delete Gift Exchange
                  </button>
                </div>
              )}
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
