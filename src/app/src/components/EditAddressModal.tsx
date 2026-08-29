import { useState } from 'react'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './EditAddressModal.css'

/** Which email a correction will resend, or none when nothing has been sent yet. */
export type ResendKind = 'invitation' | 'announcement' | 'none'

interface EditAddressModalProps {
  participantName: string
  currentEmail: string
  resendKind: ResendKind
  isSaving: boolean
  error: string
  onCancel: () => void
  onConfirm: (newEmail: string) => Promise<void>
}

const RESEND_LABELS: Record<Exclude<ResendKind, 'none'>, string> = {
  invitation: 'their invitation, with the name they drew',
  announcement: 'the announcement that the exchange has finished',
}

/**
 * Correcting the address one participant was invited at.
 *
 * The friction here is the point, and it is aimed at one specific mistake: an organizer treating
 * this as a general-purpose edit and quietly re-sending people's invitations while tidying up.
 * Once invitations have gone out the correction always sends an email, so it is stated before the
 * fact and has to be acknowledged — the same bargain SendConfirmationModal makes, scaled to one
 * recipient rather than everybody.
 *
 * Before invitations go out nothing is sent, so there is nothing to acknowledge and the dialog says
 * so instead of inventing a warning.
 */
export function EditAddressModal({
  participantName,
  currentEmail,
  resendKind,
  isSaving,
  error,
  onCancel,
  onConfirm,
}: EditAddressModalProps) {
  const [newEmail, setNewEmail] = useState('')
  const [acknowledged, setAcknowledged] = useState(false)

  const willResend = resendKind !== 'none'
  const trimmed = newEmail.trim()
  const unchanged = trimmed.toLowerCase() === currentEmail.toLowerCase()
  const canSubmit =
    trimmed.length > 0 && !unchanged && (!willResend || acknowledged) && !isSaving

  return (
    <div className="modal-overlay" onClick={isSaving ? undefined : onCancel}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Edit {participantName}&rsquo;s address</h2>
        </div>

        <div className="edit-address-body">
          <p className="edit-address-current">
            Currently <strong>{currentEmail}</strong>
          </p>

          <label className="edit-address-label" htmlFor="new-address">
            New email address
          </label>
          <input
            id="new-address"
            type="email"
            className="edit-address-input"
            value={newEmail}
            onChange={(e) => setNewEmail(e.target.value)}
            placeholder="them@example.com"
            disabled={isSaving}
            autoFocus
          />

          {unchanged && trimmed.length > 0 && (
            <p className="edit-address-hint">That is already their address.</p>
          )}

          {willResend ? (
            <>
              <div className="edit-address-warning">
                <p>
                  Saving this will email <strong>{RESEND_LABELS[resendKind]}</strong> to the new
                  address straight away.
                </p>
                {/*
                  * Said plainly because the opposite is the natural assumption. Fixing the address
                  * changes where future email goes; it cannot retrieve what already arrived
                  * somewhere else, and an organizer who believes otherwise will not think to warn
                  * the person whose exchange was disclosed.
                  */}
                <p>
                  It does not recall anything sent to the old address. If that address reached a
                  real person, they have already seen it.
                </p>
              </div>

              <label className="edit-address-acknowledgement">
                <input
                  type="checkbox"
                  checked={acknowledged}
                  onChange={(e) => setAcknowledged(e.target.checked)}
                  disabled={isSaving}
                />
                <span>I understand an email will be sent now</span>
              </label>
            </>
          ) : (
            <p className="edit-address-hint">
              Nothing has been sent for this gift exchange yet, so changing this emails nobody.
            </p>
          )}

          {error && <p className="edit-address-error">{error}</p>}

          <div className="modal-actions">
            <button
              type="button"
              className="secondary-button"
              onClick={onCancel}
              disabled={isSaving}
            >
              Cancel
            </button>
            <button
              type="button"
              className="primary-button"
              onClick={() => onConfirm(trimmed)}
              disabled={!canSubmit}
            >
              {isSaving ? 'Saving...' : willResend ? 'Save & Resend' : 'Save'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
