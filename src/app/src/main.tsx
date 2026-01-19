import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Amplify } from 'aws-amplify'
import { amplifyConfig } from './aws-config'
import './index.css'
import App from './App.tsx'

// Configure Amplify
Amplify.configure(amplifyConfig)

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
