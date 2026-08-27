import { useState } from 'react'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './SendConfirmationModal.css'

interface SendConfirmationModalProps {
  organizerEmail: string
  senderIpAddress: string
  recipientCount: number
  isSending: boolean
  onCancel: () => void
  onConfirm: () => Promise<void>
}

/**
 * Last step before anything is emailed on the organizer's behalf.
 *
 * It exists to make the send feel attributable rather than anonymous: the acknowledgment has to be
 * ticked deliberately, so this is not a dialog anyone dismisses by reflex on the way to a button.
 */
export function SendConfirmationModal({
  organizerEmail,
  senderIpAddress,
  recipientCount,
  isSending,
  onCancel,
  onConfirm,
}: SendConfirmationModalProps) {
  const [acknowledged, setAcknowledged] = useState(false)

  return (
    <div className="modal-overlay" onClick={isSending ? undefined : onCancel}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Before you send</h2>
        </div>

        <p className="send-confirmation-lead">
          You're about to email {recipientCount} {recipientCount === 1 ? 'person' : 'people'}.
        </p>

        <div className="send-confirmation-terms">
          <p>
            <strong>You are identified.</strong> This gift exchange is linked to{' '}
            <strong>{organizerEmail}</strong>, verified when you signed in
            {senderIpAddress && (
              <>
                , and this request is coming from <strong>{senderIpAddress}</strong>
              </>
            )}
            . Both are recorded.
          </p>
          <p>
            <strong>You are responsible for the content.</strong> The gift exchange name,
            participant names and anything you added appear in every invitation, sent under your
            name and address.
          </p>
          <p>
            Do not use this to harass, deceive, conceal who you are, or for any unlawful purpose.
            Reports are investigated, and records including your email and IP address may be given
            to recipients or to law enforcement.
          </p>
        </div>

        <label className="send-confirmation-acknowledgement">
          <input
            type="checkbox"
            checked={acknowledged}
            onChange={(e) => setAcknowledged(e.target.checked)}
            disabled={isSending}
          />
          <span>I have read this and I am sending to people who know me</span>
        </label>

        <div className="modal-actions">
          <button
            type="button"
            className="secondary-button"
            onClick={onCancel}
            disabled={isSending}
          >
            Cancel
          </button>
          <button
            type="button"
            className="primary-button"
            onClick={onConfirm}
            disabled={!acknowledged || isSending}
          >
            {isSending
              ? 'Sending...'
              : `Send ${recipientCount} ${recipientCount === 1 ? 'Invitation' : 'Invitations'}`}
          </button>
        </div>
      </div>
    </div>
  )
}
