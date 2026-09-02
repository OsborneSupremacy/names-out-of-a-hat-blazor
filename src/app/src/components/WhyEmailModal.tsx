import { useEffect } from 'react'
// Shares the modal chrome with the other dialogs; see the note in EditNameModal.
import './CreateHatModal.css'
import './WhyEmailModal.css'

/**
 * Which address is being explained: the organizer's own, or the ones they are typing in for other
 * people. The question is different enough in each place to be worth answering differently — one is
 * "how do you know it's me", the other is "why do you need this about somebody else".
 */
export type WhyEmailContext = 'sign-in' | 'participants'

interface WhyEmailModalProps {
  context: WhyEmailContext
  onClose: () => void
}

/**
 * Why this application asks for an email address, in the two places it asks for one.
 *
 * Both answers end on the same question, because it is the one people actually have: they are not
 * really asking why not paper, they are asking why not a text message. That part is shared.
 */
export function WhyEmailModal({ context, onClose }: WhyEmailModalProps) {
  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }

    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [onClose])

  return (
    <div className="modal-overlay why-email-overlay" onClick={onClose}>
      <div
        className="modal-content why-email-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="why-email-title"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header">
          <h2 id="why-email-title">
            {context === 'sign-in' ? 'Why do we need your email?' : 'Why do we need their email?'}
          </h2>
          <button className="close-button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>

        <div className="why-email-body">
          {context === 'sign-in' ? <SignInAnswer /> : <ParticipantsAnswer />}
          <WhyNotAPhoneNumber />
        </div>

        <div className="why-email-actions">
          <button type="button" className="primary-button" onClick={onClose}>
            Got it
          </button>
        </div>
      </div>
    </div>
  )
}

function SignInAnswer() {
  return (
    <>
      <h3>It&rsquo;s how we know who you are</h3>
      <p>
        There is no password here. We send a link to your address, and opening it proves the inbox
        is yours. That is the whole of signing in.
      </p>

      <h3>It&rsquo;s also your return address</h3>
      <p>
        Names Out of a Hat sends mail for you — the invitations, the name each person drew, the
        announcement once it&rsquo;s over. We&rsquo;re glad to. But mail sent on your behalf has to
        say that it came from you. Imagine being told you are in a gift exchange with no way to tell
        who put you in it.
      </p>
    </>
  )
}

function ParticipantsAnswer() {
  return (
    <>
      <h3>It&rsquo;s how we tell them whose name they drew</h3>
      <p>
        Each participant gets their own message with their own name in it, and nobody sees anyone
        else&rsquo;s.
      </p>

      <h3>Email is a good way to do that</h3>
      <p>
        It is private: the name lands in one inbox rather than a group chat where everybody can read
        it. It is instant. It reaches people wherever they are without anyone installing an app or
        making an account. And it stays put, so they can look it up again in December once
        they&rsquo;ve forgotten — which people do.
      </p>
      <p>We are not going to mail them a letter or send a telegram.</p>
      <p>
        Participants&rsquo; addresses are used to run your gift exchange and for nothing else.
      </p>
    </>
  )
}

/**
 * The real question behind the other one.
 *
 * Deliberately not written as a legal summary. It says what stops us, in the order that actually
 * stops us — people dislike it, the platforms forbid it, and consent cannot be handed over by
 * somebody else — without stating any of it as a precise account of the law.
 */
function WhyNotAPhoneNumber() {
  return (
    <>
      <h3>Why not a phone number?</h3>
      <p>
        Would that feel better — your number, and your participants&rsquo;? We would genuinely like
        to use phone numbers. A text is how plenty of people would rather hear about this.
      </p>
      <p>
        We can&rsquo;t. Automated texts and recorded calls to people who did not ask for them are
        something most people quite reasonably resent, and messaging people who have not opted in is
        regulated: in the United States the person receiving them has to have agreed beforehand. The
        services that actually send texts hold senders to this as well, and want documented proof
        that each recipient opted in.
      </p>
      <p>
        And no, you can&rsquo;t agree on someone else&rsquo;s behalf. Permission has to come from
        the person holding the phone. That is the part that settles it: you know your participants,
        but you cannot opt them in.
      </p>
    </>
  )
}
