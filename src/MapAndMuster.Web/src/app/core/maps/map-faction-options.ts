import type { CampaignFaction, CampaignSpecialRule } from '../campaigns/campaign.models';

export const NO_FIXED_SPAWN_EFFECT_KEY = 'UndergroundNetwork';

export const ALTERNATE_PLACEMENT_EFFECT_KEYS = ['UndergroundNetwork', 'GreatCityOfMagritta'] as const;

/** Marker stored for a neutral spawn. It is not a campaign faction. */
export const GENERAL_SPAWN_FACTION_ID = '00000000-0000-4000-8000-000000000001';

export function isGeneralSpawnFactionId(factionId: string | null | undefined): boolean {
  return factionId === GENERAL_SPAWN_FACTION_ID;
}

const OPTION_SEPARATOR = '::';

export interface MapFactionOption {
  value: string;
  factionId: string;
  subfaction: string | null;
  label: string;
  spawnDisabled: boolean;
}

export function mapFactionOptionValue(factionId: string, subfaction: string | null | undefined): string {
  return subfaction ? `${factionId}${OPTION_SEPARATOR}${subfaction}` : factionId;
}

export function parseMapFactionOptionValue(value: string): { factionId: string; subfaction: string | null } {
  const separator = value.indexOf(OPTION_SEPARATOR);
  if (separator <= 0) {
    return { factionId: value, subfaction: null };
  }

  return {
    factionId: value.slice(0, separator),
    subfaction: value.slice(separator + OPTION_SEPARATOR.length) || null,
  };
}

export function mapFactionOptionLabel(
  factions: readonly CampaignFaction[],
  factionId: string | null | undefined,
  subfaction: string | null | undefined,
): string {
  if (!factionId || isGeneralSpawnFactionId(factionId)) {
    return 'Neutral';
  }

  const faction = factions.find((item) => item.id === factionId);
  if (!faction) {
    return 'Unknown faction';
  }

  return subfaction ? `${faction.name} - ${subfaction}` : faction.name;
}

export function mapFactionOptions(campaign: {
  factions: readonly CampaignFaction[];
  specialRules?: readonly CampaignSpecialRule[];
}): MapFactionOption[] {
  const noSpawnRuleIds = new Set(
    (campaign.specialRules ?? []).filter((rule) => rule.effectKey === NO_FIXED_SPAWN_EFFECT_KEY).map((rule) => rule.id),
  );
  const factions = [...campaign.factions].sort((left, right) => left.name.localeCompare(right.name));
  const options: MapFactionOption[] = [];

  for (const faction of factions) {
    const factionHasNoSpawn = (faction.specialRuleIds ?? []).some((id) => noSpawnRuleIds.has(id));
    if (faction.requiresSubfaction) {
      const names = [...faction.subfactions]
        .map((name) => name.trim())
        .filter((name) => name.length > 0)
        .sort((left, right) => left.localeCompare(right));
      for (const name of names) {
        const assigned = faction.subfactionSpecialRules?.find((item) => item.name === name)?.specialRuleIds ?? [];
        options.push({
          value: mapFactionOptionValue(faction.id, name),
          factionId: faction.id,
          subfaction: name,
          label: `${faction.name} - ${name}`,
          spawnDisabled: factionHasNoSpawn || assigned.some((id) => noSpawnRuleIds.has(id)),
        });
      }
      continue;
    }

    options.push({
      value: faction.id,
      factionId: faction.id,
      subfaction: null,
      label: faction.name,
      spawnDisabled: factionHasNoSpawn,
    });
  }

  return options;
}

export function playerFactionOptions(factions: readonly CampaignFaction[]): MapFactionOption[] {
  const sorted = [...factions].sort((left, right) => left.name.localeCompare(right.name));
  const options: MapFactionOption[] = [];

  for (const faction of sorted) {
    const names = [...faction.subfactions]
      .map((name) => name.trim())
      .filter((name) => name.length > 0)
      .sort((left, right) => left.localeCompare(right));
    if (!faction.requiresSubfaction) {
      options.push({
        value: faction.id,
        factionId: faction.id,
        subfaction: null,
        label: faction.name,
        spawnDisabled: false,
      });
    }

    for (const name of names) {
      options.push({
        value: mapFactionOptionValue(faction.id, name),
        factionId: faction.id,
        subfaction: name,
        label: `${faction.name} - ${name}`,
        spawnDisabled: false,
      });
    }
  }

  return options;
}

export function missingFixedSpawnMessage(
  randomSpawnLocations: boolean,
  factions: readonly CampaignFaction[],
  specialRules: readonly { id: string; effectKey?: string | null }[],
  territories: readonly { spawnFactionId: string | null; spawnSubfaction?: string | null }[],
): string | null {
  if (randomSpawnLocations || territories.some((territory) => isGeneralSpawnFactionId(territory.spawnFactionId))) {
    return null;
  }

  const alternateRuleIds = new Set(
    specialRules
      .filter((rule) => ALTERNATE_PLACEMENT_EFFECT_KEYS.some((effectKey) => rule.effectKey === effectKey))
      .map((rule) => rule.id),
  );
  const missing: string[] = [];
  for (const faction of [...factions].sort((left, right) => left.name.localeCompare(right.name))) {
    const factionSkips = (faction.specialRuleIds ?? []).some((id) => alternateRuleIds.has(id));
    if (factionSkips) {
      continue;
    }

    if (faction.requiresSubfaction) {
      const names = [...faction.subfactions]
        .map((name) => name.trim())
        .filter((name) => name.length > 0)
        .sort((left, right) => left.localeCompare(right));
      for (const name of names) {
        const assignedRules =
          faction.subfactionSpecialRules?.find((item) => item.name.toLowerCase() === name.toLowerCase())
            ?.specialRuleIds ?? [];
        if (assignedRules.some((id) => alternateRuleIds.has(id))) {
          continue;
        }

        const hasSpawn = territories.some(
          (territory) =>
            territory.spawnFactionId === faction.id &&
            (territory.spawnSubfaction ?? '').trim().toLowerCase() === name.toLowerCase(),
        );
        if (!hasSpawn) {
          missing.push(`${faction.name} - ${name}`);
        }
      }

      continue;
    }

    if (!territories.some((territory) => territory.spawnFactionId === faction.id)) {
      missing.push(faction.name);
    }
  }

  if (missing.length === 0) {
    return null;
  }

  const verb = missing.length === 1 ? 'has' : 'have';
  return `No neutral spawn locations exist, and ${missing.join(', ')} ${verb} no specific spawn location.`;
}

export function spawnIdentity(
  factionId: string | null | undefined,
  subfaction: string | null | undefined,
): string | null {
  if (!factionId) {
    return null;
  }

  return mapFactionOptionValue(factionId, subfaction);
}
