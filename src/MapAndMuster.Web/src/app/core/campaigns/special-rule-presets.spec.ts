import {
  OLD_WORLD_FACTION_SPECIAL_RULES,
  OLD_WORLD_SPECIAL_RULES,
  OLD_WORLD_SUBFACTION_SPECIAL_RULES,
  specialRulesFromOldWorldPreset,
} from './special-rule-presets';

describe('special rule presets', () => {
  it('gives every catalog rule a unique name, effect key, and description', () => {
    const names = OLD_WORLD_SPECIAL_RULES.map((rule) => rule.name);
    expect(new Set(names).size).toBe(names.length);
    const keys = OLD_WORLD_SPECIAL_RULES.map((rule) => rule.effectKey);
    expect(new Set(keys).size).toBe(keys.length);

    for (const rule of OLD_WORLD_SPECIAL_RULES) {
      expect(rule.description.trim().length).toBeGreaterThan(20);
      expect(rule.description.length).toBeLessThanOrEqual(2000);
      expect(rule.effectKey).toBeTruthy();
    }

    const calledByTheRelic = OLD_WORLD_SPECIAL_RULES.find((rule) => rule.name === 'Called by the Relic');
    expect(calledByTheRelic?.description).toContain('extra movement speed');
    expect(calledByTheRelic?.description).toContain('gain back the extra movement speed');
    expect(calledByTheRelic?.description).not.toContain('+2');
    expect(calledByTheRelic?.effectKey).toBe('CalledByTheRelic');
    const undead = OLD_WORLD_SPECIAL_RULES.find((rule) => rule.name === 'Undead');
    expect(undead?.description).toContain('Undead forces');
    expect(undead?.description).toContain('Exhausted');
    expect(undead?.effectKey).toBe('Undead');
    expect(OLD_WORLD_FACTION_SPECIAL_RULES['Tomb Kings of Khemri']).toEqual(['Called by the Relic', 'Undead']);
    expect(OLD_WORLD_FACTION_SPECIAL_RULES['Vampire Counts']).toEqual(['Fresh Corpses', 'Undead']);

    const copy = specialRulesFromOldWorldPreset();
    expect(copy).toHaveLength(OLD_WORLD_SPECIAL_RULES.length);
    copy[0].description = 'Edited';
    expect(OLD_WORLD_SPECIAL_RULES[0]?.description).not.toBe('Edited');
  });

  it('assigns only catalog names to Old World factions and daemon subfactions', () => {
    const catalog = new Set(OLD_WORLD_SPECIAL_RULES.map((rule) => rule.name));
    for (const names of Object.values(OLD_WORLD_FACTION_SPECIAL_RULES)) {
      for (const name of names) {
        expect(catalog.has(name)).toBe(true);
      }
    }

    for (const bySubfaction of Object.values(OLD_WORLD_SUBFACTION_SPECIAL_RULES)) {
      for (const names of Object.values(bySubfaction)) {
        for (const name of names) {
          expect(catalog.has(name)).toBe(true);
        }
      }
    }
  });
});
