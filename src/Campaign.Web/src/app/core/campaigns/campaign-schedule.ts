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

export function formatPhaseEndTimestamp(endsUtc: string, timeZone?: string | null): string {
  const date = new Date(endsUtc);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const zone = timeZone?.trim() ? timeZone.trim() : 'UTC';
  try {
    return `(${formatPhaseEndParts(date, zone)})`;
  } catch {
    return `(${formatPhaseEndParts(date, 'UTC')})`;
  }
}

function formatPhaseEndParts(date: Date, timeZone: string): string {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: 'numeric',
    minute: '2-digit',
    second: '2-digit',
    hour12: true,
    timeZoneName: 'short',
  }).formatToParts(date);
  const value = (type: Intl.DateTimeFormatPartTypes): string => parts.find((part) => part.type === type)?.value ?? '';
  return `${value('year')}-${value('month')}-${value('day')} ${value('hour')}:${value('minute')}:${value('second')} ${value('dayPeriod')} ${value('timeZoneName')}`;
}

export function formatCountdown(endsUtc: string, nowMs: number): string {
  const end = Date.parse(endsUtc);
  if (Number.isNaN(end)) {
    return '';
  }

  const remaining = Math.max(0, end - nowMs);
  const totalSeconds = Math.floor(remaining / 1000);
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  if (days > 0) {
    return `${days}d ${hours}h ${minutes}m`;
  }

  if (hours > 0) {
    return `${hours}h ${minutes}m ${seconds}s`;
  }

  if (minutes > 0) {
    return `${minutes}m ${seconds}s`;
  }

  return `${seconds}s`;
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
