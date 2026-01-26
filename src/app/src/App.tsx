import { Authenticator } from '@aws-amplify/ui-react'
import { fetchUserAttributes } from 'aws-amplify/auth'
import { useEffect, useState } from 'react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import '@aws-amplify/ui-react/styles.css'
import './App.css'
import { Home } from './pages/Home'
import { GiftExchangeDetail } from './pages/GiftExchangeDetail'

function AuthenticatedContent({ signOut }: { signOut: () => void }) {
  const [givenName, setGivenName] = useState<string>('')
  const [userEmail, setUserEmail] = useState<string>('')

  useEffect(() => {
    async function loadUserData() {
      try {
        const attributes = await fetchUserAttributes()
        setGivenName(attributes.given_name || '')
        setUserEmail(attributes.email || '')
      } catch (err) {
        console.error('Error loading user attributes:', err)
      }
    }

    loadUserData()
  }, [])

  if (!userEmail) {
    return <div>Loading...</div>
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/"
          element={
            <Home
              userEmail={userEmail}
              givenName={givenName}
              onSignOut={signOut}
            />
          }
        />
        <Route
          path="/gift-exchange/:hatId"
          element={
            <GiftExchangeDetail
              userEmail={userEmail}
              givenName={givenName}
              onSignOut={signOut}
            />
          }
        />
      </Routes>
    </BrowserRouter>
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
