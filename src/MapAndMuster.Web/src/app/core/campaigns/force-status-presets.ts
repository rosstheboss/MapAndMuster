export type ForceStatusEnableTrigger =
  'Hold' | 'AfterBattle' | 'BattleWon' | 'BattleLostOrRetreat' | 'OccupyingWater' | 'Disease';

export type ForceStatusClearTrigger =
  | 'Hold'
  | 'AfterMove'
  | 'AfterBattle'
  | 'AfterMoveOrBattle'
  | 'BattleWon'
  | 'BattleLostOrRetreat'
  | 'HoldWhileNotWater'
  | 'HoldAtSettlement';

export interface ForceStatusCondition {
  trigger: string;
  occurrences: number;
}

export interface ForceStatusPreset {
  name: string;
  effects: string;
  enableConditions: readonly ForceStatusCondition[];
  clearConditions: readonly ForceStatusCondition[];
  priority: number;
  cancelsStatusNames: readonly string[];
}

export const STANDARD_FORCE_STATUSES_PRESET_ID = 'standard-force-statuses';

export const FORCE_STATUS_PRIORITY_MIN = 0;
export const FORCE_STATUS_PRIORITY_MAX = 999;
export const FORCE_STATUS_OCCURRENCES_MIN = 1;
export const FORCE_STATUS_OCCURRENCES_MAX = 10;

export const FORCE_STATUS_ENABLE_OPTIONS: readonly { id: ForceStatusEnableTrigger; label: string }[] = [
  { id: 'Hold', label: 'After Hold' },
  { id: 'AfterBattle', label: 'After any resolved battle' },
  { id: 'BattleWon', label: 'After winning a battle' },
  { id: 'BattleLostOrRetreat', label: 'After losing a battle or forced retreat' },
  { id: 'OccupyingWater', label: 'While occupying a water-feature territory' },
  { id: 'Disease', label: 'Named Diseased engine (water, contagion, rejoin)' },
];

export const FORCE_STATUS_CLEAR_OPTIONS: readonly { id: ForceStatusClearTrigger; label: string }[] = [
  { id: 'Hold', label: 'After Hold' },
  { id: 'AfterMove', label: 'After Move or Split' },
  { id: 'AfterBattle', label: 'After any resolved battle' },
  { id: 'AfterMoveOrBattle', label: 'After Move, Split, or a resolved battle' },
  { id: 'BattleWon', label: 'After winning a battle' },
  { id: 'BattleLostOrRetreat', label: 'After losing a battle or forced retreat' },
  { id: 'HoldWhileNotWater', label: 'After Hold while not on a water-feature territory' },
  { id: 'HoldAtSettlement', label: 'After Hold at a Capital City, City, Supply Depot, or Town' },
];

/**
 * Standard force statuses copied from docs/DOMAIN.md. Normal is the absence of a status and is not
 * configured. Effects are generic campaign-app text. Priorities are 0, 1, 2... in list order.
 */
