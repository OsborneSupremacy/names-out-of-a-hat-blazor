/**
 * How many people one gift exchange may hold.
 *
 * Mirrors ParticipantLimit.MaxParticipants on the server, which is the authority: an add past the
 * limit is refused there with a message whatever this file says. This copy exists so a full
 * exchange stops offering the button rather than offering it and then explaining itself, and the
 * consequence of the two disagreeing is contained. A smaller number here retires the button early;
 * a larger one lets an organizer press it and read the server's refusal, which is what an organizer
 * with a stale page loaded gets anyway.
 */
export const MAX_PARTICIPANTS = 50
