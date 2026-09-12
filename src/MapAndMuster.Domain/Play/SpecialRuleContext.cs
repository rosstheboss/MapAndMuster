using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Campaign special-rule catalog plus faction and subfaction assignments used during resolution.
/// </summary>
public sealed class SpecialRuleContext
{
    /// <summary>
    /// Initializes a special-rule context.
    /// </summary>
    public SpecialRuleContext(
        IReadOnlyList<SpecialRuleSetup> catalog,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> factionRuleIds,
        IReadOnlyDictionary<(Guid FactionId, string Subfaction), IReadOnlyList<Guid>> subfactionRuleIds,
        IReadOnlySet<Guid>? requiresSubfactionFactionIds = null,
        IReadOnlyDictionary<Guid, int>? factionMovementSpeeds = null,
        IReadOnlyDictionary<(Guid FactionId, string Subfaction), int>? subfactionMovementSpeeds = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>? forceItemRuleIds = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<ItemObjectiveEffectSetup>>? itemEffectsByTypeId = null,
        IReadOnlyDictionary<Guid, string>? forceStatusNames = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(factionRuleIds);
        ArgumentNullException.ThrowIfNull(subfactionRuleIds);
        Catalog = catalog;
        FactionRuleIds = factionRuleIds;
        SubfactionRuleIds = subfactionRuleIds;
        RequiresSubfactionFactionIds = requiresSubfactionFactionIds ?? new HashSet<Guid>();
        FactionMovementSpeeds = factionMovementSpeeds ?? new Dictionary<Guid, int>();
        SubfactionMovementSpeeds = subfactionMovementSpeeds ?? new Dictionary<(Guid, string), int>();
        ForceItemRuleIds = forceItemRuleIds ?? new Dictionary<Guid, IReadOnlyList<Guid>>();
        ItemEffectsByTypeId = itemEffectsByTypeId ?? new Dictionary<Guid, IReadOnlyList<ItemObjectiveEffectSetup>>();
        ForceStatusNames = forceStatusNames ?? new Dictionary<Guid, string>();
        EffectById = catalog
            .Where(static rule => SpecialRuleEffectKeys.IsKnown(rule.EffectKey))
            .ToDictionary(static rule => rule.Id, static rule => rule.EffectKey!, EqualityComparer<Guid>.Default);
    }

    /// <summary>Gets an empty context with no mechanical effects.</summary>
    public static SpecialRuleContext None { get; } = new(
        [],
        new Dictionary<Guid, IReadOnlyList<Guid>>(),
        new Dictionary<(Guid, string), IReadOnlyList<Guid>>());

    /// <summary>Gets the reusable special-rule catalog.</summary>
    public IReadOnlyList<SpecialRuleSetup> Catalog { get; }

    /// <summary>Gets special-rule identifiers assigned to each faction.</summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> FactionRuleIds { get; }

    /// <summary>Gets special-rule identifiers assigned to each faction subfaction.</summary>
    public IReadOnlyDictionary<(Guid FactionId, string Subfaction), IReadOnlyList<Guid>> SubfactionRuleIds { get; }

    /// <summary>Gets factions that must choose a named subfaction.</summary>
    public IReadOnlySet<Guid> RequiresSubfactionFactionIds { get; }

    /// <summary>Gets faction movement speeds. Missing entries use the default of 1.</summary>
    public IReadOnlyDictionary<Guid, int> FactionMovementSpeeds { get; }

    /// <summary>Gets subfaction movement-speed overrides.</summary>
    public IReadOnlyDictionary<(Guid FactionId, string Subfaction), int> SubfactionMovementSpeeds { get; }

    /// <summary>Gets special-rule identifiers granted by items each force currently holds.</summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> ForceItemRuleIds { get; }

    /// <summary>Gets parameterized effects keyed by item-objective type.</summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<ItemObjectiveEffectSetup>> ItemEffectsByTypeId { get; }

