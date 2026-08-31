import {
  actionNumberAt,
  battleStatusLabel,
  BATTLE_STATUSES,
  CAMPAIGN_STATUSES,
  durationRangeMessage,
  formatCountdown,
  formatDuration,
  formatPhaseEndTimestamp,
  formatPhaseLabel,
  forceStatusClearLabel,
  forceStatusEnableLabel,
  forceStatusLabel,
  maxAmountForUnit,
  PHASE_KINDS,
  phaseKindLabel,
  statusLabel,
} from './campaign-schedule';
import { FORCE_STATUS_CLEAR_OPTIONS, FORCE_STATUS_ENABLE_OPTIONS } from './force-status-presets';

describe('campaign-schedule helpers', () => {
  it('formats durations, phases, and status labels', () => {
    expect(formatDuration(1, 'Weeks')).toBe('1 week');
    expect(formatDuration(3, 'Days')).toBe('3 days');
    expect(formatPhaseLabel('Action', 2)).toBe('Action 2');
    expect(formatPhaseLabel('Battle', 1)).toBe('Battle phase');
    expect(statusLabel('InProgress')).toBe('In progress');
    expect(statusLabel('Scheduled')).toBe('Scheduled');
  });

  it('formats a phase-end timestamp with a short time zone name', () => {
    expect(formatPhaseEndTimestamp('2026-08-15T22:30:00.000Z', 'America/New_York')).toBe(
      '(August 15, 2026, 6:30:00 PM EDT)',
    );
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

  it('labels every campaign status, battle status, and phase kind without leaking PascalCase', () => {
    expect(CAMPAIGN_STATUSES.map((status) => statusLabel(status))).toEqual(['Scheduled', 'In progress', 'Completed']);
    expect(BATTLE_STATUSES.map((status) => battleStatusLabel(status))).toEqual([
      'Pending',
      'Awaiting results',
      'Finalized',
      'Disputed',
      'GM resolved',
    ]);
    expect(PHASE_KINDS.map((kind) => phaseKindLabel(kind))).toEqual(['Action', 'Battle']);
    expect(battleStatusLabel('AwaitingResults')).not.toBe('AwaitingResults');
  });

  it('labels every force-status trigger and leaves catalog status names as written', () => {
    expect(FORCE_STATUS_ENABLE_OPTIONS.map((option) => forceStatusEnableLabel(option.id))).toEqual(
      FORCE_STATUS_ENABLE_OPTIONS.map((option) => option.label),
    );
    expect(FORCE_STATUS_CLEAR_OPTIONS.map((option) => forceStatusClearLabel(option.id))).toEqual(
      FORCE_STATUS_CLEAR_OPTIONS.map((option) => option.label),
    );
    expect(forceStatusLabel('Shaken')).toBe('Shaken');
    expect(forceStatusLabel(null)).toBe('Normal');
  });
});
