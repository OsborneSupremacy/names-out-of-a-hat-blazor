import { useState, FormEvent } from 'react'
import './CreateHatModal.css'

interface CreateHatModalProps {
  organizerName: string
  organizerEmail: string
  onClose: () => void
  onSubmit: (hatName: string) => Promise<void>
}

export function CreateHatModal({ organizerName, organizerEmail, onClose, onSubmit }: CreateHatModalProps) {
  const [hatName, setHatName] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    const trimmedName = hatName.trim()
    if (!trimmedName) {
      setError('Gift Exchange name cannot be empty')
      return
    }

    setError('')
    setIsSubmitting(true)

    try {
      await onSubmit(trimmedName)
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

          <div className="form-group">
            <label>Organizer</label>
            <div className="organizer-info">
              <div><strong>{organizerName}</strong></div>
              <div className="organizer-email">{organizerEmail}</div>
            </div>
          </div>

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
