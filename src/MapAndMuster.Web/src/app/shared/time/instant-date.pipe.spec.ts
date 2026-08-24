import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthService } from '../../core/auth/auth.service';
import { InstantDatePipe } from './instant-date.pipe';

describe('InstantDatePipe', () => {
  const instant = '2026-08-13T16:00:00.000Z';
  const currentUser = signal<{ dateTimeDisplayFormat?: string } | null>(null);
  let pipe: InstantDatePipe;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        InstantDatePipe,
        { provide: AuthService, useValue: { currentUser } },
      ],
    });
    currentUser.set(null);
    pipe = TestBed.inject(InstantDatePipe);
  });

  it('formats Month Day, Year, Time Timezone with seconds by default', () => {
    expect(pipe.transform(instant, 'UTC')).toBe('August 13, 2026, 4:00:00 PM UTC');
  });

  it('formats in UTC when no zone is selected', () => {
    expect(pipe.transform(instant, null)).toBe(pipe.transform(instant, 'UTC'));
    expect(pipe.transform(instant, '')).toBe(pipe.transform(instant, 'UTC'));
  });

  it('formats in the selected IANA time zone', () => {
    expect(pipe.transform(instant, 'America/Halifax')).not.toBe(pipe.transform(instant, 'UTC'));
  });

  it('uses the signed-in profile format', () => {
    currentUser.set({ dateTimeDisplayFormat: 'IsoSortable24h' });
    expect(pipe.transform(instant, 'UTC')).toBe('2026-08-13 16:00:00 UTC');
  });
});
