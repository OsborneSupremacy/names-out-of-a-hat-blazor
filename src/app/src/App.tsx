import { Authenticator } from '@aws-amplify/ui-react'
import { fetchUserAttributes } from 'aws-amplify/auth'
import { useEffect, useState } from 'react'
import '@aws-amplify/ui-react/styles.css'
import './App.css'
import { getHats, HatMetadata } from './api'

function AuthenticatedContent({ signOut }: { signOut: () => void }) {
  const [givenName, setGivenName] = useState<string>('')
  const [userEmail, setUserEmail] = useState<string>('')
  const [hats, setHats] = useState<HatMetadata[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')

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
    // TODO: Navigate to create gift exchange page
    alert('Create new gift exchange - TODO')
  }

  return (
    <main>
      <h1>Hello {givenName || 'there'}!</h1>
      <p>Welcome to Names Out of a Hat!</p>

      {loading ? (
        <p>Loading your gift exchanges...</p>
      ) : error ? (
        <p style={{ color: 'red' }}>{error}</p>
      ) : hats.length > 0 ? (
        <div>
          <h2>Your Gift Exchanges</h2>
          <ul>
            {hats.map((hat) => (
              <li key={hat.hatId}>
                <strong>{hat.hatName}</strong>
              </li>
            ))}
          </ul>
        </div>
      ) : (
        <div>
          <p>You don't have any Gift Exchanges</p>
          <button onClick={handleCreateNew}>Create a new Gift Exchange</button>
        </div>
      )}

      <button onClick={signOut}>Sign out</button>
    </main>
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
