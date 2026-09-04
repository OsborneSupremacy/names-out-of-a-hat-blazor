/**
 * The shapes a draw can be asked to come out in.
 *
 * Two vocabularies, deliberately. `label` and `summary` are what the organizer chooses between and
 * are written to be understood without knowing anything; `technical` is what the popover reveals
 * for the organizer who wants to know exactly what they are asking for, in the language the server
 * is actually written in. Neither is a simplification of the other — they are the same rule stated
 * at two altitudes, and the mapping between them is the point of showing both.
 *
 * The wire values are the server's vocabulary and stay as they are, for the reason hatStatus.ts
 * gives. They mirror GiftExchange.Library.Models.DrawTypes.All, and adding one here is not enough
 * on its own — the schema enum and the DrawType class are the other two places.
 */
export type DrawType = 'ANYTHING_GOES' | 'NO_MUTUAL_PAIRS' | 'SINGLE_CYCLE'

/** What the dialog opens on: the rule most groups have always played by, and the least likely to fail. */
export const DEFAULT_DRAW_TYPE: DrawType = 'ANYTHING_GOES'

export interface DrawTypeOption {
  value: DrawType
  label: string
  summary: string
  technical: string
}

export const DRAW_TYPE_OPTIONS: DrawTypeOption[] = [
  {
    value: 'ANYTHING_GOES',
    label: 'Anything goes',
    summary:
      'Any draw at all, as long as nobody draws themselves. Two people may end up drawing each other.',
    technical:
      'Any derangement of the participants — a permutation with no fixed point. The draw decomposes into cycles of whatever lengths happen to fall out, and cycles of length two (two people who drew each other) are permitted. This is the least constrained option, and the one least likely to fail.',
  },
  {
    value: 'NO_MUTUAL_PAIRS',
    label: 'No mutual pairs',
    summary:
      'Nobody draws the person who drew them. Rules out the case where two people are effectively their own private gift exchange.',
    technical:
      'A derangement containing no 2-cycle. Every cycle in the result is three people or longer, so no pair is closed off from the rest of the group. Enforced while the draw is built rather than checked afterwards, so it holds every time rather than usually.',
  },
  {
    value: 'SINGLE_CYCLE',
    label: 'Single cycle',
    summary:
      'Everybody in one unbroken chain: A draws B, B draws C, and so on until the last person draws A. No separate little groups anywhere.',
    technical:
      'A cyclic permutation — one cycle of length n, equivalently a Hamiltonian cycle in the graph of who is allowed to draw whom. The strictest of the three, and it implies no mutual pairs. Worth knowing: in a very small exchange a single cycle leaks information, because with few enough people a participant can narrow down the rest of the draw from their own name alone.',
  },
]

export function drawTypeLabel(drawType: DrawType): string {
  return DRAW_TYPE_OPTIONS.find((option) => option.value === drawType)?.label ?? drawType
}
