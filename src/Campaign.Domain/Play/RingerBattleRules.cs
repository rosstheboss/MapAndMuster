using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Common;

namespace Campaign.Domain.Play;

/// <summary>
/// Ephemeral GM ringer battles. The ringer is not a map force and leaves no trace.
/// </summary>
public static class RingerBattleRules
{
    /// <summary>
    /// Starts a ringer fight against an idle player force in an open battle phase.
    /// </summary>
    public static bool TryInject(
        CampaignPlayState state,
        PlayMap map,
        Guid gmUserId,
        Guid targetForceId,
        Guid ringerFactionId,
        Guid? missionId,
        bool playerIsDefender,
        IReadOnlyList<TerrainTypeSetup> terrainTypes,
        IReadOnlyList<StructureTypeSetup> structureTypes,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(terrainTypes);
        ArgumentNullException.ThrowIfNull(structureTypes);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(pickIndex);
        next = null;
        var window = state.CurrentWindow();
        if (window is not { Kind: RoundPhaseKind.Battle, Status: PhaseWindowStatus.Open } || utcNow >= window.EndsUtc)
        {
            error = new DomainError("ringer.window.closed", "A ringer battle can only be started during an open battle phase.");
            return false;
        }

        var force = state.Forces.FirstOrDefault(item => item.Id == targetForceId);
        if (force is null)
        {
            error = new DomainError("ringer.force.invalid", "That force was not found.");
            return false;
        }

        if (force.ControllerUserId == gmUserId)
        {
            error = new DomainError("ringer.own_force", "A ringer battle cannot target your own player force.");
            return false;
        }

        if (force.InBattle || state.Battles.Any(item =>
                item.BattleWindowId == window.Id
                && item.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved
                && item.ParticipantForceIds.Contains(force.Id)))
        {
            error = new DomainError("ringer.force.engaged", "Choose a force that is not currently in a battle.");
            return false;
        }

        var territory = map.Territory(force.TerritoryId);
        if (territory is null || territory.IsSpawn)
        {
            error = new DomainError("ringer.spawn", "Ringer battles cannot be fought on a spawn territory.");
            return false;
        }

        var occupants = state.Forces.Where(item => item.TerritoryId == force.TerritoryId).ToArray();
        if (occupants.Any(item =>
                item.Id != force.Id
                && ActionResolution.AreEnemies(item.FactionId, force.FactionId, factionAllyGroups, state.BrokenAllyFactionIds)))
        {
            error = new DomainError("ringer.force.engaged", "Choose a force that is not currently in a battle.");
            return false;
        }

        var pool = BattleMissionRules.PoolFor(territory, terrainTypes, structureTypes);
        Guid? chosenMission = missionId;
        if (chosenMission is { } specified)
        {
            var catalog = terrainTypes.SelectMany(static type => type.Missions)
                .Concat(structureTypes.SelectMany(static type => type.Missions))
                .Select(static mission => mission.Id)
                .ToHashSet();
            if (!catalog.Contains(specified) && pool.All(mission => mission.Id != specified))
            {
                error = new DomainError("ringer.mission.invalid", "Choose a mission from this campaign.", "missionId");
                return false;
            }
        }
        else if (pool.Count > 0)
        {
            chosenMission = pool[Math.Clamp(pickIndex(pool.Count), 0, pool.Count - 1)].Id;
        }

        var battle = new CampaignBattle(
            Guid.NewGuid(),
            force.TerritoryId,
            window.Id,
            window.Id,
            BattleStatus.AwaitingResults,
            [force.Id],
            winnerForceId: null,
            isDraw: false,
            utcNow,
            missionId: chosenMission,
            attackerForceId: playerIsDefender ? null : force.Id,
            defenderForceId: playerIsDefender ? force.Id : null,
            isRinger: true,
            ringerFactionId: ringerFactionId,
            initiatingGmUserId: gmUserId,
            ringerIsAttacker: playerIsDefender);
        var forces = state.Forces.Select(item => item.Id == force.Id ? item.With(inBattle: true) : item).ToArray();
        next = state
            .With(forces: forces, battles: [.. state.Battles, battle])
            .AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.RingerBattleCreated,
                window.Id,
                force.Id,
                gmUserId,
                force.TerritoryId,
                targetTerritoryId: null,
                battle.Id,
                ActionKind.Battle,
                [force.Id]));
        error = null;
        return true;
    }

    /// <summary>
    /// Ringer supply: currently owned terrain and operational structures treated as connected,
    /// plus the round free-supply bonus. No split penalty and no temporary pool.
    /// </summary>
    public static int MapSupply(PlayMap map, SupplyCatalog catalog, Guid factionId)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(catalog);
        var total = 0;
        foreach (var territory in map.Territories)
        {
            if (territory.OwnerFactionId != factionId)
            {
                continue;
            }

            if (territory.TerrainTypeId is { } terrainId
                && catalog.TerrainSupplyByType.TryGetValue(terrainId, out var terrainSupply))
            {
                total += terrainSupply;
            }

            if (territory.StructureTypeId is { } structureId
                && territory.StructureCondition == StructureCondition.Operational
                && catalog.Structures.TryGetValue(structureId, out var structure))
            {
                total += structure.SupplyPoints;
            }
        }

        return total;
    }
}
