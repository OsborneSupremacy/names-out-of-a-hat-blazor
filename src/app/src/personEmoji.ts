/**
 * The faces a participant can be marked with.
 *
 * Mirrors PersonEmoji.All on the server, which is the authority: an edit naming anything else is
 * refused by EditParticipantEmojiRequestValidator before it reaches the database. This copy exists
 * so the picker has something to offer without a round trip for a list that changes about as often
 * as the logo does — and the consequence of the two disagreeing is contained. A face here that the
 * server does not know is refused with a message; one the server knows and this list omits is
 * simply never offered, and still renders wherever a participant already wears it, because the list
 * below is what the picker shows rather than what the page is willing to display.
 */
export const PERSON_EMOJI = [
  // Grins and laughs
  '😀',
  '😃',
  '😄',
  '😁',
  '😆',
  '😅',
  '🤣',
  '😂',

  // Smiles and warmth
  '🙂',
  '🙃',
  '😉',
  '😊',
  '😌',
  '😇',
  '🥰',
  '😍',
  '🤩',

  // Playful
  '😋',
  '😛',
  '😜',
  '🤪',
  '😝',
  '🤗',
  '🤭',
  '🤫',
  '😏',

  // Costumes and characters
  '🤠',
  '🥳',
  '😎',
  '🤖',
  '👽',
  '👾',
  '👻',

  // Cats
  '😺',
  '😸',
  '😹',
  '😻',
  '😼',

  // Sun and moon
  '🌝',
  '🌞',
  '🌛',
  '🌜',
] as const
