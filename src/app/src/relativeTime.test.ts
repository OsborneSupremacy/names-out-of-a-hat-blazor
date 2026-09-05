import { formatRelativeTime, formatAbsoluteTime, formatDateAndTime } from './relativeTime'

const NOW = new Date('2026-09-04T12:00:00Z')

const agoBy = (seconds: number) => new Date(NOW.getTime() - seconds * 1000).toISOString()

describe('formatRelativeTime', () => {
  it('says now for anything within the last minute', () => {
    expect(formatRelativeTime(agoBy(0), NOW)).toBe('now')
    expect(formatRelativeTime(agoBy(59), NOW)).toBe('now')
  })

  it('counts in minutes, then hours, then days', () => {
    expect(formatRelativeTime(agoBy(90), NOW)).toBe('1 minute ago')
    expect(formatRelativeTime(agoBy(45 * 60), NOW)).toBe('45 minutes ago')
    expect(formatRelativeTime(agoBy(3 * 60 * 60), NOW)).toBe('3 hours ago')
    expect(formatRelativeTime(agoBy(3 * 24 * 60 * 60), NOW)).toBe('3 days ago')
  })

  // The calendar shorthands are deliberately not used: at one in the morning "yesterday" is off by
  // a day for a stamp that is 24 hours old, and the elapsed time is not a calendar.
  it('counts a single day rather than calling it yesterday', () => {
    expect(formatRelativeTime(agoBy(24 * 60 * 60), NOW)).toBe('1 day ago')
  })

  it('coarsens as the timestamp gets older, so a list stays scannable', () => {
    expect(formatRelativeTime(agoBy(10 * 24 * 60 * 60), NOW)).toBe('1 week ago')
    expect(formatRelativeTime(agoBy(100 * 24 * 60 * 60), NOW)).toBe('3 months ago')
    expect(formatRelativeTime(agoBy(800 * 24 * 60 * 60), NOW)).toBe('2 years ago')
  })

  // Two clocks disagreeing, not something that has yet to happen.
  it('reads a timestamp slightly in the future as now', () => {
    expect(formatRelativeTime(agoBy(-30), NOW)).toBe('now')
  })

  // The API spells "no date" with the minimum date rather than with null, and "2025 years ago" is
  // not what an organizer should be shown when a timestamp is missing.
  it('renders nothing for the minimum date', () => {
    expect(formatRelativeTime('0001-01-01T00:00:00+00:00', NOW)).toBe('')
  })

  it('renders nothing for a timestamp it cannot read', () => {
    expect(formatRelativeTime('not a date', NOW)).toBe('')
    expect(formatRelativeTime('', NOW)).toBe('')
  })
})

describe('formatAbsoluteTime', () => {
  it('writes the moment out in full for the tooltip', () => {
    expect(formatAbsoluteTime('2026-09-04T12:00:00Z')).not.toBe('')
  })

  // Empty for the same inputs, so a caller can hang the phrase and its tooltip off one check.
  it('renders nothing wherever the relative phrase does not', () => {
    expect(formatAbsoluteTime('0001-01-01T00:00:00+00:00')).toBe('')
    expect(formatAbsoluteTime('not a date')).toBe('')
  })
})

describe('formatDateAndTime', () => {
  // The delivery column reads this out to somebody searching a mailbox, so the month has to be
  // named rather than numbered: 9/5 is the fifth of September or the ninth of May depending on
  // where the reader lives, and they are looking for one particular morning.
  it('names the month, and gives a time to go with it', () => {
    const formatted = formatDateAndTime('2026-09-04T12:00:00Z')

    expect(formatted).not.toBe('')
    expect(formatted).toMatch(/[A-Za-z]{3}/)
    expect(formatted).toMatch(/\d{4}/)
    expect(formatted).toMatch(/\d{1,2}:\d{2}/)
  })

  // Empty for the same inputs as the other two, so a caller can hang all of them off one check.
  it('renders nothing wherever the relative phrase does not', () => {
    expect(formatDateAndTime('0001-01-01T00:00:00+00:00')).toBe('')
    expect(formatDateAndTime('not a date')).toBe('')
    expect(formatDateAndTime('')).toBe('')
  })
})
