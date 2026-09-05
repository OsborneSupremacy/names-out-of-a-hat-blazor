import { formatDeliveryMessageType, formatDeliveryStatus, showsDeliveryDetail } from './deliveryStatus'

describe('formatDeliveryStatus', () => {
  // The distinction the whole column turns on: nothing heard is not "not delivered".
  it('calls the empty status what it is', () => {
    expect(formatDeliveryStatus('')).toBe('No confirmation yet')
  })

  it('names the statuses SES reports', () => {
    expect(formatDeliveryStatus('DELIVERED')).toBe('Delivered')
    expect(formatDeliveryStatus('BOUNCED')).toBe('Bounced')
    expect(formatDeliveryStatus('COMPLAINED')).toBe('Marked as spam')
  })

  // A status this file has never heard of still has to render as something readable, since the
  // server's vocabulary can grow without the app being redeployed.
  it('falls back to title case for anything it does not know', () => {
    expect(formatDeliveryStatus('SOME_NEW_THING')).toBe('Some New Thing')
  })
})

describe('formatDeliveryMessageType', () => {
  it('names the two messages an ordinary exchange sends', () => {
    expect(formatDeliveryMessageType('INVITATION')).toBe('Invitation')
    expect(formatDeliveryMessageType('COMPLETION')).toBe('Announcement')
  })

  // Two tags for one event, told to the organizer and to everybody else. The split is ours; a
  // reader has no use for it.
  it('collapses the two leave notices into one name', () => {
    expect(formatDeliveryMessageType('PARTICIPANT_LEFT')).toBe('Someone-left notice')
    expect(formatDeliveryMessageType('ORGANIZER_LEFT_NOTE')).toBe('Someone-left notice')
  })

  // Empty is the signal to render nothing at all. UNSPECIFIED means a send reached SES without a
  // type tag, which is a bug here rather than a fact worth putting in front of an organizer — and
  // the empty string is what a participant nothing has been heard about carries.
  it('renders nothing for an untagged message or for silence', () => {
    expect(formatDeliveryMessageType('UNSPECIFIED')).toBe('')
    expect(formatDeliveryMessageType('')).toBe('')
  })
})

describe('showsDeliveryDetail', () => {
  it('shows the receiving server’s sentence only where an organizer can act on it', () => {
    expect(showsDeliveryDetail('BOUNCED', 'Permanent/General: 550 user unknown')).toBe(true)
    expect(showsDeliveryDetail('DELIVERED', 'anything')).toBe(false)
    expect(showsDeliveryDetail('BOUNCED', '')).toBe(false)
  })
})
