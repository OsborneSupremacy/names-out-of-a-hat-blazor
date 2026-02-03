import { fetchAuthSession } from 'aws-amplify/auth'
import { apiConfig } from './aws-config'

export interface HatMetadata {
  hatId: string
  hatName: string
  invitationsQueued: boolean
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

export interface EditHatRequest {
  hatId: string
  organizerEmail: string
  name: string
  additionalInformation: string
  priceRange: string
}

export interface AddParticipantRequest {
  organizerEmail: string
  hatId: string
  name: string
  email: string
}

export interface RemoveParticipantRequest {
  organizerEmail: string
  hatId: string
  email: string
}

export interface EditParticipantRequest {
  organizerEmail: string
  hatId: string
  email: string
  eligibleRecipients: string[]
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

async function handleApiError(response: Response, defaultMessage: string): Promise<never> {
  try {
    const errorData = await response.json()
    if (errorData.message) {
      throw new Error(errorData.message)
    }
  } catch (e) {
    // If parsing fails or no message field, fall through to default
    if (e instanceof Error && e.message !== defaultMessage) {
      throw e
    }
  }
  throw new Error(`${defaultMessage}: ${response.statusText}`)
}

export async function getHats(email: string): Promise<GetHatsResponse> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hats/${encodeURIComponent(email)}`, {
    method: 'GET',
    headers,
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to fetch hats')
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
    await handleApiError(response, 'Failed to create gift exchange')
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
    await handleApiError(response, 'Failed to fetch gift exchange')
  }

  return response.json()
}

export async function editHat(request: EditHatRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat`, {
    method: 'PUT',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to edit gift exchange')
  }
}

export async function addParticipant(request: AddParticipantRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/participant`, {
    method: 'POST',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to add participant')
  }
}

export async function removeParticipant(request: RemoveParticipantRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/participant`, {
    method: 'DELETE',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to remove participant')
  }
}

export async function editParticipant(request: EditParticipantRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/participant`, {
    method: 'PUT',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to edit participant')
  }
}
