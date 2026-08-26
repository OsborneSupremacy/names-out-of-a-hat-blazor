import { useState, useRef, useEffect } from 'react'
import { updateProfile } from '../api'
import { EditNameModal } from './EditNameModal'
import './Header.css'

interface HeaderProps {
  userEmail: string
  givenName: string
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
          currentName={givenName}
          onClose={() => setShowEditName(false)}
          onSubmit={handleNameSubmit}
        />
      )}
    </header>
  )
}
