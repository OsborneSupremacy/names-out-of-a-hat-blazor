import { Authenticator } from '@aws-amplify/ui-react'
import '@aws-amplify/ui-react/styles.css'
import './App.css'

function App() {
  return (
    <Authenticator
      loginMechanisms={['email']}
      signUpAttributes={['given_name']}
    >
      {({ signOut, user }) => (
        <main>
          <h1>Hello {user?.signInDetails?.loginId}</h1>
          <p>Welcome to Names Out of a Hat!</p>
          <button onClick={signOut}>Sign out</button>
        </main>
      )}
    </Authenticator>
  )
}

export default App
