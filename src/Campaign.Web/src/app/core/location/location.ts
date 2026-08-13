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
