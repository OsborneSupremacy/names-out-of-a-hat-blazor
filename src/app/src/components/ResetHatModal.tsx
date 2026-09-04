import { useState, FormEvent } from 'react'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './ResetHatModal.css'

interface ResetHatModalProps {
  hatName: string
  participantCount: number
  /** True when names are already out, so the reset throws a draw away as well as the rules. */
  hasBeenShaken: boolean
  onClose: () => void
  onSubmit: () => Promise<void>
}

/**
 * Confirms a reset, and says what is about to be lost before it is lost.
 *
 * A dialog rather than a confirm(), for the reason the shake dialog gives: what a reset throws away
 * depends on how far the exchange has got, and a browser prompt cannot tell the organizer that. The
 * exchange itself, its name and its people all survive — which is the part somebody about to press
 * this most needs to hear, since "Reset" on its own sounds like it might mean "Delete".
 */
export function ResetHatModal({
  hatName,
  participantCount,
  hasBeenShaken,
  onClose,
  onSubmit,
}: ResetHatModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    setError('')
    setIsSubmitting(true)

    try {
      await onSubmit()
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reset the gift exchange')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={isSubmitting ? undefined : onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Reset Gift Exchange</h2>
          <button
            className="close-button"
            onClick={onClose}
            aria-label="Close"
            disabled={isSubmitting}
          >
            ×
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <p className="modal-note">
            This takes <strong>{hatName}</strong> back to how it started. Nothing is deleted — the
            gift exchange keeps its name, its details, and all{' '}
            {participantCount === 1 ? 'one person' : `${participantCount} people`} in it.
          </p>

          <ul className="reset-effects">
            <li>Everybody will be allowed to draw everybody else again.</li>
            <li>Any rules you set about who can draw whom will be gone.</li>
            {hasBeenShaken && <li>The names that have been drawn will be thrown away.</li>}
          </ul>

          <p className="reset-warning">
            This cannot be undone. You will need to set the rules up again and shake the hat afresh.
          </p>

          {error && <div className="error-text reset-error">{error}</div>}

          <div className="modal-actions">
            <button
              type="button"
              className="secondary-button"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button type="submit" className="danger-button" disabled={isSubmitting}>
              {isSubmitting ? 'Resetting...' : 'Reset Gift Exchange'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
