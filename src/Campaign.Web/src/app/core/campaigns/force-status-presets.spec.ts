import { describe, expect, it } from 'vitest';

import { forceStatusesFromStandardPreset, STANDARD_FORCE_STATUSES } from './force-status-presets';

describe('force-status-presets', () => {
  it('omits Normal and includes the documented statuses', () => {
    const names = STANDARD_FORCE_STATUSES.map((status) => status.name);
    expect(names).toEqual(['Diseased', 'Shaken', 'Confident', 'Exhausted', 'Well Rested']);
    expect(names).not.toContain('Normal');
  });

  it('copies preset entries so later edits do not mutate the catalog', () => {
    const copy = forceStatusesFromStandardPreset();
    copy[0].name = 'Changed';
    expect(STANDARD_FORCE_STATUSES[0].name).toBe('Diseased');
    expect(copy.find((status) => status.name === 'Well Rested')?.enableTrigger).toBe('Hold');
  });
});
