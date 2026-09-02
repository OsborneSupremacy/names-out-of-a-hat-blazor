import { useState, FormEvent } from 'react'
import './CreateHatModal.css'

interface CreateHatModalProps {
  organizerName: string
  organizerEmail: string
  onClose: () => void
  onSubmit: (hatName: string, organizerName: string) => Promise<void>
}

export function CreateHatModal({ organizerName, organizerEmail, onClose, onSubmit }: CreateHatModalProps) {
  // Once we know who the organizer is there is nothing to ask them, so the name and address are
  // left off the form entirely rather than shown back to them for confirmation.
  const knownOrganizerName = organizerName.trim()
  const [hatName, setHatName] = useState('')
  const [name, setName] = useState(organizerName)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    const trimmedName = hatName.trim()
    if (!trimmedName) {
      setError('Gift Exchange name cannot be empty')
      return
    }

    const trimmedOrganizerName = knownOrganizerName || name.trim()
    if (!trimmedOrganizerName) {
      setError('Your name cannot be empty')
      return
    }

    setError('')
    setIsSubmitting(true)

    try {
      await onSubmit(trimmedName, trimmedOrganizerName)
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create gift exchange')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Create New Gift Exchange</h2>
          <button className="close-button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="hatName">Gift Exchange Name *</label>
            <input
              type="text"
              id="hatName"
              value={hatName}
              onChange={(e) => setHatName(e.target.value)}
              placeholder="e.g., Family Christmas 2026"
              autoFocus
              disabled={isSubmitting}
            />
            {error && <div className="error-text">{error}</div>}
          </div>

          {!knownOrganizerName && (
            <div className="form-group">
              <label htmlFor="organizerName">Your Name *</label>
              <input
                type="text"
                id="organizerName"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="How participants will see you"
                disabled={isSubmitting}
              />
              <div className="organizer-info">
                <div className="organizer-email">{organizerEmail}</div>
              </div>
            </div>
          )}

          <div className="modal-actions">
            <button
              type="button"
              className="secondary-button"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="primary-button"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Creating...' : 'Create Gift Exchange'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
