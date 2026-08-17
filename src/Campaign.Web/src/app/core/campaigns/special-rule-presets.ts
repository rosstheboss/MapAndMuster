export interface SpecialRulePreset {
  name: string;
  description: string;
}

/**
 * Generic catalog derived from the Hunt in Estalia / Old World faction-rule categories in
 * docs/CAMPAIGN-RULES-MATRIX.md. Names and descriptions are reusable: no faction names, lore, or
 * proprietary prose.
 */
export const OLD_WORLD_SPECIAL_RULES: readonly SpecialRulePreset[] = [
  {
    name: 'Forced March',
    description:
      'A Move by this force may cover one extra adjacent territory in the same action. If that extra ' +
      'step would enter a territory that already contains an enemy force, movement stops there and a ' +
      'battle is created. This does not change which territories are adjacent.',
  },
  {
    name: 'Stockpiled Holds',
    description:
      'Operational structures this force controls grant extra campaign supply of the amount listed on ' +
      'the campaign sheet, in addition to the normal supply graph.',
  },
  {
    name: 'Harsh Reaving',
    description:
      'A Pillage by this force may affect extra eligible structures in the same territory, or apply a ' +
      'stronger pillage result, as listed on the campaign sheet.',
  },
  {
    name: 'Unyielding Host',
    description:
      'This force may ignore a named campaign status, or transfer that status to another eligible force, ' +
      'when the campaign sheet allows it.',
  },
  {
    name: 'Chosen Withdrawal',
    description:
      'When this force retreats, eligible destinations may include an exception listed on the campaign ' +
      'sheet instead of only the default retreat list.',
  },
  {
    name: 'Relic Sense',
    description:
      'The holder may be told whether a hidden item objective is in an adjacent territory. The exact ' +
      'territory is not revealed until the item is found or a manager reveals it.',
  },
  {
    name: 'Relic Pursuit',
    description:
      'Campaign movement for this force may be directed toward a revealed item objective when the ' +
      'campaign sheet requires or allows it.',
  },
  {
    name: 'Living Ground',
    description:
      'Named terrain types this force controls count as a structure for supply and/or defense, as listed ' +
      'on the campaign sheet.',
  },
  {
    name: 'Independent Stores',
    description:
      'This force may generate campaign supply from a source other than the normal owned and allied ' +
      'territory graph, as listed on the campaign sheet.',
  },
  {
    name: 'Scattered Arrival',
    description:
      'Starting placement for this force may use an alternate spawn or a random eligible territory ' +
      'instead of the faction default spawn, as listed on the campaign sheet.',
  },
  {
    name: 'Compelled Hunt',
    description:
      'While a named item objective is revealed, this force must Move toward it when it is able to, as ' +
      'listed on the campaign sheet.',
  },
  {
    name: "Raider's Cache",
    description:
      'A successful Pillage by this force may grant temporary extra supply for the next eligible battle, ' +
      'as listed on the campaign sheet.',
  },
  {
    name: 'Ambush Doctrine',
    description:
      'Tabletop battles fought by this force may use ambush modifiers recorded on the assigned mission. ' +
      'The app displays this and does not resolve the tabletop effect.',
  },
  {
    name: 'Arcane Reserves',
    description:
      'This force may use extra casting or dispelling resources recorded as battle metadata. The app ' +
      'displays this and does not resolve spells.',
  },
  {
    name: 'Once-only Stratagem',
    description:
      'This force has a one-use battle ability described on the campaign sheet. The app displays this ' +
      'and does not resolve it.',
  },
  {
    name: 'Grounded Warfare',
    description:
      'Tabletop modifiers may apply when fighting on named terrain types, including water features when ' +
      'the campaign sheet says so. The app displays this with the mission and does not resolve tabletop ' +
      'movement.',
  },
  {
    name: 'Auxiliary Levy',
    description:
      'Eligible army lists or hired auxiliaries may change according to the campaign sheet. The app ' +
      'displays this and does not validate army lists.',
  },
];

export const OLD_WORLD_FACTION_SPECIAL_RULES: Readonly<Record<string, readonly string[]>> = {
  'Beastmen Brayherds': ['Scattered Arrival', 'Harsh Reaving', 'Living Ground'],
  'Dark Elves': ['Ambush Doctrine', "Raider's Cache"],
  'Chaos Dwarfs': ['Stockpiled Holds', 'Grounded Warfare'],
  'Daemons of Chaos': ['Arcane Reserves', 'Unyielding Host'],
  'Dwarfen Mountain Holds': ['Stockpiled Holds', 'Unyielding Host'],
  'Grand Cathay': ['Forced March', 'Arcane Reserves'],
  'Empire of Man': ['Auxiliary Levy', 'Forced March'],
  'High Elf Realms': ['Arcane Reserves', 'Relic Sense'],
  Lizardmen: ['Living Ground', 'Relic Sense'],
  'Kingdom of Bretonnia': ['Forced March', 'Auxiliary Levy'],
  'Ogre Kingdoms': ['Independent Stores', 'Harsh Reaving'],
  'Orc & Goblin Tribes': ['Harsh Reaving', 'Scattered Arrival'],
  'Tomb Kings of Khemri': ['Relic Sense', 'Unyielding Host'],
  Skaven: ['Ambush Doctrine', 'Scattered Arrival'],
  'Warriors of Chaos': ['Compelled Hunt', 'Harsh Reaving'],
  'Vampire Counts': ['Unyielding Host', 'Relic Pursuit'],
  'Wood Elf Realms': ['Living Ground', 'Ambush Doctrine'],
};

export function specialRulesFromOldWorldPreset(): SpecialRulePreset[] {
  return OLD_WORLD_SPECIAL_RULES.map((rule) => ({ name: rule.name, description: rule.description }));
}

export function specialRuleNamesForFaction(factionName: string): readonly string[] {
  return OLD_WORLD_FACTION_SPECIAL_RULES[factionName] ?? [];
}
