import { huntInEstaliaArmyEscalations } from './hunt-in-estalia-defaults';

describe('huntInEstaliaArmyEscalations', () => {
  it('returns the eight-round Hunt in Estalia table', () => {
    const rows = huntInEstaliaArmyEscalations(8);
    expect(rows.map((row) => row.maxArmyPoints)).toEqual([500, 750, 1000, 1250, 1500, 2000, 2500, 3000]);
    expect(rows.map((row) => row.freeSupplyPoints)).toEqual([1, 1, 1, 2, 2, 2, 3, 3]);
    expect(rows.map((row) => row.freeCharacterCount)).toEqual([1, 1, 1, 1, 1, 2, 2, 2]);
  });

  it('copies the last Hunt row for longer campaigns', () => {
    expect(huntInEstaliaArmyEscalations(9)[8]).toEqual({
      roundNumber: 9,
      maxArmyPoints: 3000,
      freeSupplyPoints: 3,
      freeCharacterCount: 2,
    });
  });
});
