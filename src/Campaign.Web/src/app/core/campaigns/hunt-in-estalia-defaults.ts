export interface HuntRoundEscalation {
  roundNumber: number;
  maxArmyPoints: number;
  freeSupplyPoints: number;
  freeCharacterCount: number;
}

export const HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_PERCENT = 25;

export const HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS = 1;

const HUNT_IN_ESTALIA_ESCALATION_TEMPLATE: readonly Omit<HuntRoundEscalation, 'roundNumber'>[] = [
  { maxArmyPoints: 1000, freeSupplyPoints: 0, freeCharacterCount: 0 },
  { maxArmyPoints: 1250, freeSupplyPoints: 0, freeCharacterCount: 0 },
  { maxArmyPoints: 1500, freeSupplyPoints: 1, freeCharacterCount: 1 },
  { maxArmyPoints: 1750, freeSupplyPoints: 1, freeCharacterCount: 1 },
  { maxArmyPoints: 2000, freeSupplyPoints: 2, freeCharacterCount: 1 },
  { maxArmyPoints: 2000, freeSupplyPoints: 2, freeCharacterCount: 2 },
  { maxArmyPoints: 2000, freeSupplyPoints: 3, freeCharacterCount: 2 },
  { maxArmyPoints: 2000, freeSupplyPoints: 3, freeCharacterCount: 2 },
];

export function huntInEstaliaArmyEscalations(roundCount: number): HuntRoundEscalation[] {
  const last = HUNT_IN_ESTALIA_ESCALATION_TEMPLATE[HUNT_IN_ESTALIA_ESCALATION_TEMPLATE.length - 1];

  return Array.from({ length: Math.max(1, roundCount) }, (_, index) => {
    const entry = HUNT_IN_ESTALIA_ESCALATION_TEMPLATE[index] ?? last;
    return {
      roundNumber: index + 1,
      maxArmyPoints: entry.maxArmyPoints,
      freeSupplyPoints: entry.freeSupplyPoints,
      freeCharacterCount: entry.freeCharacterCount,
    };
  });
}
