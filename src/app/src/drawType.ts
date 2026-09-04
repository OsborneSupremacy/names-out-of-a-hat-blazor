/**
 * The shapes a draw can be asked to come out in.
 *
 * The summaries say what each option does in the terms the choice is actually made in — who can
 * end up drawing whom — rather than in the permutation language the server is written in. An
 * organizer picking between these does not need to know what a 2-cycle is to know whether they
 * want one, and the technical reading is in the README for whoever does.
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
}

export const DRAW_TYPE_OPTIONS: DrawTypeOption[] = [
  {
    value: 'ANYTHING_GOES',
    label: 'Anything goes',
    summary:
      'Any draw at all, as long as nobody draws themselves. Two people may end up drawing each other.',
  },
  {
    value: 'NO_MUTUAL_PAIRS',
    label: 'No mutual pairs',
    summary:
      'Nobody draws the person who drew them. Rules out the case where two people are effectively their own private gift exchange.',
  },
  {
    value: 'SINGLE_CYCLE',
    label: 'Single cycle',
    summary:
      'Everybody in one unbroken chain: A draws B, B draws C, and so on until the last person draws A. No separate little groups anywhere.',
  },
]

export function drawTypeLabel(drawType: DrawType): string {
  return DRAW_TYPE_OPTIONS.find((option) => option.value === drawType)?.label ?? drawType
}
