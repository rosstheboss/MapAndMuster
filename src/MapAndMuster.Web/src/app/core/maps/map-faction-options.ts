import type { CampaignFaction, CampaignSpecialRule } from '../campaigns/campaign.models';

export const NO_FIXED_SPAWN_EFFECT_KEY = 'UndergroundNetwork';

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
  if (!factionId) {
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

export function spawnIdentity(
  factionId: string | null | undefined,
  subfaction: string | null | undefined,
): string | null {
  if (!factionId) {
    return null;
  }

  return mapFactionOptionValue(factionId, subfaction);
}
