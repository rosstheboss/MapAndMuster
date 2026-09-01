import type { CampaignFaction, CampaignStructureType, CampaignTerrainType } from '../campaigns/campaign.models';
import { territoryLabel, type MapTerritory } from './map-graph.models';

export interface TerritoryTooltipForce {
  id: string;
  territoryId: string;
  name: string;
  inBattle: boolean;
}

export interface TerritoryTooltipBattle {
  territoryId: string;
  status: string;
  participantForceIds: readonly string[];
  winnerForceId: string | null;
  isDraw: boolean;
  isNoContest?: boolean;
}

const OPEN_BATTLE_STATUSES = new Set(['Pending', 'AwaitingResults', 'Disputed']);
const RESOLVED_BATTLE_STATUSES = new Set(['Finalized', 'GMResolved']);

export function territoryHoverTooltip(
  territory: MapTerritory,
  options: {
    factions: readonly Pick<CampaignFaction, 'id' | 'name'>[];
    terrainTypes: readonly Pick<CampaignTerrainType, 'id' | 'name'>[];
    structures: readonly Pick<CampaignStructureType, 'id' | 'name'>[];
    forces?: readonly TerritoryTooltipForce[];
    battles?: readonly TerritoryTooltipBattle[];
  },
): string {
  const owner = territory.ownerFactionId
    ? (options.factions.find((faction) => faction.id === territory.ownerFactionId)?.name ?? 'Unknown faction')
    : 'Neutral';
  const terrain = options.terrainTypes.find((type) => type.id === territory.terrainTypeId)?.name ?? 'None';
  const structure =
    territory.structureTypeId && territory.structureCondition !== 'Destroyed'
      ? (options.structures.find((type) => type.id === territory.structureTypeId) ?? null)
      : null;
  const structureLabel = structure
    ? territory.structureCondition === 'Pillaged'
      ? `${structure.name} (pillaged)`
      : structure.name
    : null;
  const forcesHere = (options.forces ?? []).filter((force) => force.territoryId === territory.id);
  const battlesHere = (options.battles ?? []).filter((battle) => battle.territoryId === territory.id);
  const retreating = retreatingForces(forcesHere, battlesHere);
  const retreatingIds = new Set(retreating.map((force) => force.id));
  const openBattle =
    battlesHere.some((battle) => OPEN_BATTLE_STATUSES.has(battle.status)) ||
    forcesHere.some((force) => force.inBattle && !retreatingIds.has(force.id));
  const lines = [
    territoryLabel(territory),
    `Owner: ${owner}`,
    ...(structureLabel ? [structureLabel] : []),
    `Terrain: ${terrain}`,
    `Forces: ${forcesHere.map((force) => force.name).join(', ') || 'None'}`,
  ];
  if (openBattle) {
    lines.push('Battle');
  }

  if (retreating.length > 0) {
    lines.push(`Retreating: ${retreating.map((force) => force.name).join(', ')}`);
  }

  return lines.join('\n');
}

function retreatingForces(
  forces: readonly TerritoryTooltipForce[],
  battles: readonly TerritoryTooltipBattle[],
): TerritoryTooltipForce[] {
  const ids = new Set<string>();
  for (const battle of battles) {
    if (!RESOLVED_BATTLE_STATUSES.has(battle.status)) {
      continue;
    }

    for (const force of forces) {
      if (!battle.participantForceIds.includes(force.id)) {
        continue;
      }

      if (battle.isDraw || battle.isNoContest || battle.winnerForceId !== force.id) {
        ids.add(force.id);
      }
    }
  }

  return forces.filter((force) => ids.has(force.id));
}
