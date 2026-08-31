import { useEffect, useState, FormEvent } from 'react'
import { submitFeedback, FeedbackCategory } from '../api'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './ContactModal.css'

const MAX_MESSAGE_LENGTH = 4000

/**
 * The categories, their labels, and the prompt each one puts above the box.
 *
 * The values mirror FeedbackCategories.All on the server, and the request schema API Gateway
 * validates against holds the same three. All three have to move together — a category added here
 * alone is rejected at the edge before the validator ever sees it.
 */
const CATEGORIES: ReadonlyArray<{ value: FeedbackCategory; label: string; prompt: string }> = [
  {
    value: 'QUESTION',
    label: 'Question',
    prompt: 'What would you like to know?',
  },
  {
    value: 'FEATURE_REQUEST',
    label: 'Feature request',
    prompt: 'What would you like it to do?',
  },
  {
    value: 'OTHER_FEEDBACK',
    label: 'Other feedback',
    prompt: 'What would you like to say?',
  },
]

interface ContactModalProps {
  onClose: () => void
}

/**
 * The contact form behind the footer's Contact link.
 *
 * There is no name or email field. The footer only renders on signed-in pages, so the server takes
 * the sender from the session — which is both one less thing to type and the reason this endpoint
 * needs no CAPTCHA: there is no anonymous route to it.
 */
export function ContactModal({ onClose }: ContactModalProps) {
  const [category, setCategory] = useState<FeedbackCategory>('QUESTION')
  const [message, setMessage] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      // Not while a send is in flight: the request cannot be recalled, and closing over it would
      // leave the sender unable to tell whether it went.
      if (event.key === 'Escape' && !isSending) onClose()
    }

    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [onClose, isSending])

  const trimmed = message.trim()
  const selected = CATEGORIES.find((entry) => entry.value === category)!

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    if (!trimmed) {
      setError('Please write a message first.')
      return
    }

    setError('')
    setIsSending(true)

    try {
      await submitFeedback({ category, message: trimmed })
      setSent(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send your message')
    } finally {
      setIsSending(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={isSending ? undefined : onClose}>
      <div
        className="modal-content"
        role="dialog"
        aria-modal="true"
        aria-labelledby="contact-title"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header">
          <h2 id="contact-title">{sent ? 'Thanks' : 'Get in touch'}</h2>
          <button
            className="close-button"
            onClick={onClose}
            aria-label="Close"
            disabled={isSending}
          >
            ×
          </button>
        </div>

        {sent ? (
          <div className="contact-sent">
            <p>Your message is on its way. If it needs a reply, it will come to the address you signed in with.</p>
            <div className="modal-actions">
              <button type="button" className="primary-button" onClick={onClose}>
                Done
              </button>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="feedback-category">What&rsquo;s this about?</label>
              <select
                id="feedback-category"
                className="contact-select"
                value={category}
                onChange={(e) => setCategory(e.target.value as FeedbackCategory)}
                disabled={isSending}
              >
                {CATEGORIES.map((entry) => (
                  <option key={entry.value} value={entry.value}>
                    {entry.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="feedback-message">{selected.prompt}</label>
              <textarea
                id="feedback-message"
                className="contact-textarea"
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                maxLength={MAX_MESSAGE_LENGTH}
                rows={7}
                disabled={isSending}
                autoFocus
              />
              {/* Only near the ceiling. A counter sitting under an empty box reads as a length
                * requirement, which is the opposite of what it is. */}
              {message.length > MAX_MESSAGE_LENGTH - 500 && (
                <div className="contact-counter">
                  {MAX_MESSAGE_LENGTH - message.length} characters left
                </div>
              )}
              {error && <div className="error-text">{error}</div>}
            </div>

            <p className="modal-note">
              Sent along with the email address you signed in with, so a reply can reach you.
            </p>

            <div className="modal-actions">
              <button
                type="button"
                className="secondary-button"
                onClick={onClose}
                disabled={isSending}
              >
                Cancel
              </button>
              <button type="submit" className="primary-button" disabled={isSending || !trimmed}>
                {isSending ? 'Sending...' : 'Send'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}
