import { useState, FormEvent } from 'react'
import { DRAW_TYPE_OPTIONS, DEFAULT_DRAW_TYPE, DrawType } from '../drawType'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './ShakeHatModal.css'

interface ShakeHatModalProps {
  /** True when names are already out, which makes this a re-shake and throws the last draw away. */
  isReshake: boolean
  participantCount: number
  onClose: () => void
  onSubmit: (drawType: DrawType) => Promise<void>
}

/**
 * Asks how the names should be drawn, then draws them.
 *
 * The draw type is asked here rather than kept as a property of the exchange because it is a
 * property of the draw: once names are out the rule is baked into the assignment and there is
 * nothing left for a stored setting to govern. A re-shake is a new draw, so it asks again — and
 * the confirmation for throwing away the previous one lives in this dialog too, rather than in the
 * browser confirm() it used to be, so that the organizer sees what they are about to choose and
 * what they are about to lose in the same place.
 */
export function ShakeHatModal({
  isReshake,
  participantCount,
  onClose,
  onSubmit,
}: ShakeHatModalProps) {
  const [drawType, setDrawType] = useState<DrawType>(DEFAULT_DRAW_TYPE)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const isConstrained = drawType !== 'ANYTHING_GOES'

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    setError('')
    setIsSubmitting(true)

    try {
      await onSubmit(drawType)
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to shake the hat')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={isSubmitting ? undefined : onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{isReshake ? 'Shake the Hat Again' : 'Shake the Hat'}</h2>
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
          {isReshake && (
            <p className="shake-reshake-note">
              Everybody has already drawn a name. Shaking again throws that draw away and starts a
              new one — nobody has been told who they drew yet, so nothing is spoiled.
            </p>
          )}

          <fieldset className="shake-options" disabled={isSubmitting}>
            <legend>How should the names be drawn?</legend>

            {DRAW_TYPE_OPTIONS.map((option) => (
              <label className="shake-option" key={option.value}>
                <input
                  type="radio"
                  name="drawType"
                  value={option.value}
                  checked={drawType === option.value}
                  onChange={() => setDrawType(option.value)}
                />
                <span>
                  <strong>{option.label}</strong>
                  <span className="shake-option-hint">{option.summary}</span>
                </span>
              </label>
            ))}
          </fieldset>

          {/* Stated for every draw type, and above the warning rather than inside it, because it is
              true regardless of what is selected. The rules people set up by hand are the thing
              they would most reasonably fear a new setting had quietly replaced. */}
          <p className="shake-note">
            Whoever each person is allowed to draw still applies, whichever of these you pick. This
            only adds to those rules — it never overrides them.
          </p>

          {isConstrained && (
            <p className="shake-warning">
              Asking for more than "Anything goes" makes the draw harder to satisfy, and with the
              rules you have set up it may not be possible at all
              {participantCount > 0 && ` for these ${participantCount} people`}. If it isn't, the
              hat will tell you and nothing will change — you can shake again, pick "Anything goes",
              or loosen who is allowed to draw whom.
            </p>
          )}

          {error && <div className="error-text shake-error">{error}</div>}

          <div className="modal-actions">
            <button
              type="button"
              className="secondary-button"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={isSubmitting}>
              {isSubmitting ? 'Shaking...' : 'Shake the Hat!'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
