import { useState } from 'react'
import { TermsModal } from './TermsModal'
import { ContactModal } from './ContactModal'
import './Footer.css'

export function Footer() {
  const currentYear = new Date().getFullYear()
  const [showTerms, setShowTerms] = useState(false)
  const [showContact, setShowContact] = useState(false)

  return (
    <>
      <footer className="app-footer">
        <div className="footer-content">
          {/* Buttons rather than anchors. Neither of these is a place — there is no URL to copy,
            * open in a tab, or bookmark — and an <a href="#"> that opens a dialog is a link that
            * lies to a screen reader and scrolls the page for anybody who middle-clicks it. */}
          <nav className="footer-links" aria-label="Site information">
            <button type="button" className="footer-link" onClick={() => setShowTerms(true)}>
              Terms &amp; Conditions
            </button>
            <span className="footer-separator" aria-hidden="true">
              ·
            </span>
            <button type="button" className="footer-link" onClick={() => setShowContact(true)}>
              Contact
            </button>
          </nav>

          <p>&copy; {currentYear} Names Out of a Hat. All rights reserved.</p>
        </div>
      </footer>

      {showTerms && <TermsModal onClose={() => setShowTerms(false)} />}
      {showContact && <ContactModal onClose={() => setShowContact(false)} />}
    </>
  )
}
