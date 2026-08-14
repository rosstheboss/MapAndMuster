import {
  actionNumberAt,
  durationRangeMessage,
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
