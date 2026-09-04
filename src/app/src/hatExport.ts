import { ExportHatResponse } from './api'

/**
 * What an exported gift exchange is called once it lands in somebody's downloads folder.
 *
 * The name and the date both, because that folder is where these go to be forgotten: a file called
 * "export.json" is indistinguishable from every other export a month later, and one carrying only
 * the exchange's name is indistinguishable from the copy taken before the last shake.
 */
export function exportFileName(hatName: string, exportedAt: string): string {
  const slug = hatName
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')

  // A name made entirely of characters a filename cannot hold — emoji, say — leaves nothing to
  // slugify, and an empty stem would produce a file called "-2026-09-04.json".
  const stem = slug || 'gift-exchange'

  const taken = new Date(exportedAt)
  const date = Number.isNaN(taken.getTime())
    ? new Date().toISOString().slice(0, 10)
    : taken.toISOString().slice(0, 10)

  return `${stem}-export-${date}.json`
}

/**
 * Hands the export to the browser as a file.
 *
 * A blob and a synthetic click rather than a link the organizer follows: the export is fetched with
 * the session token in an Authorization header, which an anchor pointing at the endpoint could not
 * carry. The object URL is revoked afterwards, since it pins the blob in memory until it is.
 */
export function downloadExport(exported: ExportHatResponse): void {
  const blob = new Blob([JSON.stringify(exported, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)

  const link = document.createElement('a')
  link.href = url
  link.download = exportFileName(exported.hat.name, exported.exportedAt)

  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)

  URL.revokeObjectURL(url)
}
