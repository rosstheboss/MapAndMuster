import { InstantDatePipe } from './instant-date.pipe';

describe('InstantDatePipe', () => {
  const pipe = new InstantDatePipe();
  const instant = '2026-08-13T16:00:00.000Z';

  it('formats in UTC when no zone is selected', () => {
    expect(pipe.transform(instant, null)).toBe(pipe.transform(instant, 'UTC'));
    expect(pipe.transform(instant, '')).toBe(pipe.transform(instant, 'UTC'));
  });

  it('formats in the selected IANA time zone', () => {
    expect(pipe.transform(instant, 'America/Halifax')).not.toBe(pipe.transform(instant, 'UTC'));
  });
});