    /// <summary>Gets catalog force-status names keyed by type identifier.</summary>
    public IReadOnlyDictionary<Guid, string> ForceStatusNames { get; }

    private IReadOnlyDictionary<Guid, string> EffectById { get; }

    /// <summary>Returns whether the faction must take a subfaction.</summary>
    public bool FactionRequiresSubfaction(Guid factionId)
    {
        return RequiresSubfactionFactionIds.Contains(factionId);
    }

    /// <summary>Returns whether the force has a mechanical special rule.</summary>
    public bool Has(CampaignForce force, string effectKey)
    {
        ArgumentNullException.ThrowIfNull(force);
        if (Has(force.FactionId, force.Subfaction, effectKey))
        {
            return true;
        }

        if (!ForceItemRuleIds.TryGetValue(force.Id, out var itemIds))
        {
            return false;
        }

        foreach (var id in itemIds)
        {
            if (EffectById.TryGetValue(id, out var key)
                && string.Equals(key, effectKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Configured movement speed for the force before item and Called by the Relic bonuses.</summary>
    public int MovementSpeedFor(CampaignForce force)
    {
        ArgumentNullException.ThrowIfNull(force);
        if (!string.IsNullOrWhiteSpace(force.Subfaction)
            && TrySubfactionSpeed(force.FactionId, force.Subfaction, out var subSpeed))
        {
            return subSpeed;
        }

        return FactionMovementSpeeds.TryGetValue(force.FactionId, out var speed)
            ? Math.Clamp(speed, ForceMovementSpeeds.Min, ForceMovementSpeeds.Max)
            : ForceMovementSpeeds.Default;
    }

    /// <summary>Parameterized effects configured on an item-objective type.</summary>
    public IReadOnlyList<ItemObjectiveEffectSetup> EffectsForItemType(Guid typeId)
    {
        return ItemEffectsByTypeId.TryGetValue(typeId, out var effects) ? effects : [];
    }

    /// <summary>Catalog status name for a force-status type identifier.</summary>
    public string? ForceStatusName(Guid statusTypeId)
    {
        return ForceStatusNames.TryGetValue(statusTypeId, out var name) ? name : null;
    }

    /// <summary>Returns whether a faction, optionally with a subfaction, has a mechanical special rule.</summary>
    public bool Has(Guid factionId, string? subfaction, string effectKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectKey);
        foreach (var id in AssignedIds(factionId, subfaction))
        {
            if (EffectById.TryGetValue(id, out var key)
                && string.Equals(key, effectKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns whether any assigned force of the faction has the effect.</summary>
    public bool FactionHas(Guid factionId, string effectKey)
    {
        return Has(factionId, null, effectKey);
    }

    /// <summary>Returns whether any catalog assignment in the campaign has the effect.</summary>
    public bool AnyoneHas(string effectKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectKey);
        return EffectById.Values.Any(key => string.Equals(key, effectKey, StringComparison.OrdinalIgnoreCase))
            && (FactionRuleIds.Values.SelectMany(static ids => ids).Any(EffectById.ContainsKey)
                || SubfactionRuleIds.Values.SelectMany(static ids => ids).Any(EffectById.ContainsKey)
                || ForceItemRuleIds.Values.SelectMany(static ids => ids).Any(EffectById.ContainsKey));
    }

    /// <summary>Returns whether any player in the campaign is assigned For Hire.</summary>
    public bool OgreMercenariesAvailable()
    {
        foreach (var pair in FactionRuleIds)
        {
            if (Has(pair.Key, null, SpecialRuleEffectKeys.ForHire))
            {
                return true;
            }
        }

        return false;
    }

    private bool TrySubfactionSpeed(Guid factionId, string subfaction, out int speed)
    {
        if (SubfactionMovementSpeeds.TryGetValue((factionId, subfaction), out speed))
        {
            speed = Math.Clamp(speed, ForceMovementSpeeds.Min, ForceMovementSpeeds.Max);
            return true;
        }

        foreach (var pair in SubfactionMovementSpeeds)
        {
            if (pair.Key.FactionId == factionId
                && string.Equals(pair.Key.Subfaction, subfaction, StringComparison.OrdinalIgnoreCase))
            {
                speed = Math.Clamp(pair.Value, ForceMovementSpeeds.Min, ForceMovementSpeeds.Max);
                return true;
            }
        }

        speed = ForceMovementSpeeds.Default;
        return false;
    }

    private IEnumerable<Guid> AssignedIds(Guid factionId, string? subfaction)
    {
        if (FactionRuleIds.TryGetValue(factionId, out var factionIds))
        {
            foreach (var id in factionIds)
            {
                yield return id;
            }
        }

        if (string.IsNullOrWhiteSpace(subfaction))
        {
            yield break;
        }

        if (SubfactionRuleIds.TryGetValue((factionId, subfaction), out var subIds))
        {
            foreach (var id in subIds)
            {
                yield return id;
            }
        }
        else
        {
            foreach (var pair in SubfactionRuleIds)
            {
                if (pair.Key.FactionId == factionId
                    && string.Equals(pair.Key.Subfaction, subfaction, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var id in pair.Value)
                    {
                        yield return id;
                    }

                    yield break;
                }
            }
        }
    }
}

/// <summary>
/// One extra Move hop for a multi-territory Move.
/// </summary>
/// <param name="ViaTerritoryId">The first territory entered.</param>
/// <param name="TargetTerritoryId">The territory the force intends to end in.</param>
/// <param name="IntermediateTerritoryIds">Hops between the first via and the destination when speed is greater than 2.</param>
public sealed record MoveHop(
    Guid ViaTerritoryId,
    Guid TargetTerritoryId,
    IReadOnlyList<Guid> IntermediateTerritoryIds)
{
    /// <summary>A two-territory hop with no extra intermediates.</summary>
    public MoveHop(Guid viaTerritoryId, Guid targetTerritoryId)
        : this(viaTerritoryId, targetTerritoryId, [])
    {
    }
}

/// <summary>
/// A daemon-god or other subfaction that left its implicit alliance.
/// </summary>
/// <param name="FactionId">The parent faction.</param>
/// <param name="Subfaction">The subfaction that backstabbed.</param>
public sealed record BrokenAllySubfaction(Guid FactionId, string Subfaction);

/// <summary>
/// Name matching for catalog structures used by special-rule policies.
/// </summary>
public static class StructureKinds
{
    /// <summary>Returns whether the structure is a Town or City.</summary>
    public static bool IsTownOrCity(string? name) => Matches(name, "Town", "City");

    /// <summary>Returns whether the structure is a Town, City, Castle, or Capital City.</summary>
    public static bool IsSettlement(string? name) => Matches(name, "Town", "City", "Castle", "Capital City", "CapitalCity");

    /// <summary>Returns whether the structure is a Capital City.</summary>
    public static bool IsCapitalCity(string? name) => Matches(name, "Capital City", "CapitalCity");

    /// <summary>Returns whether the structure is a Supply Depot.</summary>
    public static bool IsSupplyDepot(string? name) => Matches(name, "Supply Depot", "SupplyDepot");

    /// <summary>
    /// Returns whether Hold on this structure can clear Diseased: Capital City, City, Supply Depot,
    /// or Town. Destroyed structures are excluded by the caller.
    /// </summary>
    public static bool CanCureDisease(string? name) =>
        Matches(name, "Capital City", "CapitalCity", "City", "Supply Depot", "SupplyDepot", "Town");

    /// <summary>Returns whether the structure is a Fortification.</summary>
    public static bool IsFortification(string? name) => Matches(name, "Fortification");

    /// <summary>Returns whether the structure is a City (not Capital City).</summary>
    public static bool IsCity(string? name) => Matches(name, "City");

    private static bool Matches(string? name, params string[] options)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        foreach (var option in options)
        {
            if (string.Equals(name.Trim(), option, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
