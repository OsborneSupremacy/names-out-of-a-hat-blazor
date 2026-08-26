/**
 * User-facing labels for hat statuses.
 *
 * The wire values are the server's vocabulary and stay as they are. These are the only place the
 * UI decides what to call them — the list page and the detail page previously formatted the same
 * status two different ways, so READY_TO_CLOSE appeared as "Ready To Close" in one place and
 * "Cooling Off" in the other.
 *
 * The end of a gift exchange is framed as revealing the picks rather than closing anything,
 * because that is what the organizer is actually waiting for, and it makes the wait explain
 * itself: the picks stay hidden until the exchange has had time to happen.
 */
const HAT_STATUS_LABELS: Record<string, string> = {
  IN_PROGRESS: 'In Progress',
  READY_FOR_ASSIGNMENT: 'Ready For Assignment',
  NAMES_ASSIGNED: 'Names Assigned',
  INVITATIONS_SENT: 'Invitations Sent',
  READY_TO_CLOSE: 'Ready to Reveal',
  CLOSED: 'Revealed',
}

/** The statuses a gift exchange moves through, in order, for the progress indicator. */
export const HAT_STATUS_STEPS = [
  'IN_PROGRESS',
  'READY_FOR_ASSIGNMENT',
  'NAMES_ASSIGNED',
  'INVITATIONS_SENT',
  'READY_TO_CLOSE',
  'CLOSED',
] as const

export function formatHatStatus(status: string): string {
  // An unknown status still renders readably rather than shouting SNAKE_CASE at somebody.
  return (
    HAT_STATUS_LABELS[status] ??
    status
      .split('_')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ')
  )
}
