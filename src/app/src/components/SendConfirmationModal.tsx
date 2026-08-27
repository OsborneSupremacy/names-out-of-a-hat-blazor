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
          <h2>Before sending</h2>
        </div>

        <div className="send-confirmation-body">
          <p className="send-confirmation-lead">
            These invitations will go to {recipientCount}{' '}
            {recipientCount === 1 ? 'person' : 'people'}.
          </p>

          <div className="send-confirmation-terms">
            <p>By sending these invitations, I confirm that:</p>
            <ul>
              <li>
                They are sent by namesoutofahat.com on my behalf, and carry my name and email
                address.
              </li>
              <li>
                I am responsible for what they say — the gift exchange name, the participant names,
                and any additional information I have added.
              </li>
              {/*
                * The closing sentence has to agree with how many things it just named. Without an
                * IP there is only the address, and "both are recorded" would be counting something
                * the reader cannot see.
                */}
              <li>
                I am sending as <strong>{organizerEmail}</strong>, the address I verified when I
                signed in
                {senderIpAddress ? (
                  <>
                    , from <strong>{senderIpAddress}</strong>. Both are recorded.
                  </>
                ) : (
                  <>. It is recorded.</>
                )}
              </li>
              <li>
                I will not use this service to harass or deceive anyone, to conceal my identity, or
                for anything unlawful.
              </li>
              <li>
                I understand that namesoutofahat.com investigates reports of misuse, and may
                disclose these records to recipients or to law enforcement.
              </li>
            </ul>
          </div>

          <label className="send-confirmation-acknowledgement">
            <input
              type="checkbox"
              checked={acknowledged}
              onChange={(e) => setAcknowledged(e.target.checked)}
              disabled={isSending}
            />
            <span>I have read and agree to the above</span>
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
    </div>
  )
}
