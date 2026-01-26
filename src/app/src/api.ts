import { fetchAuthSession } from 'aws-amplify/auth'
import { apiConfig } from './aws-config'

export interface HatMetadata {
  hatId: string
  hatName: string
}

export interface GetHatsResponse {
  hats: HatMetadata[]
}

export interface CreateHatRequest {
  hatName: string
  organizerName: string
  organizerEmail: string
}

export interface CreateHatResponse {
  hatId: string
}

export interface Participant {
  person: {
    name: string
    email: string
  }
  pickedRecipient: string
  eligibleRecipients: string[]
}

export interface Hat {
  id: string
  name: string
  additionalInformation: string
  priceRange: string
  organizer: {
    name: string
    email: string
  }
  participants: Participant[]
  organizerVerified: boolean
  recipientsAssigned: boolean
  invitationsQueued: boolean
  invitationsQueuedDate: string
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

export async function createHat(request: CreateHatRequest): Promise<CreateHatResponse> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat`, {
    method: 'POST',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to create gift exchange: ${response.statusText}`)
  }

  return response.json()
}

export async function getHat(email: string, hatId: string, showPickedRecipients: boolean = false): Promise<Hat> {
  const headers = await getAuthHeaders()

  const url = new URL(`${apiConfig.endpoint}/hat/${encodeURIComponent(email)}/${hatId}`)
  url.searchParams.set('showpickedrecipients', showPickedRecipients.toString())

  const response = await fetch(url.toString(), {
    method: 'GET',
    headers,
  })

  if (!response.ok) {
    throw new Error(`Failed to fetch gift exchange: ${response.statusText}`)
  }

  return response.json()
}
