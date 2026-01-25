import { Authenticator } from '@aws-amplify/ui-react'
import { fetchUserAttributes } from 'aws-amplify/auth'
import { useEffect, useState } from 'react'
import '@aws-amplify/ui-react/styles.css'
import './App.css'
import { getHats, createHat, HatMetadata } from './api'
import { Header } from './components/Header'
import { Footer } from './components/Footer'
import { CreateHatModal } from './components/CreateHatModal'

function AuthenticatedContent({ signOut }: { signOut: () => void }) {
  const [givenName, setGivenName] = useState<string>('')
  const [userEmail, setUserEmail] = useState<string>('')
  const [hats, setHats] = useState<HatMetadata[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [showCreateModal, setShowCreateModal] = useState(false)

  useEffect(() => {
    async function loadUserData() {
      try {
        const attributes = await fetchUserAttributes()
        setGivenName(attributes.given_name || '')
        setUserEmail(attributes.email || '')

        if (attributes.email) {
          const response = await getHats(attributes.email)
          setHats(response.hats)
        }
      } catch (err) {
        console.error('Error loading data:', err)
        setError('Failed to load your gift exchanges')
      } finally {
        setLoading(false)
      }
    }

    loadUserData()
  }, [])

  const handleCreateNew = () => {
    setShowCreateModal(true)
  }

  const handleCreateSubmit = async (hatName: string) => {
    const response = await createHat({
      hatName,
      organizerName: givenName,
      organizerEmail: userEmail,
    })

    // Refresh the hats list
    const updatedHats = await getHats(userEmail)
    setHats(updatedHats.hats)
  }

  return (
    <div className="app-container">
      <Header
        userEmail={userEmail}
        givenName={givenName}
        onSignOut={signOut}
      />

      <main className="main-content">
        <div className="content-wrapper">
          <h2>Hello {givenName || 'there'}!</h2>
          <p>Welcome to Names Out of a Hat!</p>

          {loading ? (
            <p>Loading your gift exchanges...</p>
          ) : error ? (
            <p className="error-message">{error}</p>
          ) : (
            <>
              {hats.length > 0 ? (
                <div className="gift-exchanges-section">
                  <div className="section-header">
                    <h3>Your Gift Exchanges</h3>
                    <button className="primary-button" onClick={handleCreateNew}>
                      Create New Gift Exchange
                    </button>
                  </div>
                  <ul className="gift-exchanges-list">
                    {hats.map((hat) => (
                      <li key={hat.hatId} className="gift-exchange-item">
                        <strong>{hat.hatName}</strong>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : (
                <div className="empty-state">
                  <p>You don't have any Gift Exchanges</p>
                  <button className="primary-button" onClick={handleCreateNew}>
                    Create a Gift Exchange
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </main>

      <Footer />

      {showCreateModal && (
        <CreateHatModal
          organizerName={givenName}
          organizerEmail={userEmail}
          onClose={() => setShowCreateModal(false)}
          onSubmit={handleCreateSubmit}
        />
      )}
    </div>
  )
}

function App() {
  return (
    <Authenticator
      loginMechanisms={['email']}
      signUpAttributes={['given_name']}
      formFields={{
        signUp: {
          given_name: {
            order: 1,
          },
          email: {
            order: 2,
          },
          password: {
            order: 3,
          },
          confirm_password: {
            order: 4,
          },
        },
      }}
    >
      {({ signOut }) => <AuthenticatedContent signOut={signOut} />}
    </Authenticator>
  )
}

export default App
