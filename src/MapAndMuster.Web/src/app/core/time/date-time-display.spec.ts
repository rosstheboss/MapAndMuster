import { DATE_TIME_FORMAT_SAMPLE, formatInstant, parseDateTimeDisplayFormat } from './date-time-display';

describe('date-time display formatting', () => {
  const instant = DATE_TIME_FORMAT_SAMPLE;
  const zone = 'America/New_York';

  it('defaults to Month Day, Year, 12-hour time with timezone', () => {
    expect(formatInstant(instant, zone)).toBe('January 5, 2027, 12:34:52 PM EST');
    expect(parseDateTimeDisplayFormat(undefined)).toBe('MonthDayYear12h');
  });

  it('formats each supported profile option', () => {
    expect(formatInstant(instant, zone, 'MonthDayYear12h')).toBe('January 5, 2027, 12:34:52 PM EST');
    expect(formatInstant(instant, zone, 'DayMonthYear12h')).toBe('5 January 2027, 12:34:52 PM EST');
    expect(formatInstant(instant, zone, 'MonthDayYear24h')).toBe('January 5, 2027, 12:34:52 EST');
    expect(formatInstant(instant, zone, 'IsoSortable12h')).toBe('2027-01-05 12:34:52 PM EST');
    expect(formatInstant(instant, zone, 'IsoSortable24h')).toBe('2027-01-05 12:34:52 EST');
    expect(formatInstant(instant, zone, 'NumericUs12h')).toBe('1/5/2027, 12:34:52 PM EST');
    expect(formatInstant(instant, zone, 'NumericEu24h')).toBe('5/1/2027, 12:34:52 EST');
  });

  it('falls back to UTC when the zone is missing or invalid', () => {
    expect(formatInstant(instant, null)).toBe(formatInstant(instant, 'UTC'));
    expect(formatInstant(instant, 'Not/AZone')).toBe(formatInstant(instant, 'UTC'));
  });
});
