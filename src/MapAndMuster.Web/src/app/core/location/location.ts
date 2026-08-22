import { COUNTRIES, COUNTRY_CODES, REGIONS_BY_COUNTRY_CODE } from './location-catalog';

export function listCountries(): readonly string[] {
  return COUNTRIES;
}

export function regionsForCountry(country: string): readonly string[] {
  const code = COUNTRY_CODES[country];
  if (!code) {
    return [];
  }

  return REGIONS_BY_COUNTRY_CODE[code] ?? [];
}

export function listTimeZones(): readonly string[] {
  const supported =
    typeof Intl !== 'undefined' && typeof Intl.supportedValuesOf === 'function'
      ? Intl.supportedValuesOf('timeZone')
      : [];
  const named = supported.filter((zone) => zone !== 'UTC');
  return ['UTC', ...named];
}

export function formatLocation(
  city: string | null | undefined,
  region: string | null | undefined,
  country: string | null | undefined,
): string | null {
  const parts = [city, region, country].map((part) => part?.trim() ?? '').filter((part) => part.length > 0);
  return parts.length === 0 ? null : parts.join(', ');
}
