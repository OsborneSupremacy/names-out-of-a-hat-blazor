import { useEffect, useRef, useState, FormEvent } from 'react'
import { DRAW_TYPE_OPTIONS, DEFAULT_DRAW_TYPE, DrawType, DrawTypeOption } from '../drawType'
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
  const [openExplanation, setOpenExplanation] = useState<DrawType | null>(null)
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
              <DrawTypeChoice
                key={option.value}
                option={option}
                isSelected={drawType === option.value}
                isExplanationOpen={openExplanation === option.value}
                onSelect={() => setDrawType(option.value)}
                onToggleExplanation={() =>
                  setOpenExplanation((current) =>
                    current === option.value ? null : option.value
                  )
                }
                onCloseExplanation={() => setOpenExplanation(null)}
              />
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

interface DrawTypeChoiceProps {
  option: DrawTypeOption
  isSelected: boolean
  isExplanationOpen: boolean
  onSelect: () => void
  onToggleExplanation: () => void
  onCloseExplanation: () => void
}

/**
 * One draw type: the radio and its plain summary, an information button, and the technical reading
 * behind that button.
 *
 * The explanation is a card that appears under the option rather than floating over it. It looks
 * like a popover and behaves like one — opens on click (not hover, which strands touch users and
 * anybody reading with a keyboard), closes on Escape or a click elsewhere — but it takes up space
 * instead of being positioned on top of things. The dialog it lives in scrolls its own content,
 * and an absolutely positioned panel inside a scrolling box is clipped by it: the third option's
 * would have been cut off at the bottom edge of the dialog. Sitting in the flow means the dialog
 * grows or scrolls to fit it, which is also the only version of this that works on a phone.
 */
function DrawTypeChoice({
  option,
  isSelected,
  isExplanationOpen,
  onSelect,
  onToggleExplanation,
  onCloseExplanation,
}: DrawTypeChoiceProps) {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isExplanationOpen) return

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onCloseExplanation()
    }

    const handlePointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) onCloseExplanation()
    }

    document.addEventListener('keydown', handleKeyDown)
    document.addEventListener('mousedown', handlePointerDown)

    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      document.removeEventListener('mousedown', handlePointerDown)
    }
  }, [isExplanationOpen, onCloseExplanation])

  return (
    <div className="shake-option-block" ref={containerRef}>
      <div className="shake-option-row">
        <label className="shake-option">
          <input
            type="radio"
            name="drawType"
            value={option.value}
            checked={isSelected}
            onChange={onSelect}
          />
          <span>
            <strong>{option.label}</strong>
            <span className="shake-option-hint">{option.summary}</span>
          </span>
        </label>

        {/* Outside the label on purpose: asking what an option means is not the same as choosing
            it, and a button inside a label would do both. */}
        <button
          type="button"
          className="shake-explain-trigger"
          onClick={onToggleExplanation}
          aria-expanded={isExplanationOpen}
          aria-label={`What "${option.label}" means, in technical terms`}
        >
          i
        </button>
      </div>

      {isExplanationOpen && (
        <p className="shake-explanation" role="note">
          {option.technical}
        </p>
      )}
    </div>
  )
}
