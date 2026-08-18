export type ForceStatusEnableTrigger = 'Hold' | 'AfterBattle' | 'BattleWon' | 'BattleLostOrRetreat' | 'OccupyingWater';

export type ForceStatusClearTrigger =
  | 'Hold'
  | 'AfterMove'
  | 'AfterBattle'
  | 'AfterMoveOrBattle'
  | 'BattleWon'
  | 'BattleLostOrRetreat'
  | 'HoldWhileNotWater';

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
];

export const FORCE_STATUS_CLEAR_OPTIONS: readonly { id: ForceStatusClearTrigger; label: string }[] = [
  { id: 'Hold', label: 'After Hold' },
  { id: 'AfterMove', label: 'After Move or Split' },
  { id: 'AfterBattle', label: 'After any resolved battle' },
  { id: 'AfterMoveOrBattle', label: 'After Move, Split, or a resolved battle' },
  { id: 'BattleWon', label: 'After winning a battle' },
  { id: 'BattleLostOrRetreat', label: 'After losing a battle or forced retreat' },
  { id: 'HoldWhileNotWater', label: 'After Hold while not on a water-feature territory' },
];

/**
 * Standard force statuses copied from docs/DOMAIN.md. Normal is the absence of a status and is not
 * configured. Effects are generic campaign-app text.
 */
export const STANDARD_FORCE_STATUSES: readonly ForceStatusPreset[] = [
  {
    name: 'Diseased',
    effects:
      "Tabletop battles fought while diseased use the campaign sheet's disease modifiers. " +
      'The app displays this and does not resolve the tabletop effect. Map movement is unchanged.',
    enableTrigger: 'OccupyingWater',
    clearTrigger: 'HoldWhileNotWater',
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
