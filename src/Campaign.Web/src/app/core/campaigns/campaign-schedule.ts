export const DURATION_UNITS = ['Minutes', 'Hours', 'Days', 'Weeks', 'Months'] as const;

export const PHASE_KINDS = ['Action', 'Battle'] as const;

const SINGULAR_UNITS: Readonly<Record<string, string>> = {
  Minutes: 'minute',
  Hours: 'hour',
  Days: 'day',
  Weeks: 'week',
  Months: 'month',
};

export function formatDuration(amount: number, unit: string): string {
  const word = SINGULAR_UNITS[unit] ?? unit.toLowerCase();
  return `${amount} ${word}${amount === 1 ? '' : 's'}`;
}

export function formatPhaseLabel(kind: string, actionNumber: number): string {
  if (kind === 'Battle') {
    return 'Battle phase';
  }

  return `Action ${actionNumber}`;
}

export function statusLabel(status: string): string {
  if (status === 'InProgress') {
    return 'In progress';
  }

  return status;
}

export function actionNumberAt(phases: readonly { kind: string }[], index: number): number {
  return phases.slice(0, index + 1).filter((phase) => phase.kind === 'Action').length;
}

export function maxAmountForUnit(unit: string): number {
  switch (unit) {
    case 'Minutes':
      return 60;
    case 'Hours':
      return 24;
    case 'Days':
      return 7;
    case 'Weeks':
      return 52;
    case 'Months':
      return 12;
    default:
      return 60;
  }
}

export function durationRangeMessage(label: string, amount: number, unit: string): string | null {
  const max = maxAmountForUnit(unit);
  if (amount >= 1 && amount <= max) {
    return null;
  }

  const word = SINGULAR_UNITS[unit] ?? unit.toLowerCase();
  return `${label} must be between 1 and ${max} ${word}${max === 1 ? '' : 's'}.`;
}
