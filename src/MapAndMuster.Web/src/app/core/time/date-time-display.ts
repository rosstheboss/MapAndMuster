export const DATE_TIME_DISPLAY_FORMATS = [
  'MonthDayYear12h',
  'DayMonthYear12h',
  'MonthDayYear24h',
  'IsoSortable12h',
  'IsoSortable24h',
  'NumericUs12h',
  'NumericEu24h',
] as const;

export type DateTimeDisplayFormat = (typeof DATE_TIME_DISPLAY_FORMATS)[number];

export const DEFAULT_DATE_TIME_DISPLAY_FORMAT: DateTimeDisplayFormat = 'MonthDayYear12h';

/** Fixed instant used for profile picker examples (noon-ish in US Eastern in January). */
export const DATE_TIME_FORMAT_SAMPLE = '2027-01-05T17:34:52.000Z';

export function parseDateTimeDisplayFormat(value: string | null | undefined): DateTimeDisplayFormat {
  if (value && (DATE_TIME_DISPLAY_FORMATS as readonly string[]).includes(value)) {
    return value as DateTimeDisplayFormat;
  }

  return DEFAULT_DATE_TIME_DISPLAY_FORMAT;
}

export function formatInstant(
  value: string | Date | null | undefined,
  timeZone?: string | null,
  format?: string | null,
): string {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const zone = timeZone?.trim() ? timeZone.trim() : 'UTC';
  const chosen = parseDateTimeDisplayFormat(format);
  try {
    return formatWithZone(date, zone, chosen);
  } catch {
    return formatWithZone(date, 'UTC', chosen);
  }
}

function formatWithZone(date: Date, timeZone: string, format: DateTimeDisplayFormat): string {
  const hour12 = format.endsWith('12h');
  const iso = format.startsWith('Iso');
  const numeric = format.startsWith('Numeric');
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    year: 'numeric',
    month: iso ? '2-digit' : numeric ? 'numeric' : 'long',
    day: iso ? '2-digit' : 'numeric',
    hour: hour12 ? 'numeric' : '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12,
    timeZoneName: 'short',
  }).formatToParts(date);
  const read = (type: Intl.DateTimeFormatPartTypes): string => parts.find((part) => part.type === type)?.value ?? '';
  const year = read('year');
  const month = read('month');
  const day = read('day');
  const time = hour12
    ? `${read('hour')}:${read('minute')}:${read('second')} ${read('dayPeriod')} ${read('timeZoneName')}`
    : `${read('hour')}:${read('minute')}:${read('second')} ${read('timeZoneName')}`;

  switch (format) {
    case 'MonthDayYear12h':
    case 'MonthDayYear24h':
      return `${month} ${day}, ${year}, ${time}`;
    case 'DayMonthYear12h':
      return `${day} ${month} ${year}, ${time}`;
    case 'IsoSortable12h':
    case 'IsoSortable24h':
      return `${year}-${month}-${day} ${time}`;
    case 'NumericUs12h':
      return `${month}/${day}/${year}, ${time}`;
    case 'NumericEu24h':
      return `${day}/${month}/${year}, ${time}`;
  }
}
