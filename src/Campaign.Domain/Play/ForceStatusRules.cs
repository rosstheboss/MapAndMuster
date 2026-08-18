using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// Applies configured force-status enable and clear triggers. A force has at most one status;
/// Normal is stored as no status name. Faction exceptions remain display-only.
/// </summary>
public static class ForceStatusRules
{
    /// <summary>
    /// Resolution facts used to enable or clear a force status.
    /// </summary>
    public readonly struct Facts
    {
        /// <summary>Initializes facts for one force after a window resolves.</summary>
        public Facts(
            bool held,
            bool moved,
            bool foughtBattle,
            bool won,
            bool lost,
            bool retreated,
            bool occupiesWater)
        {
            Held = held;
            Moved = moved;
            FoughtBattle = foughtBattle;
            Won = won;
            Lost = lost;
            Retreated = retreated;
            OccupiesWater = occupiesWater;
        }

        /// <summary>Gets whether the force Held.</summary>
        public bool Held { get; }

        /// <summary>Gets whether the force Moved or Splits.</summary>
        public bool Moved { get; }

        /// <summary>Gets whether the force fought a resolved battle.</summary>
        public bool FoughtBattle { get; }

        /// <summary>Gets whether the force won a resolved battle.</summary>
        public bool Won { get; }

        /// <summary>Gets whether the force lost a resolved battle.</summary>
        public bool Lost { get; }

        /// <summary>Gets whether the force was forced to retreat.</summary>
        public bool Retreated { get; }

        /// <summary>Gets whether the force occupies a water-feature territory.</summary>
        public bool OccupiesWater { get; }
    }

    /// <summary>
    /// Applies catalog statuses to forces using per-force facts. Current statuses that have not
    /// met their clear trigger are kept; otherwise the first matching enable in catalog order wins.
    /// </summary>
    public static IReadOnlyList<CampaignForce> Apply(
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<ForceStatusSetup> statuses,
        IReadOnlyDictionary<Guid, Facts> factsByForceId)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(statuses);
        ArgumentNullException.ThrowIfNull(factsByForceId);
        if (statuses.Count == 0)
        {
            return forces;
        }

        var byName = statuses.ToDictionary(static status => status.Name, StringComparer.OrdinalIgnoreCase);
        return
        [
            .. forces.Select(force =>
            {
                if (!factsByForceId.TryGetValue(force.Id, out var facts))
                {
                    return force;
                }

                var current = force.StatusName is { } name && byName.TryGetValue(name, out var status)
                    ? status
                    : null;
                string? next = force.StatusName;
                if (current is not null && MatchesClear(current.ClearTrigger, facts))
                {
                    next = null;
                }

                if (next is null)
                {
                    foreach (var candidate in statuses)
                    {
                        if (MatchesEnable(candidate.EnableTrigger, facts))
                        {
                            next = candidate.Name;
                            break;
                        }
                    }
                }

                return string.Equals(next, force.StatusName, StringComparison.Ordinal)
                    ? force
                    : force.WithStatus(next);
            }),
        ];
    }

    /// <summary>
    /// Builds action-window facts from the latest submission and the force's territory.
    /// </summary>
    public static Facts FromAction(ActionKind? kind, bool occupiesWater)
    {
        var held = kind is null or ActionKind.Hold;
        var moved = kind is ActionKind.Move or ActionKind.Split or ActionKind.Retreat;
        return new Facts(held, moved, false, false, false, false, occupiesWater);
    }

    /// <summary>
    /// Builds battle-window facts from finalized engagements and retreats.
    /// </summary>
    public static Facts FromBattle(bool fought, bool won, bool lost, bool retreated, bool occupiesWater)
    {
        return new Facts(false, false, fought, won, lost, retreated, occupiesWater);
    }

    private static bool MatchesEnable(ForceStatusEnableTrigger trigger, Facts facts)
    {
        return trigger switch
        {
            ForceStatusEnableTrigger.Hold => facts.Held,
            ForceStatusEnableTrigger.AfterBattle => facts.FoughtBattle,
            ForceStatusEnableTrigger.BattleWon => facts.Won,
            ForceStatusEnableTrigger.BattleLostOrRetreat => facts.Lost || facts.Retreated,
            ForceStatusEnableTrigger.OccupyingWater => facts.OccupiesWater,
            _ => false,
        };
    }

    private static bool MatchesClear(ForceStatusClearTrigger trigger, Facts facts)
    {
        return trigger switch
        {
            ForceStatusClearTrigger.Hold => facts.Held,
            ForceStatusClearTrigger.AfterMove => facts.Moved,
            ForceStatusClearTrigger.AfterBattle => facts.FoughtBattle,
            ForceStatusClearTrigger.AfterMoveOrBattle => facts.Moved || facts.FoughtBattle,
            ForceStatusClearTrigger.BattleWon => facts.Won,
            ForceStatusClearTrigger.BattleLostOrRetreat => facts.Lost || facts.Retreated,
            ForceStatusClearTrigger.HoldWhileNotWater => facts.Held && !facts.OccupiesWater,
            _ => false,
        };
    }
}
