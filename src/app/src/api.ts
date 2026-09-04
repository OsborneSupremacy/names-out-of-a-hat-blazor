import { apiConfig } from './config'
import { getSession } from './auth'

export interface HatMetadata {
  hatId: string
  hatName: string
  status: string
}

export interface GetHatsResponse {
  organizerName: string
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

export interface EditParticipantAddressRequest {
  organizerEmail: string
  hatId: string
  currentEmail: string
  newEmail: string
}

export interface EditParticipantAddressResponse {
  /** False before invitations have gone out, when there is nothing to resend. */
  emailResent: boolean
  /** INVITATION, COMPLETION, or empty when nothing was sent. */
  messageType: string
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

export interface PreviewInvitationsRequest {
  organizerEmail: string
  hatId: string
}

export interface PreviewInvitationsResponse {
  subject: string
  htmlBody: string
  senderIpAddress: string
}

export interface UpdateProfileRequest {
  name: string
}

/** The categories the contact form offers. Mirrors FeedbackCategories.All on the server. */
export type FeedbackCategory = 'QUESTION' | 'FEATURE_REQUEST' | 'OTHER_FEEDBACK'

export interface SubmitFeedbackRequest {
  category: FeedbackCategory
  message: string
}

export interface CopyHatRequest {
  organizerEmail: string
  hatId: string
  newHatName: string
  excludePreviousRecipients: boolean
}

export interface CopyHatResponse {
  hatId: string
  /**
   * How many of the previous exchange's participants were not carried over, because they had asked
   * not to be added. A count and not names: the API does not say who, deliberately.
   */
  participantsOmitted: number
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
  /**
   * How far the last email sent to this participant is known to have got. Empty means nothing has
   * been heard, which is not the same as not delivered — see deliveryStatus.ts.
   */
  deliveryStatus: string
  /** Why, for the statuses that have a why. Written by a remote mail server, so never trusted as markup. */
  deliveryDetail: string
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
}

async function getAuthHeaders() {
  const session = getSession()

  if (!session) {
    throw new Error('Your session has expired. Please sign in again.')
  }

  return {
    'Authorization': `Bearer ${session.token}`,
    'Content-Type': 'application/json',
  }
}

async function handleApiError(response: Response, defaultMessage: string): Promise<never> {
  // Reading the body is best effort: some endpoints answer with a status and no body at all, and
  // API Gateway's own error responses are not always JSON. A parse failure must not become the
  // message the user sees, which is what the previous `throw e` in the catch produced.
  let message = ''

  try {
    const errorData = await response.json()

    if (typeof errorData?.message === 'string') {
      message = errorData.message
    }
  } catch {
    // Leave message empty and fall through to the default.
  }

  // The status is included because it is often the only clue when the body is empty.
  throw new Error(message || `${defaultMessage} (${response.status} ${response.statusText})`)
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

/**
 * Corrects the address a participant was invited at.
 *
 * Its own endpoint rather than part of editParticipant, which edits eligibility and resets the
 * exchange to IN_PROGRESS as a side effect — harmless before the draw, and destructive after
 * invitations have gone out, which is exactly when an address needs correcting.
 */
export async function editParticipantAddress(
  request: EditParticipantAddressRequest
): Promise<EditParticipantAddressResponse> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/participant/address`, {
    method: 'PUT',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to update the address')
  }

  return await response.json()
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

export async function previewInvitations(request: PreviewInvitationsRequest): Promise<PreviewInvitationsResponse> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat/${encodeURIComponent(request.organizerEmail)}/previewinvitations/${request.hatId}`, {
    method: 'GET',
    headers,
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to preview invitations')
  }

  return response.json()
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

export async function copyHat(request: CopyHatRequest): Promise<CopyHatResponse> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/hat/copy`, {
    method: 'POST',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to copy gift exchange')
  }

  return response.json()
}

export async function updateProfile(request: UpdateProfileRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/profile`, {
    method: 'PUT',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to update your name')
  }
}

export async function submitFeedback(request: SubmitFeedbackRequest): Promise<void> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${apiConfig.endpoint}/feedback`, {
    method: 'POST',
    headers,
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    await handleApiError(response, 'Failed to send your message')
  }
}
