import { Authenticator } from '@aws-amplify/ui-react'
import { fetchUserAttributes } from 'aws-amplify/auth'
import { useEffect, useState } from 'react'
import '@aws-amplify/ui-react/styles.css'
import './App.css'

function AuthenticatedContent({ signOut }: { signOut: () => void }) {
  const [givenName, setGivenName] = useState<string>('')

  useEffect(() => {
    fetchUserAttributes().then((attributes) => {
      setGivenName(attributes.given_name || '')
    })
  }, [])

  return (
    <main>
      <h1>Hello {givenName || 'there'}!</h1>
      <p>Welcome to Names Out of a Hat!</p>
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
