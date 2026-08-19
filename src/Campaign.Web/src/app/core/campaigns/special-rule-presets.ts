export interface SpecialRulePreset {
  name: string;
  description: string;
  effectKey?: string;
}

/**
 * Hunt in Estalia / Old World faction special rules. Mechanical keys match
 * Campaign.Domain.Campaigns.SpecialRuleEffectKeys.
 */
export const OLD_WORLD_SPECIAL_RULES: readonly SpecialRulePreset[] = [
  {
    name: 'Expert Ambushers',
    effectKey: 'ExpertAmbushers',
    description: 'Beastmen Ambushers gain a +1 to Ambushing rolls.',
  },
  {
    name: 'Crusaders',
    effectKey: 'Crusaders',
    description:
      'During the Move action, a Bretonnian force can move up to two adjacent territories in one action. ' +
      'The player must place the Move order to state what territory they will go first and then the territory they will ' +
      'land in. The second territory does not need to be adjacent to the origin territory, just to the first territory ' +
      'the Bretonnian force moves into. For example, a Move order will look like this “Force at A Moves to B and then C.” ' +
      'If along the way, the Bretonnian player encounters another opponent faction’s force, they will stop and do battle. ' +
      'The Bretonnian player who moves two territories does not claim the first territory they move through, regardless of ' +
      'its state. They also cannot move through the opposing player’s spawn location. Split forces are only rejoined if ' +
      'both forces are moved into the same location.',
  },
  {
    name: 'Safe in Water',
    effectKey: 'SafeInWater',
    description:
      'Bretonnian Questing and Grail vow knights, Questing and Grail vow characters, Handmaidens of the Lady, and ' +
      'special characters never roll for dangerous terrain on Water Features.',
  },
  {
    name: 'Slavers',
    effectKey: 'Slavers',
    description: 'Captured unpillaged towns and cities provide an extra supply point each.',
  },
  {
    name: 'Divided We Stand',
    effectKey: 'DividedWeStand',
    description:
      'You cannot take an undivided Daemons army. Before the campaign, you must choose which Chaos god you follow and ' +
      'each god is treated as a separate faction. As well, each of the four Daemon factions count as Allies with each ' +
      'other (and can backstab each other).',
  },
  {
    name: 'Only Blood Satisfies!',
    effectKey: 'OnlyBloodSatisfies',
    description:
      'Khorne, when pillaging, can choose to destroy the structure immediately in a single action. They can also pillage ' +
      'and destroy allied structures.',
  },
  {
    name: 'Bringers of the Plague',
    effectKey: 'BringersOfThePlague',
    description:
      'A Nurgle army can never be diseased or well rested. However, if they beat any army that is not Diseased or Shaken, ' +
      'then that opposing army is now Diseased.',
  },
  {
    name: 'Alluring',
    effectKey: 'Alluring',
    description:
      'During the command phase, one unit in the army with line of sight to an enemy non-character, non-war machine unit ' +
      'within 12” that is not locked in close combat or fleeing can attempt to “seduce” the target unit. The Slaanesh unit ' +
      'performs a leadership check with their own leadership but at -1 to their Ld (stacking with any other modifiers in ' +
      'effect). If successful, the target unit gains the Stupidity special rule.',
  },
  {
    name: 'Magical Supply',
    effectKey: 'MagicalSupply',
    description:
      'Any supply points Tzeentch players don’t use in army composition for a battle can be used for a one per battle ' +
      'casting or dispelling reroll. So if for example, the Tzeentch player has access to five supply points but only uses ' +
      'three in army composition, the remaining two can be used in the upcoming battle. These extra re-rolls cannot be ' +
      'saved up from battle to battle and can only be used in the battle you write the army list for.',
  },
  {
    name: 'Treacherous',
    effectKey: 'Treacherous',
    description:
      'Once per game in the Command phase, the Dark Elves player can nominate a non-character, non-Monster, non-War ' +
      'Machine unit that is not fleeing or locked in combat. That unit gains the Fly(10) and Ethereal special rules until ' +
      'the end of the Dark Elf player’s turn, but they cannot charge.',
  },
  {
    name: 'It Is Going In The Book!',
    effectKey: 'ItIsGoingInTheBook',
    description:
      'Once per battle, the Dwarfs randomly select one non-war machine, non-chariot, and non-character unit that does ' +
      'not have the Hatred special rule. That randomly selected unit has the Hatred special rule.',
  },
  {
    name: 'Rulers of Stone',
    effectKey: 'RulersOfStone',
    description:
      'When playing on Mountain or Cave terrain maps, all partial cover from terrain (not units) count as Full Cover. ' +
      'Also, Dwarfs can never flee off of cliffs: they will stop at the Cliff edge (but still count as fleeing).',
  },
  {
    name: 'Prepared for Battle',
    effectKey: 'PreparedForBattle',
    description:
      'Empire of Man can spend a single supply point to bring “Extra Black Powder” during each battle. If they do, each ' +
      'unit firing a black powder weapon and all war machines firing a black powder weapon get a single reroll to hit ' +
      'once per battle.',
  },
  {
    name: 'The Art of War',
    effectKey: 'ArtOfWar',
    description:
      'If a Cathay army retreats, they can retreat into any territory and can even capture a territory in this way.',
  },
  {
    name: 'Determined',
    effectKey: 'Determined',
    description:
      'When on a Beach territory or an adjacent territory to a Spawn Location, the High Elves get to simply choose who ' +
      'goes first unless the mission explicitly states who goes first. This overrides the Bretonnian and the Wolves of ' +
      'the Sea and any other faction special rule on who goes first or second. The High Elf player makes this choice ' +
      'after deployment, scouts, and vanguard moves.',
  },
  {
    name: 'Conduits of Power',
    effectKey: 'ConduitsOfPower',
    description:
      'When in an adjacent territory with a hidden relic, the Lizardmen player will be notified that they are within one ' +
      'territory of the relic. Once a Relic has been found, they can move to an adjacent location of the Relic regardless ' +
      'of where they are on the map.',
  },
  {
    name: 'Spawning Pools',
    effectKey: 'SpawningPools',
    description:
      'All water feature tiles captured by the Lizardmen faction (not allies) count as both Supply Depots and ' +
      'Fortifications unless they have a town, city, and castle on them. Lizardmen forces do not require a path to these ' +
      'territories for the benefit. This benefit does not confer to allies and ally captured water feature territories do ' +
      'not count for supply depots (but can still be used defensively as fortifications). Lizardmen can build or repair a ' +
      'supply depot or fortification and will gain an extra supply point from these built structures.',
  },
  {
    name: 'For Hire',
    effectKey: 'ForHire',
    description:
      'Ogre Kingdom non-character units can be mercenaries in any other faction, but they follow all other mercenary ' +
      'rules and are subject to the Misbehaving Mercenaries special rule.',
  },
  {
    name: 'Tough Guts',
    effectKey: 'ToughGuts',
    description:
      'An Ogre Kingdoms army (or Mercenary units) can never have the Disease status. Before the beginning of a battle, ' +
      'one non-character unit without Frenzy at random is subject to Frenzy.',
  },
  {
    name: 'The Green Tide',
    effectKey: 'GreenTide',
    description:
      'O&G armies do not build supply depots (but pillaged supply depots can be repaired). Any location that they control ' +
      'that does not have a structure counts as a supply depot. Existing supply depots do count as supply depots as well. ' +
      'This does not include ally supply depots or confers this bonus to allies. Pillaged structures don’t count as ' +
      'structures for this ability.',
  },
  {
    name: 'Defenders of the Homeland',
    effectKey: 'DefendersOfTheHomeland',
    description:
      'Renegade Crowns can use any neutral town or city as supply depots regardless of location. However, allies can only ' +
      'connect to Renegade Crowns supply lines that are connected to their supply lines and force.',
  },
  {
    name: 'The Great City of Magritta',
    effectKey: 'GreatCityOfMagritta',
    description:
      'Renegade Crowns spawn location is at the capital city. The capital city provides them the supply points of a city.',
  },
  {
    name: 'The Underground Network',
    effectKey: 'UndergroundNetwork',
    description:
      'Skaven do not have a spawn point. At the beginning of the game, they will be randomized into one of the towns or ' +
      'cities. Afterwards, if they are forced to move to a Spawn Location, they are randomized to move into an empty town ' +
      'or city. If none are empty, then they appear in the capital city. They immediately capture the empty town or city ' +
      'even if another force controls it (except the Capital city or any Spawn Location). If in a Spawn Location with ' +
      'another faction, they won’t fight until they move out of the Spawn Location into another territory.',
  },
  {
    name: 'Called by the Relic',
    effectKey: 'CalledByTheRelic',
    description:
      'If a Relic is found, the Tomb Kings players must make Move actions and travel to the closest territory (or choose ' +
      'from equal choices) until they capture the Relic or they are forced to do battle.',
  },
  {
    name: 'Relic of a Past Age',
    effectKey: 'RelicOfAPastAge',
    description:
      'If they have a relic, they may gain +2 to a casting or dispelling roll once per battle (even on a Fated Dispel).',
  },
  {
    name: 'Undead',
    effectKey: 'Undead',
    description:
      'Undead forces do not suffer from being Shaken or Diseased, but also are never Well Rested or Confident.',
  },
  {
    name: 'Fresh Corpses',
    effectKey: 'FreshCorpses',
    description: 'If in Towns, Castles, or Cities, the VC gain +D3 wounds to their Arise result for Infantry.',
  },
  {
    name: 'Northern Raiders',
    effectKey: 'NorthernRaiders',
    description: 'When Warriors of Chaos force does a pillage action, they gain two supply points rather than one.',
  },
  {
    name: 'Navigators of the Forests',
    effectKey: 'NavigatorsOfTheForests',
    description: 'Non-Tree Spirit units in forests or woods do not take dangerous terrain tests from burning woods.',
  },
  {
    name: 'Healed by Nature',
    effectKey: 'HealedByNature',
    description:
      'Tree Spirits with 50+% of their models in forests or woods can regain wounds or lost models during the command ' +
      'phase after the unit passes a successful leadership. Dryads can regain up to D3+1 wounds, Tree Kin can regain D3 ' +
      'wounds, and Treemen and Characters can regain 1 wound. They cannot gain wounds beyond their starting strength.',
  },
];

