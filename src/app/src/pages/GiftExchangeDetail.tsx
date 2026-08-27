import { Fragment, useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  getHat,
  editHat,
  addParticipant,
  removeParticipant,
  editParticipant,
  deleteHat,
  validateHat,
  assignRecipients,
  previewInvitations,
  sendInvitations,
  closeHat,
  copyHat,
  Hat,
  PreviewInvitationsResponse,
} from '../api'
import { HAT_STATUS_STEPS, formatHatStatus } from '../hatStatus'
import { Header } from '../components/Header'
import { Footer } from '../components/Footer'
import { AddParticipantModal } from '../components/AddParticipantModal'
import { InvitationsPreviewModal } from '../components/InvitationsPreviewModal'
import { SendConfirmationModal } from '../components/SendConfirmationModal'
import { CopyHatModal } from '../components/CopyHatModal'
import './GiftExchangeDetail.css'

interface GiftExchangeDetailProps {
  userEmail: string
  onSignOut: () => void
}

export function GiftExchangeDetail({ userEmail, onSignOut }: GiftExchangeDetailProps) {

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
  const [editingEligibleFor, setEditingEligibleFor] = useState<string | null>(null)
  const [tempEligibleRecipients, setTempEligibleRecipients] = useState<string[]>([])
  const [isAssigning, setIsAssigning] = useState(false)
  const [validationErrors, setValidationErrors] = useState<string[]>([])
  const [isPreviewLoading, setIsPreviewLoading] = useState(false)
  const [showInvitationsPreview, setShowInvitationsPreview] = useState(false)
  const [invitationsPreview, setInvitationsPreview] = useState<PreviewInvitationsResponse | null>(null)
  const [isSendingInvitations, setIsSendingInvitations] = useState(false)
  const [showSendConfirmation, setShowSendConfirmation] = useState(false)
  const [isClosing, setIsClosing] = useState(false)
  const [showCopyModal, setShowCopyModal] = useState(false)

  useEffect(() => {
    async function loadHat() {
      if (!hatId) {
        setError('No gift exchange ID provided')
        setLoading(false)
        return
      }

      try {
        const hatData = await getHat(userEmail, hatId)
        setHat(hatData)
        setEditedName(hatData.name)
        setEditedAdditionalInfo(hatData.additionalInformation)
        setEditedPriceRange(hatData.priceRange)
      } catch (err) {
        console.error('Error loading gift exchange:', err)
        setError(err instanceof Error ? err.message : 'Failed to load gift exchange details')
      } finally {
        setLoading(false)
      }
    }

    loadHat()
  }, [hatId, userEmail])

  // The organizer's name is displayed from the loaded hat, so reload it rather than patching
  // state that the server has just changed underneath us.
  const handleNameUpdated = async () => {
    if (!hatId) return

    try {
      setHat(await getHat(userEmail, hatId))
    } catch (err) {
      console.error('Error reloading gift exchange after rename:', err)
    }
  }

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
      const updatedHat = await getHat(userEmail, hatId)
      setHat(updatedHat)
      setIsEditing(false)
    } catch (err) {
      console.error('Error saving changes:', err)
      setError(err instanceof Error ? err.message : 'Failed to save changes')
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
    const updatedHat = await getHat(userEmail, hatId)
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
      const updatedHat = await getHat(userEmail, hatId)
      setHat(updatedHat)
    } catch (err) {
      console.error('Error removing participant:', err)
      setError(err instanceof Error ? err.message : 'Failed to remove participant')
    } finally {
      setRemovingParticipant(null)
    }
  }

  const handleEditEligibleRecipients = (participantEmail: string, currentEligible: string[]) => {
    setEditingEligibleFor(participantEmail)
    setTempEligibleRecipients(currentEligible)
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
      const updatedHat = await getHat(userEmail, hatId)
      setHat(updatedHat)
      setEditingEligibleFor(null)
    } catch (err) {
      console.error('Error updating eligible recipients:', err)
      setError(err instanceof Error ? err.message : 'Failed to update eligible recipients')
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
      setError(err instanceof Error ? err.message : 'Failed to delete gift exchange')
    }
  }

  const handleSendInvitations = async () => {
    if (!hatId || !hat) return

    setIsPreviewLoading(true)
    setError('')

    try {
      const preview = await previewInvitations({
        organizerEmail: userEmail,
        hatId,
      })

      setInvitationsPreview(preview)
      setShowInvitationsPreview(true)
    } catch (err) {
      console.error('Error loading invitation preview:', err)
      setError(err instanceof Error ? err.message : 'Failed to load invitation preview')
    } finally {
      setIsPreviewLoading(false)
    }
  }

  const handleBackFromInvitationsPreview = () => {
    if (isSendingInvitations) return
    setShowInvitationsPreview(false)
  }

  // The preview's Send button no longer sends; it opens the acknowledgment step.
  const handleProceedToSendConfirmation = async () => {
    setShowInvitationsPreview(false)
    setShowSendConfirmation(true)
  }

  const handleCancelSendConfirmation = () => {
    if (isSendingInvitations) return

    // Back to the preview rather than out of the flow entirely, so cancelling is not punishing.
    setShowSendConfirmation(false)
    setShowInvitationsPreview(true)
  }

  const handleConfirmSendInvitations = async () => {
    if (!hatId || !hat) return

    setIsSendingInvitations(true)
    setError('')

    try {
      await sendInvitations({
        organizerEmail: userEmail,
        hatId,
      })

      // Reload the hat data to reflect the updated status
      const updatedHat = await getHat(userEmail, hatId)
      setHat(updatedHat)
      setShowSendConfirmation(false)
      setInvitationsPreview(null)
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
        const updatedHat = await getHat(userEmail, hatId)
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
      'Reveal who everybody drew? This shows the picked name for every participant and cannot be undone, so only do it once the gift exchange has actually happened.'
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
      const updatedHat = await getHat(userEmail, hatId)
      setHat(updatedHat)
    } catch (err) {
      console.error('Error revealing picked names:', err)
      setError(err instanceof Error ? err.message : 'Failed to reveal the picked names')
    } finally {
      setIsClosing(false)
    }
  }

  /**
   * The copy is a separate gift exchange, so the organizer is taken to it. Staying on a revealed
   * exchange after asking for a new one would leave them wondering whether anything happened.
   */
  const handleCopyHat = async (newHatName: string, excludePreviousRecipients: boolean) => {
    if (!hatId) return

    const { hatId: newHatId } = await copyHat({
      organizerEmail: userEmail,
      hatId,
      newHatName,
      excludePreviousRecipients,
    })

    navigate(`/gift-exchange/${newHatId}`)
  }

  const handleShakeHat = async () => {
    if (!hatId || !hat) return

    // If recipients are already assigned, confirm before re-shaking
    if (hat.status === 'NAMES_ASSIGNED') {
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
      const updatedHat = await getHat(userEmail, hatId)
      setHat(updatedHat)
    } catch (err) {
      console.error('Error shaking hat:', err)
      setError(err instanceof Error ? err.message : 'Failed to shake the hat')
    } finally {
      setIsAssigning(false)
    }
  }

  const isEditableStatus = hat
    ? ['IN_PROGRESS', 'READY_FOR_ASSIGNMENT', 'NAMES_ASSIGNED'].includes(hat.status)
    : false

  // Eligibility is about who somebody may draw, so with only the organizer in the hat there is
  // nothing to edit. Offering the row anyway opened an editor with nothing in it.
  const canEditEligibility = isEditableStatus && (hat?.participants.length ?? 0) > 1

  return (
    <div className="app-container">
      <Header
        userEmail={userEmail}
        givenName={hat ? hat.organizer.name : null}
        onSignOut={onSignOut}
        onNameUpdated={handleNameUpdated}
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
                {hat.status !== 'INVITATIONS_SENT' && hat.status !== 'READY_TO_CLOSE' && hat.status !== 'CLOSED' && (
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
                  {HAT_STATUS_STEPS.map((status, index) => (
                    <Fragment key={status}>
                      {index > 0 && <div className="status-step-connector"></div>}
                      <div className={`status-step ${hat.status === status ? 'active' : ''}`}>
                        <div className="status-step-indicator"></div>
                        <div className="status-step-label">{formatHatStatus(status)}</div>
                      </div>
                    </Fragment>
                  ))}
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
                      disabled={isSendingInvitations || isPreviewLoading}
                    >
                      {isPreviewLoading
                        ? 'Loading Preview...'
                        : isSendingInvitations
                          ? 'Sending Invitations...'
                          : 'Send Invitations'}
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
                    <p className="action-complete">Invitations have been sent</p>
                    <p className="action-hint">The picks stay hidden for a short while so nobody's surprise is spoiled early.</p>
                  </div>
                )}

                {hat.status === 'READY_TO_CLOSE' && (
                  <div className="action-container">
                    <button
                      className="action-button action-close-button"
                      onClick={handleCloseHat}
                      disabled={isClosing}
                    >
                      {isClosing ? 'Revealing...' : 'Reveal Picked Names'}
                    </button>
                    <p className="action-hint">Show who everybody drew. Do this once the gift exchange has actually happened — it cannot be undone.</p>
                  </div>
                )}

                {hat.status === 'CLOSED' && (
                  <div className="action-container">
                    <p className="action-complete">The picks are revealed</p>
                    <p className="action-hint">See below for the names everyone was assigned.</p>
                    <button
                      className="action-button copy-hat-button"
                      onClick={() => setShowCopyModal(true)}
                    >
                      Copy to a New Exchange
                    </button>
                    <p className="action-hint">Start the next one with the same people and the same rules. This exchange stays as it is.</p>
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
              </div>

              <div className="participants-section">
                <div className="section-header">
                  <div>
                    <h3>Participants ({hat.participants.length})</h3>
                    {canEditEligibility && (
                      <p className="participants-edit-hint">Click a participant row to edit eligible recipients.</p>
                    )}
                  </div>
                  {hat.status !== 'CLOSED' && hat.status !== 'INVITATIONS_SENT' && hat.status !== 'READY_TO_CLOSE' && (
                    <button
                      className="primary-button"
                      onClick={() => setShowAddParticipantModal(true)}
                    >
                      Add Participant
                    </button>
                  )}
                </div>
                {hat.participants.length > 0 ? (
                  (hat.status === 'CLOSED' || hat.status === 'INVITATIONS_SENT' || hat.status === 'READY_TO_CLOSE') ? (
                    <table className="participants-table">
                      <thead>
                        <tr>
                          <th>Name</th>
                          <th>Picked Recipient</th>
                        </tr>
                      </thead>
                      <tbody>
                        {hat.participants.map((participant, index) => (
                          <tr key={index}>
                            <td>
                              <div className="participant-name-cell">
                                <strong>{participant.person.name}</strong>
                                <span className="participant-email">{participant.person.email}</span>
                              </div>
                            </td>
                            <td>
                              <strong>{participant.pickedRecipient || 'Not assigned'}</strong>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  ) : (
                    <table className="participants-table">
                      <thead>
                        <tr>
                          <th>Name</th>
                          <th>Recipients</th>
                        </tr>
                      </thead>
                      <tbody>
                        {hat.participants.map((participant, index) => {
                          const isOrganizer = participant.person.email === hat.organizer.email
                          const isEditingThis = editingEligibleFor === participant.person.email
                          const otherParticipants = hat.participants.filter(p => p.person.email !== participant.person.email)
                          const eligibleRecipients = otherParticipants
                            .filter(otherParticipant => participant.eligibleRecipients.includes(otherParticipant.person.name))
                            .map(otherParticipant => otherParticipant.person.name)
                          const ineligibleRecipients = otherParticipants
                            .filter(otherParticipant => !participant.eligibleRecipients.includes(otherParticipant.person.name))
                            .map(otherParticipant => otherParticipant.person.name)

                          return (
                            <tr
                              key={index}
                              className={[
                                isEditingThis ? 'editing-row' : '',
                                !isEditingThis && canEditEligibility ? 'clickable-row' : '',
                                canEditEligibility ? 'editable-status-clickable' : '',
                              ].filter(Boolean).join(' ')}
                              onClick={() => canEditEligibility && !isEditingThis && handleEditEligibleRecipients(
                                participant.person.email,
                                participant.eligibleRecipients
                              )}
                            >
                              <td>
                                <div className="participant-name-cell">
                                  <strong>
                                    {participant.person.name}
                                    {isOrganizer && <span className="organizer-badge">Organizer</span>}
                                  </strong>
                                  <span className="participant-email">{participant.person.email}</span>
                                  {canEditEligibility && !isEditingThis && (
                                    <span className="row-edit-indicator">Click row to edit</span>
                                  )}
                                  {isEditingThis && !isOrganizer && (
                                    <button
                                      className="danger-button remove-participant-button"
                                      onClick={() => handleRemoveParticipant(participant.person.email)}
                                      disabled={removingParticipant === participant.person.email}
                                    >
                                      {removingParticipant === participant.person.email ? 'Removing...' : 'Remove Participant'}
                                    </button>
                                  )}
                                </div>
                              </td>
                              <td>
                                <div className="eligible-recipients-section">
                                  {otherParticipants.length > 0 ? (
                                    <>
                                      {isEditingThis ? (
                                        <>
                                          <div className="recipients-list">
                                            {otherParticipants.map((otherParticipant) => {
                                              const eligible = tempEligibleRecipients.includes(otherParticipant.person.name)

                                              return (
                                                <label key={otherParticipant.person.email} className="recipient-checkbox">
                                                  <input
                                                    type="checkbox"
                                                    checked={eligible}
                                                    onChange={() => handleToggleEligible(otherParticipant.person.name)}
                                                  />
                                                  <span>{otherParticipant.person.name}</span>
                                                </label>
                                              )
                                            })}
                                          </div>

                                          <div className="eligible-actions">
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
                                          </div>
                                        </>
                                      ) : (
                                        <div className="recipient-summary">
                                          <div className="recipient-group recipient-group-eligible">
                                            <span className="recipient-group-label">Eligible</span>
                                            {eligibleRecipients.length > 0 ? (
                                              <div className="recipient-chips">
                                                {eligibleRecipients.map((name, nameIndex) => (
                                                  <span key={`${name}-${nameIndex}`} className="recipient-chip">{name}</span>
                                                ))}
                                              </div>
                                            ) : (
                                              <span className="recipient-group-empty">None</span>
                                            )}
                                          </div>
                                          {ineligibleRecipients.length > 0 && (
                                            <div className="recipient-group recipient-group-ineligible">
                                              <span className="recipient-group-label">Ineligible</span>
                                              <div className="recipient-chips">
                                                {ineligibleRecipients.map((name, nameIndex) => (
                                                  <span key={`${name}-${nameIndex}`} className="recipient-chip">{name}</span>
                                                ))}
                                              </div>
                                            </div>
                                          )}
                                        </div>
                                      )}
                                    </>
                                  ) : (
                                    <p className="text-muted">No other participants to assign</p>
                                  )}
                                </div>
                              </td>
                            </tr>
                          )
                        })}
                      </tbody>
                    </table>
                  )
                ) : (
                  <p className="text-muted">No participants yet</p>
                )}
              </div>

              {hat.status !== 'INVITATIONS_SENT' && hat.status !== 'READY_TO_CLOSE' && hat.status !== 'CLOSED' && (
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

      {showInvitationsPreview && invitationsPreview && (
        <InvitationsPreviewModal
          subject={invitationsPreview.subject}
          htmlBody={invitationsPreview.htmlBody}
          isSending={isSendingInvitations}
          onBack={handleBackFromInvitationsPreview}
          onSend={handleProceedToSendConfirmation}
        />
      )}

      {showCopyModal && hat && (
        <CopyHatModal
          sourceHatName={hat.name}
          participants={hat.participants}
          onClose={() => setShowCopyModal(false)}
          onSubmit={handleCopyHat}
        />
      )}

      {showSendConfirmation && invitationsPreview && hat && (
        <SendConfirmationModal
          organizerEmail={userEmail}
          senderIpAddress={invitationsPreview.senderIpAddress}
          recipientCount={hat.participants.length}
          isSending={isSendingInvitations}
          onCancel={handleCancelSendConfirmation}
          onConfirm={handleConfirmSendInvitations}
        />
      )}
    </div>
  )
}