export const STANDARD_FORCE_STATUSES: readonly ForceStatusPreset[] = [
  {
    name: 'Diseased',
    effects:
      'In battle, before deployment, roll a D6 for every non-Character, non-War Machine, non-Chariot unit. ' +
      'On a 1 that unit is Sick and rerolls 6s to Wound unless it has Poisoned attacks. ' +
      'The app displays this and does not resolve the tabletop effect. ' +
      'Gained after three consecutive actions in water-feature territories, a fought defeat on water, ' +
      'surrender after two water-feature actions, contagion from another faction, rejoining a Diseased split, ' +
      'or a plague-bearing combat win. Cleared by Hold at a Capital City, City, Supply Depot, or Town. ' +
      'Priority 0, so it outranks other standard statuses unless a cancel-out applies.',
    enableConditions: [{ trigger: 'Disease', occurrences: 1 }],
    clearConditions: [{ trigger: 'HoldAtSettlement', occurrences: 1 }],
    priority: 0,
    cancelsStatusNames: [],
  },
  {
    name: 'Shaken',
    effects:
      "Tabletop battles fought while shaken use the campaign sheet's shaken modifiers. " +
      'The app displays this and does not resolve the tabletop effect.',
    enableConditions: [{ trigger: 'BattleLostOrRetreat', occurrences: 1 }],
    clearConditions: [{ trigger: 'Hold', occurrences: 1 }],
    priority: 1,
    cancelsStatusNames: [],
  },
  {
    name: 'Confident',
    effects:
      "Tabletop battles fought while confident use the campaign sheet's confident modifiers. " +
      'The app displays this and does not resolve the tabletop effect.',
    enableConditions: [{ trigger: 'BattleWon', occurrences: 1 }],
    clearConditions: [{ trigger: 'BattleLostOrRetreat', occurrences: 1 }],
    priority: 2,
    cancelsStatusNames: [],
  },
  {
    name: 'Exhausted',
    effects:
      "Tabletop battles fought while exhausted use the campaign sheet's fatigue modifiers. " +
      'The app displays this and does not resolve the tabletop effect. ' +
      'Cancels Well Rested: gaining Exhausted while Well Rested leaves the force with no status.',
    enableConditions: [{ trigger: 'AfterBattle', occurrences: 1 }],
    clearConditions: [{ trigger: 'Hold', occurrences: 1 }],
    priority: 3,
    cancelsStatusNames: ['Well Rested'],
  },
  {
    name: 'Well Rested',
    effects:
      "Tabletop battles fought while well rested use the campaign sheet's rest modifiers. " +
      'The app displays this and does not resolve the tabletop effect. Hold is the rest action that grants this status.',
    enableConditions: [{ trigger: 'Hold', occurrences: 1 }],
    clearConditions: [{ trigger: 'AfterMoveOrBattle', occurrences: 1 }],
    priority: 4,
    cancelsStatusNames: [],
  },
];

export function forceStatusesFromStandardPreset(): ForceStatusPreset[] {
  return STANDARD_FORCE_STATUSES.map((status) => ({
    ...status,
    enableConditions: status.enableConditions.map((condition) => ({ ...condition })),
    clearConditions: status.clearConditions.map((condition) => ({ ...condition })),
    cancelsStatusNames: [...status.cancelsStatusNames],
  }));
}

export function forceStatusEnableConditions(status: {
  enableConditions?: readonly ForceStatusCondition[] | null;
  enableTrigger?: string;
  enableOccurrences?: number;
}): ForceStatusCondition[] {
  return listedOrLegacy(status.enableConditions, status.enableTrigger, status.enableOccurrences);
}

export function forceStatusClearConditions(status: {
  clearConditions?: readonly ForceStatusCondition[] | null;
  clearTrigger?: string;
  clearOccurrences?: number;
}): ForceStatusCondition[] {
  return listedOrLegacy(status.clearConditions, status.clearTrigger, status.clearOccurrences);
}

function listedOrLegacy(
  listed: readonly ForceStatusCondition[] | null | undefined,
  trigger: string | undefined,
  occurrences: number | undefined,
): ForceStatusCondition[] {
  if (listed && listed.length > 0) {
    return listed.map((condition) => ({
      trigger: condition.trigger,
      occurrences: normalizeForceStatusOccurrences(condition.occurrences),
    }));
  }

  if (!trigger) {
    return [];
  }

  return [{ trigger, occurrences: normalizeForceStatusOccurrences(occurrences) }];
}

export function normalizeForceStatusOccurrences(value: number | undefined): number {
  return Number.isInteger(value) && value! >= FORCE_STATUS_OCCURRENCES_MIN && value! <= FORCE_STATUS_OCCURRENCES_MAX
    ? value!
    : FORCE_STATUS_OCCURRENCES_MIN;
}

export function nextForceStatusPriority(used: readonly number[]): number {
  const taken = new Set(used);
  for (let value = FORCE_STATUS_PRIORITY_MIN; value <= FORCE_STATUS_PRIORITY_MAX; value++) {
    if (!taken.has(value)) {
      return value;
    }
  }

  return FORCE_STATUS_PRIORITY_MAX;
}

export function committedForceStatusPriority(
  raw: string | number | null | undefined,
  previous: number,
  usedByOthers: readonly number[],
): number {
  if (raw === '' || raw === null || raw === undefined) {
    return previous;
  }

  const text = String(raw).trim();
  if (!/^-?\d+$/.test(text)) {
    return previous;
  }

  const value = Number(text);
  if (
    !Number.isInteger(value) ||
    value < FORCE_STATUS_PRIORITY_MIN ||
    value > FORCE_STATUS_PRIORITY_MAX ||
    usedByOthers.includes(value)
  ) {
    return previous;
  }

  return value;
}
