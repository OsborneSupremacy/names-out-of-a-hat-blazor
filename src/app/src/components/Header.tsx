import { useState, useRef, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { updateProfile } from '../api'
import { EditNameModal } from './EditNameModal'
import './Header.css'

interface HeaderProps {
  userEmail: string
  /**
   * null while the name is still being loaded, '' when the user genuinely has no name yet.
   *
   * The distinction matters: falling back to the email initial while loading is not a placeholder,
   * it is a different answer, and the UI visibly corrects itself once the real name arrives.
   */
  givenName: string | null
  onSignOut: () => void
  onNameUpdated: (name: string) => void
}

export function Header({ userEmail, givenName, onSignOut, onNameUpdated }: HeaderProps) {
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [showEditName, setShowEditName] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  const handleNameSubmit = async (name: string) => {
    await updateProfile({ name })
    onNameUpdated(name)
  }

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
          <h1>
            {/*
              A router Link rather than an <a href="/">: a plain anchor reloads the whole bundle
              and throws away the session state held in App, which is the difference between
              returning home and starting the app again.

              It stays a link on the home page itself, where it goes nowhere. Making it conditional
              would mean the one element every page shares is sometimes focusable and sometimes not,
              and somebody tabbing through would find it missing without being told why.
            */}
            <Link to="/" className="app-logo-link">
              <img
                className="app-logo"
                src="/logo-horizontal.png"
                alt="Names Out of a Hat"
                width={960}
                height={323}
              />
            </Link>
          </h1>
        </div>

        <div className="profile-section" ref={menuRef}>
          <button
            className="profile-button"
            onClick={() => setIsMenuOpen(!isMenuOpen)}
            aria-label="Profile menu"
          >
            <div className="profile-icon">
              {givenName === null
                ? '\u00A0'
                : (givenName.charAt(0) || userEmail.charAt(0)).toUpperCase()}
            </div>
          </button>

          {isMenuOpen && (
            <div className="profile-menu">
              <div className="profile-menu-header">
                {givenName !== null && <div className="profile-name">{givenName || 'User'}</div>}
                <div className="profile-email">{userEmail}</div>
              </div>
              <div className="profile-menu-divider"></div>
              <button
                className="profile-menu-item"
                onClick={() => {
                  setIsMenuOpen(false)
                  setShowEditName(true)
                }}
              >
                Edit Name
              </button>
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

      {showEditName && (
        <EditNameModal
          currentName={givenName ?? ''}
          onClose={() => setShowEditName(false)}
          onSubmit={handleNameSubmit}
        />
      )}
    </header>
  )
}
