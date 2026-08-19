export interface RoundArmyEscalationRow {
  roundNumber: number;
  maxArmyPoints: number;
  freeSupplyPoints: number;
  freeCharacterCount: number;
}

export const DEFAULT_ROUND_ARMY_POINTS = 1000;

export const DEFAULT_ROUND_FREE_SUPPLY_POINTS = 1;

export const DEFAULT_ROUND_FREE_CHARACTER_COUNT = 1;

export function defaultArmyEscalations(roundCount: number): RoundArmyEscalationRow[] {
  return Array.from({ length: Math.max(1, roundCount) }, (_, index) => ({
    roundNumber: index + 1,
    maxArmyPoints: DEFAULT_ROUND_ARMY_POINTS,
    freeSupplyPoints: DEFAULT_ROUND_FREE_SUPPLY_POINTS,
    freeCharacterCount: DEFAULT_ROUND_FREE_CHARACTER_COUNT,
  }));
}
