import { useEffect, useState } from 'react'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './EditEmojiModal.css'
import { PERSON_EMOJI } from '../personEmoji'

interface EditEmojiModalProps {
  participantName: string
  /** What they wear now, so the grid opens with it selected. */
  currentEmoji: string
  isSaving: boolean
  error: string
  onCancel: () => void
  onConfirm: (emoji: string) => Promise<void>
}

/**
 * Choosing the face a participant is marked with.
 *
 * A grid of what is on offer rather than a text box, because the field is a closed list on the
 * server and typing into it could only produce a refusal. It also sidesteps the whole question of
 * an emoji keyboard, which on a desktop browser is somewhere between three keystrokes and
 * unavailable.
 *
 * No warning and no acknowledgement, unlike EditAddressModal. Nothing is sent when this is saved:
 * the invitations that already went out keep the face they were written with, and the only message
 * still to be written — the announcement at the end — will use whatever is chosen here.
 */
export function EditEmojiModal({
  participantName,
  currentEmoji,
  isSaving,
  error,
  onCancel,
  onConfirm,
}: EditEmojiModalProps) {
  const [selected, setSelected] = useState(currentEmoji)

  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !isSaving) onCancel()
    }

    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [isSaving, onCancel])

  const unchanged = selected === currentEmoji

  return (
    <div className="modal-overlay" onClick={isSaving ? undefined : onCancel}>
      <div
        className="modal-content"
        role="dialog"
        aria-modal="true"
        aria-labelledby="edit-emoji-title"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header">
          <h2 id="edit-emoji-title">Choose {participantName}&rsquo;s emoji</h2>
        </div>

        <div className="edit-emoji-body">
          <p className="edit-emoji-hint">
            This sits beside their name here, and beside it in the email telling somebody they drew
            them.
          </p>

          <div className="edit-emoji-grid" role="radiogroup" aria-label="Emoji">
            {PERSON_EMOJI.map((emoji) => (
              <button
                key={emoji}
                type="button"
                role="radio"
                aria-checked={emoji === selected}
                // The emoji itself is the whole of the button, so without this a screen reader is
                // left reading out a character name and nothing about what choosing it does.
                aria-label={`Mark ${participantName} with ${emoji}`}
                className={`edit-emoji-option${emoji === selected ? ' edit-emoji-option-selected' : ''}`}
                onClick={() => setSelected(emoji)}
                disabled={isSaving}
              >
                {emoji}
              </button>
            ))}
          </div>

          {error && <p className="edit-emoji-error">{error}</p>}

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
              onClick={() => onConfirm(selected)}
              disabled={unchanged || isSaving}
            >
              {isSaving ? 'Saving...' : 'Save'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
