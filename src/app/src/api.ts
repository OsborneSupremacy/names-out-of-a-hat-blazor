import { fetchAuthSession } from 'aws-amplify/auth'
import { apiConfig } from './aws-config'

export interface HatMetadata {
  hatId: string
  hatName: string
  status: string
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

export interface DeleteHatRequest {
  organizerEmail: string
  hatId: string
}

export interface ValidateHatRequest {
  organizerEmail: string
  hatId: string
}

export interface ValidateHatResponse {
  success: boolean
  errors: string[]
}

export interface AssignRecipientsRequest {
  organizerEmail: string
  hatId: string
}

export interface SendInvitationsRequest {
  organizerEmail: string
  hatId: string
}

export interface CloseHatRequest {
  organizerEmail: string
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
  status: string
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

export async function getHat(email: string, hatId: string): Promise<Hat> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat/${encodeURIComponent(email)}/${hatId}`, {
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

export async function deleteHat(request: DeleteHatRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat`, {
    method: 'DELETE',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to delete gift exchange')
  }
}

export async function validateHat(request: ValidateHatRequest): Promise<ValidateHatResponse> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat/validate`, {
    method: 'POST',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to validate gift exchange')
  }

  return response.json()
}

export async function assignRecipients(request: AssignRecipientsRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/recipients`, {
    method: 'POST',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to assign recipients')
  }
}

export async function sendInvitations(request: SendInvitationsRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat/sendinvitations`, {
    method: 'POST',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to send invitations')
  }
}

export async function closeHat(request: CloseHatRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat/close`, {
    method: 'POST',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to close gift exchange')
  }
}
