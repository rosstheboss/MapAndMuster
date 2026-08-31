import type { CampaignFaction, SubfactionAppearance, SubfactionFlagSource } from './campaign.models';

export type { SubfactionFlagSource };

export interface ResolvedFactionAppearance {
  color: string;
  hasFlagImage: boolean;
  tint: boolean;
}

export function resolveFactionAppearance(
  faction: CampaignFaction | null | undefined,
  subfactionName?: string | null,
): ResolvedFactionAppearance {
  if (!faction) {
    return { color: '#44403c', hasFlagImage: false, tint: false };
  }

  const appearance = findSubfactionAppearance(faction, subfactionName);
  const color = appearance?.color?.trim() ? appearance.color : faction.color;
  const source = appearance?.flagSource ?? 'inherit';
  if (source === 'color') {
    return { color, hasFlagImage: false, tint: false };
  }

  if (source === 'image') {
    if (appearance?.hasFlagImage) {
      return { color, hasFlagImage: true, tint: appearance.tintFlagImage === true };
    }

    return {
      color,
      hasFlagImage: faction.hasFlagImage,
      tint: faction.tintFlagImage === true,
    };
  }

  return {
    color,
    hasFlagImage: faction.hasFlagImage,
    tint: faction.tintFlagImage === true,
  };
}

export function findSubfactionAppearance(
  faction: CampaignFaction,
  subfactionName?: string | null,
): SubfactionAppearance | null {
  const wanted = subfactionName?.trim();
  if (!wanted) {
    return null;
  }

  return faction.subfactionAppearances?.find((item) => item.name.toLowerCase() === wanted.toLowerCase()) ?? null;
}
