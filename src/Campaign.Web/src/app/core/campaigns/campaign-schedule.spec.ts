import {
  actionNumberAt,
  durationRangeMessage,
  formatCountdown,
  formatDuration,
  formatPhaseLabel,
  maxAmountForUnit,
  statusLabel,
} from './campaign-schedule';

describe('campaign-schedule helpers', () => {
  it('formats durations, phases, and status labels', () => {
    expect(formatDuration(1, 'Weeks')).toBe('1 week');
    expect(formatDuration(3, 'Days')).toBe('3 days');
    expect(formatPhaseLabel('Action', 2)).toBe('Action 2');
    expect(formatPhaseLabel('Battle', 1)).toBe('Battle phase');
    expect(statusLabel('InProgress')).toBe('In progress');
    expect(statusLabel('Scheduled')).toBe('Scheduled');
  });

  it('formats a remaining phase countdown from a server end instant', () => {
    const now = Date.parse('2026-08-14T12:00:00.000Z');
    expect(formatCountdown('2026-08-16T12:00:00.000Z', now)).toBe('2d 0h 0m');
    expect(formatCountdown('2026-08-14T13:00:00.000Z', now)).toBe('1h 0m 0s');
    expect(formatCountdown('2026-08-14T12:01:30.000Z', now)).toBe('1m 30s');
    expect(formatCountdown('2026-08-14T11:00:00.000Z', now)).toBe('0s');
  });

  it('numbers actions in mixed round order', () => {
    const phases = [{ kind: 'Action' }, { kind: 'Battle' }, { kind: 'Action' }];
    expect(actionNumberAt(phases, 0)).toBe(1);
    expect(actionNumberAt(phases, 2)).toBe(2);
  });

  it('uses unit-specific duration ranges', () => {
    expect(maxAmountForUnit('Minutes')).toBe(60);
    expect(maxAmountForUnit('Hours')).toBe(24);
    expect(maxAmountForUnit('Days')).toBe(7);
    expect(maxAmountForUnit('Weeks')).toBe(52);
    expect(maxAmountForUnit('Months')).toBe(12);
    expect(durationRangeMessage('Round length', 8, 'Days')).toBe('Round length must be between 1 and 7 days.');
    expect(durationRangeMessage('Round length', 1, 'Weeks')).toBeNull();
  });
});
