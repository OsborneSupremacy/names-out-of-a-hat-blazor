import { useState, FormEvent } from 'react'
import './AddParticipantModal.css'

interface AddParticipantModalProps {
  onClose: () => void
  onSubmit: (name: string, email: string) => Promise<void>
}

export function AddParticipantModal({ onClose, onSubmit }: AddParticipantModalProps) {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    const trimmedName = name.trim()
    const trimmedEmail = email.trim()

    if (!trimmedName) {
      setError('Name cannot be empty')
      return
    }

    if (!trimmedEmail) {
      setError('Email cannot be empty')
      return
    }

    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(trimmedEmail)) {
      setError('Please enter a valid email address')
      return
    }

    setError('')
    setIsSubmitting(true)

    try {
      await onSubmit(trimmedName, trimmedEmail)
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add participant')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Add Participant</h2>
          <button className="close-button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="participantName">Name *</label>
            <input
              type="text"
              id="participantName"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter participant name"
              autoFocus
              disabled={isSubmitting}
            />
          </div>

          <div className="form-group">
            <label htmlFor="participantEmail">Email *</label>
            <input
              type="email"
              id="participantEmail"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="participant@example.com"
              disabled={isSubmitting}
            />
          </div>

          {error && <div className="error-text">{error}</div>}

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
              {isSubmitting ? 'Adding...' : 'Add Participant'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
