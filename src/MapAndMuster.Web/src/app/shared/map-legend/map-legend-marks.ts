import type {
  CampaignFaction,
  CampaignItemObjectiveType,
  CampaignStructureType,
} from '../../core/campaigns/campaign.models';
import { resolveFactionAppearance } from '../../core/campaigns/faction-appearance';
import type { MapLegendFactionMark, MapLegendItemMark, MapLegendStructureMark } from './map-legend.component';

export function representedLegendFactions(options: {
  territories: readonly { ownerFactionId?: string | null; spawnFactionId?: string | null }[];
  forceFactionIds: readonly string[];
  factions: readonly CampaignFaction[];
  flagImageUrl: (factionId: string, subfaction?: string | null) => string | null;
  failedFlagUrls?: ReadonlySet<string>;
}): MapLegendFactionMark[] {
  const used = new Set<string>(options.forceFactionIds);
  for (const territory of options.territories) {
    if (territory.ownerFactionId) {
      used.add(territory.ownerFactionId);
    }

    if (territory.spawnFactionId) {
      used.add(territory.spawnFactionId);
    }
  }

  return options.factions
    .filter((faction) => used.has(faction.id))
    .map((faction) => {
      const appearance = resolveFactionAppearance(faction, null);
      const flagUrl = appearance.hasFlagImage ? options.flagImageUrl(faction.id, null) : null;
      return {
        id: faction.id,
        name: faction.name,
        color: appearance.color,
        image: flagUrl && !options.failedFlagUrls?.has(flagUrl) ? flagUrl : null,
        tint: appearance.tint,
      };
    });
}

export function representedLegendStructures(options: {
  territories: readonly { structureTypeId?: string | null; structureCondition?: string | null }[];
  structures: readonly CampaignStructureType[];
  structureImageUrl: (structureTypeId: string, pillaged?: boolean) => string | null;
}): MapLegendStructureMark[] {
  const used = new Set<string>();
  for (const territory of options.territories) {
    if (territory.structureTypeId && territory.structureCondition !== 'Destroyed') {
      used.add(territory.structureTypeId);
    }
  }

  return options.structures
    .filter((structure) => used.has(structure.id))
    .map((structure) => ({
      id: structure.id,
      name: structure.name,
      builtinSymbol: structure.builtinSymbol,
      hasImage: structure.hasImage,
      hasPillagedImage: structure.hasPillagedImage,
      isPillageable: structure.isPillageable,
      imageUrl: structure.hasImage ? options.structureImageUrl(structure.id, false) : null,
      pillagedImageUrl: structure.hasPillagedImage ? options.structureImageUrl(structure.id, true) : null,
    }));
}

export function legendPillageSample(structures: readonly MapLegendStructureMark[]): MapLegendStructureMark | null {
  return structures.find((structure) => structure.isPillageable) ?? structures.at(0) ?? null;
}

export function representedLegendItems(options: {
  itemObjectiveTypes: readonly CampaignItemObjectiveType[];
  items: readonly {
    name: string;
    hidden?: boolean;
    builtinSymbol?: string;
    color?: string;
    imageUrl?: string | null;
  }[];
  heldItems: readonly { name: string; builtinSymbol: string; color: string; imageUrl: string | null }[];
  itemImageUrl: (typeId: string) => string | null;
}): MapLegendItemMark[] {
  const seen = new Map<string, MapLegendItemMark>();
  const add = (
    name: string,
    builtinSymbol: string | undefined,
    color: string | undefined,
    imageUrl: string | null,
  ): void => {
    if (!seen.has(name)) {
      seen.set(name, {
        name,
        builtinSymbol: builtinSymbol ?? 'Crown',
        color: color ?? '#C45C26',
        imageUrl,
      });
    }
  };

  for (const type of options.itemObjectiveTypes) {
    const represented =
      options.items.some((item) => !item.hidden && item.name === type.name) ||
      options.heldItems.some((held) => held.name === type.name);
    if (represented) {
      add(type.name, type.builtinSymbol, type.color, type.hasImage ? options.itemImageUrl(type.id) : null);
    }
  }

  for (const item of options.items) {
    if (item.hidden) {
      continue;
    }

    add(item.name, item.builtinSymbol, item.color, item.imageUrl ?? null);
  }

  for (const held of options.heldItems) {
    add(held.name, held.builtinSymbol, held.color, held.imageUrl);
  }

  return [...seen.values()].sort((left, right) => left.name.localeCompare(right.name));
}
