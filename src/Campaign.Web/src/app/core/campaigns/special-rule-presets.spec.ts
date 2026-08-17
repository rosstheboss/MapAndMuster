import {
  OLD_WORLD_FACTION_SPECIAL_RULES,
  OLD_WORLD_SPECIAL_RULES,
  specialRulesFromOldWorldPreset,
} from './special-rule-presets';

const FACTION_OR_FLAVOR_WORDS = [
  'Beastmen',
  'Brayherds',
  'Empire of Man',
  'Bretonnia',
  'Cathay',
  'Skaven',
  'Vampire',
  'Warhammer',
  'Estalia',
  'Khorne',
  'Nurgle',
  'Slaanesh',
  'Tzeentch',
];

describe('special rule presets', () => {
  it('gives every catalog rule a unique name and a mechanical description', () => {
    const names = OLD_WORLD_SPECIAL_RULES.map((rule) => rule.name);
    expect(new Set(names).size).toBe(names.length);

    for (const rule of OLD_WORLD_SPECIAL_RULES) {
      expect(rule.description.trim().length).toBeGreaterThan(20);
      expect(rule.description.length).toBeLessThanOrEqual(2000);
      const haystack = `${rule.name} ${rule.description}`.toLowerCase();
      for (const word of FACTION_OR_FLAVOR_WORDS) {
        expect(haystack).not.toContain(word.toLowerCase());
      }
    }

    const forced = OLD_WORLD_SPECIAL_RULES.find((rule) => rule.name === 'Forced March');
    expect(forced?.description).toContain('one extra adjacent territory');
    expect(forced?.description.toLowerCase()).toContain('enemy');

    const copy = specialRulesFromOldWorldPreset();
    expect(copy).toHaveLength(OLD_WORLD_SPECIAL_RULES.length);
    copy[0].description = 'Edited';
    expect(OLD_WORLD_SPECIAL_RULES[0]?.description).not.toBe('Edited');
  });

  it('assigns only catalog names to Old World factions', () => {
    const catalog = new Set(OLD_WORLD_SPECIAL_RULES.map((rule) => rule.name));
    for (const names of Object.values(OLD_WORLD_FACTION_SPECIAL_RULES)) {
      for (const name of names) {
        expect(catalog.has(name)).toBe(true);
      }
    }
  });
});
