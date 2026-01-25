import { fetchAuthSession } from 'aws-amplify/auth'
import { apiConfig } from './aws-config'

export interface HatMetadata {
  hatId: string
  hatName: string
}

export interface GetHatsResponse {
  Hats: HatMetadata[]
}

async function getAuthHeaders() {
  const session = await fetchAuthSession()
  const token = session.tokens?.idToken?.toString()

  if (!token) {
    throw new Error('No authentication token available')
  }

  return {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  }
}

export async function getHats(email: string): Promise<GetHatsResponse> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hats/${encodeURIComponent(email)}`, {
    method: 'GET',
    headers,
  })

  if (!response.ok) {
    throw new Error(`Failed to fetch hats: ${response.statusText}`)
  }

  return response.json()
}
