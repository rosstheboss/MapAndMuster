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

export interface ForceStatusPreset {
  name: string;
  effects: string;
  enableTrigger: ForceStatusEnableTrigger;
  clearTrigger: ForceStatusClearTrigger;
}

export const STANDARD_FORCE_STATUSES_PRESET_ID = 'standard-force-statuses';

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
 * configured. Effects are generic campaign-app text.
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
      'Diseased overrides other catalog statuses.',
    enableTrigger: 'Disease',
    clearTrigger: 'HoldAtSettlement',
  },
  {
    name: 'Shaken',
    effects:
      "Tabletop battles fought while shaken use the campaign sheet's shaken modifiers. " +
      'The app displays this and does not resolve the tabletop effect.',
    enableTrigger: 'BattleLostOrRetreat',
    clearTrigger: 'Hold',
  },
  {
    name: 'Confident',
    effects:
      "Tabletop battles fought while confident use the campaign sheet's confident modifiers. " +
      'The app displays this and does not resolve the tabletop effect.',
    enableTrigger: 'BattleWon',
    clearTrigger: 'BattleLostOrRetreat',
  },
  {
    name: 'Exhausted',
    effects:
      "Tabletop battles fought while exhausted use the campaign sheet's fatigue modifiers. " +
      'The app displays this and does not resolve the tabletop effect.',
    enableTrigger: 'AfterBattle',
    clearTrigger: 'Hold',
  },
  {
    name: 'Well Rested',
    effects:
      "Tabletop battles fought while well rested use the campaign sheet's rest modifiers. " +
      'The app displays this and does not resolve the tabletop effect. Hold is the rest action that grants this status.',
    enableTrigger: 'Hold',
    clearTrigger: 'AfterMoveOrBattle',
  },
];

export function forceStatusesFromStandardPreset(): ForceStatusPreset[] {
  return STANDARD_FORCE_STATUSES.map((status) => ({ ...status }));
}
