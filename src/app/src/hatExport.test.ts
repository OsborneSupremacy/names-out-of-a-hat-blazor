import { exportFileName } from './hatExport'

describe('exportFileName', () => {
  it('names the file after the exchange and the day it was taken', () => {
    expect(exportFileName('Family Christmas 2026', '2026-09-04T11:22:33Z'))
      .toBe('family-christmas-2026-export-2026-09-04.json')
  })

  it('collapses punctuation rather than carrying it into a filename', () => {
    expect(exportFileName("Dave & Sue's — Draw!", '2026-09-04T00:00:00Z'))
      .toBe('dave-sue-s-draw-export-2026-09-04.json')
  })

  // A name made entirely of characters a filename cannot hold leaves nothing to slugify, and an
  // empty stem would produce a file called "-2026-09-04.json".
  it('falls back to a generic stem when there is nothing to slugify', () => {
    expect(exportFileName('🎁🎄', '2026-09-04T00:00:00Z'))
      .toBe('gift-exchange-export-2026-09-04.json')
  })

  it('falls back to today when the export carries no readable date', () => {
    const today = new Date().toISOString().slice(0, 10)

    expect(exportFileName('Office Draw', 'not a date')).toBe(`office-draw-export-${today}.json`)
  })
})
