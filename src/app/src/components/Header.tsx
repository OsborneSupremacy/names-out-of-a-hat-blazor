import { useState, useRef, useEffect } from 'react'
import './Header.css'

interface HeaderProps {
  userEmail: string
  givenName: string
  onSignOut: () => void
}

export function Header({ userEmail, givenName, onSignOut }: HeaderProps) {
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false)
      }
    }

    if (isMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isMenuOpen])

  return (
    <header className="app-header">
      <div className="header-content">
        <div className="app-title">
          <h1>Names Out of a Hat</h1>
        </div>

        <div className="profile-section" ref={menuRef}>
          <button
            className="profile-button"
            onClick={() => setIsMenuOpen(!isMenuOpen)}
            aria-label="Profile menu"
          >
            <div className="profile-icon">
              {givenName.charAt(0).toUpperCase() || userEmail.charAt(0).toUpperCase()}
            </div>
          </button>

          {isMenuOpen && (
            <div className="profile-menu">
              <div className="profile-menu-header">
                <div className="profile-name">{givenName || 'User'}</div>
                <div className="profile-email">{userEmail}</div>
              </div>
              <div className="profile-menu-divider"></div>
              <button
                className="profile-menu-item"
                onClick={() => {
                  setIsMenuOpen(false)
                  onSignOut()
                }}
              >
                Sign Out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
