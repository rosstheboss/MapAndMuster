import { describe, expect, it } from 'vitest';

import {
  committedForceStatusPriority,
  diseasedClearConditions,
  diseasedEnableConditions,
  FORCE_STATUS_CLEAR_OPTIONS,
  FORCE_STATUS_ENABLE_OPTIONS,
  forceStatusClearConditions,
  forceStatusEnableConditions,
  forceStatusesFromStandardPreset,
  nextForceStatusPriority,
  STANDARD_FORCE_STATUSES,
} from './force-status-presets';

describe('force-status-presets', () => {
  it('omits Normal and includes the documented statuses', () => {
    const names = STANDARD_FORCE_STATUSES.map((status) => status.name);
    expect(names).toEqual(['Diseased', 'Shaken', 'Confident', 'Exhausted', 'Well Rested']);
    expect(names).not.toContain('Normal');
  });

  it('assigns sequential priorities and Exhausted cancels Well Rested', () => {
    expect(STANDARD_FORCE_STATUSES.map((status) => status.priority)).toEqual([0, 1, 2, 3, 4]);
    expect(STANDARD_FORCE_STATUSES.find((status) => status.name === 'Exhausted')?.cancelsStatusNames).toEqual([
      'Well Rested',
    ]);
  });

  it('uses Water and settlement location filters for Diseased', () => {
    const water = 'water-tag';
    const structures = {
      'Capital City': 'capital',
      City: 'city',
      'Supply Depot': 'depot',
      Town: 'town',
    };
    const copy = forceStatusesFromStandardPreset(water, structures);
    const diseased = copy.find((status) => status.name === 'Diseased');
    expect(diseasedEnableConditions(water)).toEqual([
      { trigger: 'ConsecutiveActions', occurrences: 3, locationKind: 'TerrainTag', locationTagId: water },
      { trigger: 'BattleLostOrRetreat', occurrences: 1, locationKind: 'TerrainTag', locationTagId: water },
      { trigger: 'Surrender', occurrences: 2, locationKind: 'TerrainTag', locationTagId: water },
    ]);
    expect(diseasedClearConditions(structures).map((condition) => condition.locationTypeId)).toEqual([
      'capital',
      'city',
      'depot',
      'town',
    ]);
    expect(diseased?.enableConditions[0]?.trigger).toBe('ConsecutiveActions');
    expect(diseased?.clearConditions).toHaveLength(4);
  });

  it('reads listed conditions and wraps a legacy single trigger', () => {
    expect(
      forceStatusEnableConditions({
        enableConditions: [{ trigger: 'BattleLostOrRetreat', occurrences: 1 }],
      }),
    ).toEqual([
      {
        trigger: 'BattleLostOrRetreat',
        occurrences: 1,
        locationKind: 'Any',
        locationTypeId: null,
        locationTagId: null,
        requiredStatusId: null,
      },
    ]);
    expect(forceStatusEnableConditions({ enableTrigger: 'Hold', enableOccurrences: 3, enableConditions: [] })).toEqual([
      { trigger: 'Hold', occurrences: 3, locationKind: 'Any' },
    ]);
    expect(forceStatusClearConditions({ clearTrigger: 'BattleWon' })).toEqual([
      { trigger: 'BattleWon', occurrences: 1, locationKind: 'Any' },
    ]);
  });

  it('copies preset entries so later edits do not mutate the catalog', () => {
    const copy = forceStatusesFromStandardPreset();
    copy[0].name = 'Changed';
    expect(STANDARD_FORCE_STATUSES[0].name).toBe('Diseased');
    expect(STANDARD_FORCE_STATUSES[0].enableConditions[0]?.trigger).toBe('ConsecutiveActions');
    expect(STANDARD_FORCE_STATUSES[0].clearConditions[0]?.trigger).toBe('Hold');
    expect(copy.find((status) => status.name === 'Well Rested')?.enableConditions[0]?.trigger).toBe('Hold');
    expect(STANDARD_FORCE_STATUSES.find((status) => status.name === 'Exhausted')?.cancelsStatusNames).toEqual([
      'Well Rested',
    ]);
  });

  it('lists occupying-with-status triggers for enable and clear', () => {
    expect(FORCE_STATUS_ENABLE_OPTIONS.some((option) => option.id === 'OccupyingWithThisStatus')).toBe(true);
    expect(FORCE_STATUS_ENABLE_OPTIONS.some((option) => option.id === 'OccupyingWithSpecifiedStatus')).toBe(true);
    expect(FORCE_STATUS_CLEAR_OPTIONS.some((option) => option.id === 'OccupyingWithThisStatus')).toBe(true);
    expect(FORCE_STATUS_CLEAR_OPTIONS.some((option) => option.id === 'OccupyingWithSpecifiedStatus')).toBe(true);
  });

  it('picks the lowest unused priority for a new status', () => {
    expect(nextForceStatusPriority([0, 1, 5])).toBe(2);
    expect(nextForceStatusPriority([])).toBe(0);
  });

  it('reverts invalid or duplicate priority edits to the previous value', () => {
    expect(committedForceStatusPriority('3', 1, [0, 2])).toBe(3);
    expect(committedForceStatusPriority('2', 1, [0, 2])).toBe(1);
    expect(committedForceStatusPriority('1000', 4, [])).toBe(4);
    expect(committedForceStatusPriority('1.5', 4, [])).toBe(4);
    expect(committedForceStatusPriority('', 7, [])).toBe(7);
    expect(committedForceStatusPriority('abc', 7, [])).toBe(7);
  });
});