export const OLD_WORLD_FACTION_SPECIAL_RULES: Readonly<Record<string, readonly string[]>> = {
  'Beastmen Brayherds': ['Expert Ambushers'],
  'Kingdom of Bretonnia': ['Crusaders', 'Safe in Water'],
  'Chaos Dwarfs': ['Slavers'],
  'Daemons of Chaos': ['Divided We Stand'],
  'Dark Elves': ['Treacherous'],
  'Dwarfen Mountain Holds': ['It Is Going In The Book!', 'Rulers of Stone'],
  'Empire of Man': ['Prepared for Battle'],
  'Grand Cathay': ['The Art of War'],
  'High Elf Realms': ['Determined'],
  Lizardmen: ['Conduits of Power', 'Spawning Pools'],
  'Ogre Kingdoms': ['For Hire', 'Tough Guts'],
  'Orc & Goblin Tribes': ['The Green Tide'],
  'Renegade Crowns': ['Defenders of the Homeland', 'The Great City of Magritta'],
  Skaven: ['The Underground Network'],
  'Tomb Kings of Khemri': ['Called by the Relic', 'Relic of a Past Age', 'Undead'],
  'Vampire Counts': ['Fresh Corpses', 'Undead'],
  'Warriors of Chaos': ['Northern Raiders'],
  'Wood Elf Realms': ['Navigators of the Forests', 'Healed by Nature'],
};

