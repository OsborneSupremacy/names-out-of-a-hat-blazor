/**
 * User-facing labels for what SES said happened to a participant's email.
 *
 * The wire values are the server's vocabulary and stay as they are; this is the only place the UI
 * decides what to call them, for the reason hatStatus.ts gives.
 *
 * The empty status is the one that matters most to get right. It means nothing has been heard yet
 * — which is the state for the first seconds after a send, and the permanent state for a message
 * whose events were lost — and it is emphatically not "this person did not receive it". An
 * organizer who reads it as the latter goes and pesters somebody who is holding their invitation,
 * so the label says what is true about our knowledge rather than about the participant.
 */
const DELIVERY_STATUS_LABELS: Record<string, string> = {
  '': 'No confirmation yet',
  SENT: 'Sent',
  DELAYED: 'Delayed, still trying',
  DELIVERED: 'Delivered',
  COMPLAINED: 'Marked as spam',
  REJECTED: 'Rejected',
  FAILED: 'Failed to send',
  BOUNCED: 'Bounced',
}

/**
 * Which statuses the organizer can actually do something about.
 *
 * Only these show the detail underneath. For the rest the detail is either empty or says nothing
 * an organizer would act on, and a row of explanations nobody needs would bury the one that
 * matters.
 */
const ACTIONABLE_STATUSES = ['BOUNCED', 'REJECTED', 'FAILED']

/** Grouped for colour: good, waiting, or wrong. */
export type DeliveryTone = 'good' | 'neutral' | 'bad'

export function deliveryTone(status: string): DeliveryTone {
  if (status === 'DELIVERED') return 'good'
  if (ACTIONABLE_STATUSES.includes(status) || status === 'COMPLAINED') return 'bad'
  return 'neutral'
}

export function formatDeliveryStatus(status: string): string {
  return (
    DELIVERY_STATUS_LABELS[status] ??
    status
      .split('_')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ')
  )
}

/**
 * Whether the reason underneath is worth showing.
 *
 * The detail itself is written by whatever mail server rejected the message — it passes through no
 * moderation on the way here, ours or anybody's. Rendering it as React text rather than as markup
 * is what keeps that safe.
 */
export function showsDeliveryDetail(status: string, detail: string): boolean {
  return detail.length > 0 && ACTIONABLE_STATUSES.includes(status)
}
