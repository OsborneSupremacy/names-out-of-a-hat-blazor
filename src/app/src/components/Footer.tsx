import './Footer.css'

export function Footer() {
  const currentYear = new Date().getFullYear()

  return (
    <footer className="app-footer">
      <div className="footer-content">
        <p>&copy; {currentYear} Names Out of a Hat. All rights reserved.</p>
      </div>
    </footer>
  )
}
