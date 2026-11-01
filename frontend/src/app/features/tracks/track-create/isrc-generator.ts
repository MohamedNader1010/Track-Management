/**
 * Builds a random, well-formed ISRC: CC-XXX-YY-NNNNN
 *   CC    country of the registrant (we use the artist's country, e.g. EG)
 *   XXX   registrant code (fixed "TKW" for Takwene in this demo)
 *   YY    last two digits of the reference year (taken from the release date)
 *   NNNNN designation code — random here; a real label assigns these sequentially.
 *
 * Uniqueness is still enforced by the API (409 → shown on the ISRC field), so a rare collision is harmless.
 */
export const DEFAULT_REGISTRANT = 'TKW';

export function generateIsrc(options: { countryCode?: string; releaseDate?: string; registrant?: string } = {}): string {
  const country = /^[A-Za-z]{2}$/.test(options.countryCode ?? '') ? options.countryCode!.toUpperCase() : 'EG';
  const registrant = (options.registrant ?? DEFAULT_REGISTRANT).toUpperCase();
  const year = yearOf(options.releaseDate);
  const designation = randomDigits(5);

  return `${country}-${registrant}-${year}-${designation}`;
}

function yearOf(releaseDate: string | undefined): string {
  const year = releaseDate && /^\d{4}/.test(releaseDate) ? Number(releaseDate.slice(0, 4)) : new Date().getFullYear();
  return String(year % 100).padStart(2, '0');
}

function randomDigits(count: number): string {
  const values = crypto.getRandomValues(new Uint32Array(count));
  return Array.from(values, (v) => String(v % 10)).join('');
}