export const OLD_WORLD_SUBFACTION_SPECIAL_RULES: Readonly<Record<string, Readonly<Record<string, readonly string[]>>>> =
  {
    'Daemons of Chaos': {
      Khorne: ['Only Blood Satisfies!'],
      Nurgle: ['Bringers of the Plague'],
      Slaanesh: ['Alluring'],
      Tzeentch: ['Magical Supply'],
    },
  };

export function specialRulesFromOldWorldPreset(): SpecialRulePreset[] {
  return OLD_WORLD_SPECIAL_RULES.map((rule) => ({
    name: rule.name,
    description: rule.description,
    effectKey: rule.effectKey,
  }));
}

export function specialRuleNamesForFaction(factionName: string): readonly string[] {
  return OLD_WORLD_FACTION_SPECIAL_RULES[factionName] ?? [];
}

export function specialRuleNamesForSubfaction(factionName: string, subfactionName: string): readonly string[] {
  if (!Object.hasOwn(OLD_WORLD_SUBFACTION_SPECIAL_RULES, factionName)) {
    return [];
  }

  const bySubfaction = OLD_WORLD_SUBFACTION_SPECIAL_RULES[factionName];
  if (!Object.hasOwn(bySubfaction, subfactionName)) {
    return [];
  }

  return bySubfaction[subfactionName];
}
