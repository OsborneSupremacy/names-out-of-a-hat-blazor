const MINUTE = 60
const HOUR = 60 * MINUTE
const DAY = 24 * HOUR
const WEEK = 7 * DAY
const MONTH = 30 * DAY
const YEAR = 365 * DAY

// Numeric throughout, so every phrase means exactly the arithmetic behind it. The shorthands the
// 'auto' mode reaches for are calendar words, and the elapsed time is not a calendar: at one in the
// morning, "yesterday" and "last week" are both off by a day for a stamp that is 24 hours or 7 days
// old. "1 day ago" is never wrong.
const relative = new Intl.RelativeTimeFormat(undefined, { numeric: 'always' })

// The one phrase worth borrowing from the other mode. Formatting zero numerically gives "in 0
// seconds"; asking 'auto' for it gives "now", which is what belongs under a pill that has just
// changed — and it comes from Intl rather than being an English string this file made up.
const atThisMoment = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' }).format(0, 'second')

/**
 * How long ago a timestamp was, in words.
 *
 * Intl does the phrasing rather than a table of strings here, so plurals come out right in
 * whatever locale the browser is in without this file having an opinion about English.
 *
 * The unit is chosen by how old the timestamp is: a list is scanned rather than read, and "112
 * days ago" takes longer to place than "4 months ago" does. Precision beyond that is what the
 * tooltip is for.
 *
 * Returns the empty string for anything it cannot phrase, which is the signal to render nothing.
 * The minimum date is one of those: the API spells "no date" with it rather than with null, and
 * "2025 years ago" is not what an organizer should be shown when a timestamp is missing.
 */
export function formatRelativeTime(timestamp: string, now: Date = new Date()): string {
  const at = new Date(timestamp)

  if (Number.isNaN(at.getTime())) return ''

  if (at.getUTCFullYear() <= 1) return ''

  // Clamped at zero: a stamp a few seconds into the future is the two clocks disagreeing, not
  // something that has yet to happen, and "in 3 seconds" would read as a bug.
  const elapsed = Math.max(0, Math.round((now.getTime() - at.getTime()) / 1000))

  if (elapsed < MINUTE) return atThisMoment
  if (elapsed < HOUR) return relative.format(-Math.floor(elapsed / MINUTE), 'minute')
  if (elapsed < DAY) return relative.format(-Math.floor(elapsed / HOUR), 'hour')
  if (elapsed < WEEK) return relative.format(-Math.floor(elapsed / DAY), 'day')
  if (elapsed < MONTH) return relative.format(-Math.floor(elapsed / WEEK), 'week')
  if (elapsed < YEAR) return relative.format(-Math.floor(elapsed / MONTH), 'month')

  return relative.format(-Math.floor(elapsed / YEAR), 'year')
}

/**
 * The same moment written out in full, for the tooltip behind the phrase above. Empty for the same
 * inputs, so a caller can hang both off one check.
 */
export function formatAbsoluteTime(timestamp: string): string {
  const at = new Date(timestamp)

  if (Number.isNaN(at.getTime())) return ''

  if (at.getUTCFullYear() <= 1) return ''

  return at.toLocaleString(undefined, { dateStyle: 'long', timeStyle: 'short' })
}
