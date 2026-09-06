import { useState } from 'react'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './EditParticipantNameModal.css'

interface EditParticipantNameModalProps {
  currentName: string
  currentEmail: string
  isSaving: boolean
  error: string
  onCancel: () => void
  onConfirm: (newName: string) => Promise<void>
}

/**
 * Changing the name one participant is known by.
 *
 * Separate from EditNameModal, which edits the signed-in organizer's own name from the header.
 * That one is about you; this one is about somebody else, and the difference is worth a dialog of
 * its own because the two things it has to say are things that dialog does not.
 *
 * The first is reach. A name belongs to the person rather than to their place in one exchange, so
 * this changes what they are called everywhere they appear — including exchanges this organizer
 * does not run and cannot see. Stated before the fact rather than discovered afterwards.
 *
 * The second is that it may simply not be allowed. Only the person themselves and whoever first
 * added them may change a name, and the client has no way to know which of those this organizer is
 * until it asks: nothing in the participant payload says who introduced whom. So the control is
 * offered, the attempt is made, and the server's refusal is shown here — which is a better
 * experience than an "Edit Name" that is greyed out for reasons the page cannot explain.
 */
export function EditParticipantNameModal({
  currentName,
  currentEmail,
  isSaving,
  error,
  onCancel,
  onConfirm,
}: EditParticipantNameModalProps) {
  const [name, setName] = useState(currentName)

  const trimmed = name.trim()
  const unchanged = trimmed === currentName
  const canSubmit = trimmed.length > 0 && !unchanged && !isSaving

  return (
    <div className="modal-overlay" onClick={isSaving ? undefined : onCancel}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Edit {currentName}&rsquo;s name</h2>
          <button className="close-button" onClick={onCancel} aria-label="Close" disabled={isSaving}>
            ×
          </button>
        </div>

        <div className="edit-participant-name-body">
          <p className="edit-participant-name-current">
            Currently <strong>{currentName}</strong> at {currentEmail}
          </p>

          <label className="edit-participant-name-label" htmlFor="participant-name">
            Name
          </label>
          <input
            id="participant-name"
            type="text"
            className="edit-participant-name-input"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="What everybody should call them"
            disabled={isSaving}
            autoFocus
          />

          {/*
            * Said before the save rather than after it. An organizer fixing a typo is not
            * necessarily expecting the fix to land in somebody else's gift exchange, and the one
            * moment they can decide not to is this one.
            */}
          <p className="edit-participant-name-note">
            A name belongs to the person, so this changes what they are called in every gift
            exchange they take part in — not only this one. Invitations already sent still show the
            old name.
          </p>

          {error && <p className="edit-participant-name-error">{error}</p>}

          <div className="modal-actions">
            <button
              type="button"
              className="secondary-button"
              onClick={onCancel}
              disabled={isSaving}
            >
              Cancel
            </button>
            <button
              type="button"
              className="primary-button"
              onClick={() => onConfirm(trimmed)}
              disabled={!canSubmit}
            >
              {isSaving ? 'Saving...' : 'Save'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
