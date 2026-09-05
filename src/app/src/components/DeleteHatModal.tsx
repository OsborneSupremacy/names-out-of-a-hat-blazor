import { useState, FormEvent } from 'react'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './DestructiveModal.css'

interface DeleteHatModalProps {
  hatName: string
  participantCount: number
  /** True when names are already out, so the delete takes a finished draw with it. */
  hasBeenShaken: boolean
  onClose: () => void
  onSubmit: () => Promise<void>
}

/**
 * Confirms a delete, and says what is about to be lost before it is lost.
 *
 * The sibling of {@link ResetHatModal}, and deliberately its mirror image: the reset dialog leads
 * with what survives, because the thing an organizer fears there is losing their people. Here they
 * do lose them, so the dialog says so first and offers the way out — the export sitting directly
 * above this in the same menu — rather than leaving them to think of it afterwards.
 *
 * A dialog rather than the confirm() this used to be: what a delete takes depends on how far the
 * exchange has got, and a browser prompt can neither say that nor point at the export.
 */
export function DeleteHatModal({
  hatName,
  participantCount,
  hasBeenShaken,
  onClose,
  onSubmit,
}: DeleteHatModalProps) {
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
      setError(err instanceof Error ? err.message : 'Failed to delete the gift exchange')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={isSubmitting ? undefined : onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Delete Gift Exchange</h2>
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
            This throws <strong>{hatName}</strong> away for good. It will not be on your list of gift
            exchanges any more, and nobody will be able to open it again.
          </p>

          <ul className="destructive-effects">
            <li>
              {participantCount === 1 ? 'The one person' : `All ${participantCount} people`} in it
              will be gone, along with everything you typed in about them.
            </li>
            <li>Any rules you set about who can draw whom will be gone.</li>
            {hasBeenShaken && <li>The names that have been drawn will be gone.</li>}
          </ul>

          <p className="destructive-warning">
            This cannot be undone, and no copy is kept. If you might want one, close this and export
            the gift exchange first — it is in the same menu you came from.
          </p>

          {error && <div className="error-text destructive-error">{error}</div>}

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
              {isSubmitting ? 'Deleting...' : 'Delete Gift Exchange'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
