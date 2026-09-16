using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Authoritative runtime state for a launched campaign.
/// </summary>
public sealed class CampaignPlayState
{
    /// <summary>
    /// Initializes play state.
    /// </summary>
    public CampaignPlayState(
        IReadOnlyList<PhaseWindow> windows,
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<OrderDraft> drafts,
        IReadOnlyList<OrderSubmission> submissions,
        IReadOnlyList<PlayerCommitment> commitments,
        IReadOnlyList<CampaignBattle> battles,
        IReadOnlyList<BattleResultSubmission> battleSubmissions,
        IReadOnlyList<RetreatOrder> retreats,
        IReadOnlyList<Guid> brokenAllyFactionIds,
        IReadOnlyList<TerritoryStructureState> structures,
        IReadOnlyList<CampaignItemObjective> itemObjectives,
        IReadOnlyList<PlayLogEntry> log,
        IReadOnlyList<ActionWindowSnapshot>? snapshots = null,
        Guid? debugActorUserId = null,
        DateTimeOffset? debugStartedUtc = null,
        IReadOnlyList<PublicObjectiveAward>? publicObjectiveAwards = null,
        IReadOnlyList<PrivateObjectiveAssignment>? privateObjectives = null,
        IReadOnlyList<StructureDestructionFact>? structureDestructions = null,
        IReadOnlyList<PlayerSupplyBalance>? playerSupplies = null,
        IReadOnlyList<ForceDelinquency>? delinquencies = null,
        IReadOnlyList<BrokenAllySubfaction>? brokenAllySubfactions = null,
        IReadOnlyList<ForceStatusChangeFact>? forceStatusChanges = null,
        IReadOnlyList<StructureWorkFact>? structureWorks = null,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null,
        IReadOnlyList<RivalObjectiveAssignment>? rivalObjectives = null,
        IReadOnlyList<BattleArmyListSubmission>? armyLists = null)
    {
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(drafts);
        ArgumentNullException.ThrowIfNull(submissions);
        ArgumentNullException.ThrowIfNull(commitments);
        ArgumentNullException.ThrowIfNull(battles);
        ArgumentNullException.ThrowIfNull(battleSubmissions);
        ArgumentNullException.ThrowIfNull(retreats);
        ArgumentNullException.ThrowIfNull(brokenAllyFactionIds);
        ArgumentNullException.ThrowIfNull(structures);
        ArgumentNullException.ThrowIfNull(itemObjectives);
        ArgumentNullException.ThrowIfNull(log);
        Windows = windows;
        Forces = forces;
        Drafts = drafts;
        Submissions = submissions;
        Commitments = commitments;
        Battles = battles;
        BattleSubmissions = battleSubmissions;
        Retreats = retreats;
        BrokenAllyFactionIds = brokenAllyFactionIds;
        Structures = structures;
        ItemObjectives = itemObjectives;
        Log = log;
        Snapshots = snapshots ?? [];
        DebugActorUserId = debugActorUserId;
        DebugStartedUtc = debugStartedUtc;
        PublicObjectiveAwards = publicObjectiveAwards ?? [];
        PrivateObjectives = privateObjectives ?? [];
        StructureDestructions = structureDestructions ?? [];
        PlayerSupplies = playerSupplies ?? [];
        Delinquencies = delinquencies ?? [];
        BrokenAllySubfactions = brokenAllySubfactions ?? [];
        ForceStatusChanges = forceStatusChanges ?? [];
        StructureWorks = structureWorks ?? [];
        AllyBetrayals = allyBetrayals ?? [];
        RivalObjectives = rivalObjectives ?? [];
        ArmyLists = armyLists ?? [];
    }

    /// <summary>Gets an empty play state.</summary>
    public static CampaignPlayState Empty { get; } = new([], [], [], [], [], [], [], [], [], [], [], []);

    /// <summary>Gets stored phase windows.</summary>
    public IReadOnlyList<PhaseWindow> Windows { get; }

    /// <summary>Gets forces.</summary>
    public IReadOnlyList<CampaignForce> Forces { get; }

    /// <summary>Gets current drafts.</summary>
    public IReadOnlyList<OrderDraft> Drafts { get; }

    /// <summary>Gets immutable order submissions.</summary>
    public IReadOnlyList<OrderSubmission> Submissions { get; }

    /// <summary>Gets current commitments.</summary>
    public IReadOnlyList<PlayerCommitment> Commitments { get; }

    /// <summary>Gets battles.</summary>
    public IReadOnlyList<CampaignBattle> Battles { get; }

    /// <summary>Gets immutable battle-result submissions.</summary>
    public IReadOnlyList<BattleResultSubmission> BattleSubmissions { get; }

    /// <summary>Gets retreats.</summary>
    public IReadOnlyList<RetreatOrder> Retreats { get; }

    /// <summary>Gets factions that left their ally group through Backstab.</summary>
    public IReadOnlyList<Guid> BrokenAllyFactionIds { get; }

    /// <summary>Gets structure conditions by territory.</summary>
    public IReadOnlyList<TerritoryStructureState> Structures { get; }

    /// <summary>Gets spawned item objectives. Hidden instances are omitted from unauthorized reads.</summary>
    public IReadOnlyList<CampaignItemObjective> ItemObjectives { get; }

    /// <summary>Gets public resolved-action and battle facts. Secret unrevealed orders are omitted.</summary>
    public IReadOnlyList<PlayLogEntry> Log { get; }

    /// <summary>Gets pre-resolution snapshots used by debug re-resolve.</summary>
    public IReadOnlyList<ActionWindowSnapshot> Snapshots { get; }

    /// <summary>Gets the manager currently in debug mode, if any.</summary>
    public Guid? DebugActorUserId { get; }

    /// <summary>Gets when the current debug session started, in UTC.</summary>
    public DateTimeOffset? DebugStartedUtc { get; }

    /// <summary>Gets public-objective award facts. Original awards are never overwritten.</summary>
    public IReadOnlyList<PublicObjectiveAward> PublicObjectiveAwards { get; }

    /// <summary>Gets assigned private objectives. Unrevealed details are omitted from unauthorized reads.</summary>
    public IReadOnlyList<PrivateObjectiveAssignment> PrivateObjectives { get; }

    /// <summary>Gets append-only facts for destroyed structures.</summary>
    public IReadOnlyList<StructureDestructionFact> StructureDestructions { get; }

    /// <summary>Gets remaining temporary supply per player.</summary>
    public IReadOnlyList<PlayerSupplyBalance> PlayerSupplies { get; }

    /// <summary>Gets campaign-lifetime missed-order offences per force.</summary>
    public IReadOnlyList<ForceDelinquency> Delinquencies { get; }

    /// <summary>Gets daemon-god (or other) subfactions that left their implicit alliance.</summary>
    public IReadOnlyList<BrokenAllySubfaction> BrokenAllySubfactions { get; }

    /// <summary>Gets append-only facts for force status gains and losses.</summary>
    public IReadOnlyList<ForceStatusChangeFact> ForceStatusChanges { get; }

    /// <summary>Gets append-only facts for successful Build and Repair actions.</summary>
    public IReadOnlyList<StructureWorkFact> StructureWorks { get; }

    /// <summary>
    /// Gets player-scoped Backstab betrayals. The traitor, not their faction, is treated as an
    /// enemy by the betrayed faction or scoped subfaction.
    /// </summary>
    public IReadOnlyList<AllyBetrayal> AllyBetrayals { get; }

    /// <summary>Gets secret rival assignments. Unrevealed details are omitted from unauthorized reads.</summary>
    public IReadOnlyList<RivalObjectiveAssignment> RivalObjectives { get; }

    /// <summary>Gets append-only army-list submissions, independent of battle-result agreement.</summary>
    public IReadOnlyList<BattleArmyListSubmission> ArmyLists { get; }

    /// <summary>
    /// Returns a copy with replaced collections.
    /// </summary>
    public CampaignPlayState With(
        IReadOnlyList<PhaseWindow>? windows = null,
        IReadOnlyList<CampaignForce>? forces = null,
        IReadOnlyList<OrderDraft>? drafts = null,
        IReadOnlyList<OrderSubmission>? submissions = null,
        IReadOnlyList<PlayerCommitment>? commitments = null,
        IReadOnlyList<CampaignBattle>? battles = null,
        IReadOnlyList<BattleResultSubmission>? battleSubmissions = null,
        IReadOnlyList<RetreatOrder>? retreats = null,
        IReadOnlyList<Guid>? brokenAllyFactionIds = null,
        IReadOnlyList<TerritoryStructureState>? structures = null,
        IReadOnlyList<CampaignItemObjective>? itemObjectives = null,
        IReadOnlyList<PlayLogEntry>? log = null,
        IReadOnlyList<ActionWindowSnapshot>? snapshots = null,
        Guid? debugActorUserId = null,
        DateTimeOffset? debugStartedUtc = null,
        bool clearDebug = false,
        IReadOnlyList<PublicObjectiveAward>? publicObjectiveAwards = null,
        IReadOnlyList<PrivateObjectiveAssignment>? privateObjectives = null,
        IReadOnlyList<StructureDestructionFact>? structureDestructions = null,
        IReadOnlyList<PlayerSupplyBalance>? playerSupplies = null,
        IReadOnlyList<ForceDelinquency>? delinquencies = null,
        IReadOnlyList<BrokenAllySubfaction>? brokenAllySubfactions = null,
        IReadOnlyList<ForceStatusChangeFact>? forceStatusChanges = null,
        IReadOnlyList<StructureWorkFact>? structureWorks = null,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null,
        IReadOnlyList<RivalObjectiveAssignment>? rivalObjectives = null,
        IReadOnlyList<BattleArmyListSubmission>? armyLists = null)
    {
        return new CampaignPlayState(
            windows ?? Windows,
            forces ?? Forces,
            drafts ?? Drafts,
            submissions ?? Submissions,
            commitments ?? Commitments,
            battles ?? Battles,
            battleSubmissions ?? BattleSubmissions,
            retreats ?? Retreats,
            brokenAllyFactionIds ?? BrokenAllyFactionIds,
            structures ?? Structures,
            itemObjectives ?? ItemObjectives,
            log ?? Log,
            snapshots ?? Snapshots,
            clearDebug ? null : debugActorUserId ?? DebugActorUserId,
            clearDebug ? null : debugStartedUtc ?? DebugStartedUtc,
            publicObjectiveAwards ?? PublicObjectiveAwards,
            privateObjectives ?? PrivateObjectives,
            structureDestructions ?? StructureDestructions,
            playerSupplies ?? PlayerSupplies,
            delinquencies ?? Delinquencies,
            brokenAllySubfactions ?? BrokenAllySubfactions,
            forceStatusChanges ?? ForceStatusChanges,
            structureWorks ?? StructureWorks,
            allyBetrayals ?? AllyBetrayals,
            rivalObjectives ?? RivalObjectives,
            armyLists ?? ArmyLists);
    }

    /// <summary>
    /// Appends public log facts without replacing earlier entries.
    /// </summary>
    public CampaignPlayState AppendLog(params PlayLogEntry[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Length == 0)
        {
            return this;
        }

        return With(log: [.. Log, .. entries]);
    }

    /// <summary>
    /// Evaluates lifecycle from stored windows. An unresolved window that is overdue remains current.
    /// </summary>
    public CampaignProgress Evaluate(DateTimeOffset startsUtc, DateTimeOffset endsUtc, DateTimeOffset utcNow)
    {
        if (Windows.Count == 0)
        {
            return utcNow < startsUtc
                ? new CampaignProgress(CampaignStatus.Scheduled, null, null, null, null, null)
                : new CampaignProgress(CampaignStatus.Completed, null, null, null, null, null);
        }

        if (utcNow < startsUtc)
        {
            return new CampaignProgress(CampaignStatus.Scheduled, null, null, null, null, null);
        }

        var current = Windows.FirstOrDefault(static window => window.Status != PhaseWindowStatus.Resolved);
        if (current is null)
        {
            return new CampaignProgress(CampaignStatus.Completed, null, null, null, null, null);
        }

        if (utcNow < current.StartsUtc)
        {
            return new CampaignProgress(
                CampaignStatus.InProgress,
                current.RoundNumber,
                current.PhaseNumber,
                current.Kind,
                current.StartsUtc,
                current.EndsUtc);
        }

        return new CampaignProgress(
            CampaignStatus.InProgress,
            current.RoundNumber,
            current.PhaseNumber,
            current.Kind,
            current.StartsUtc,
            current.EndsUtc);
    }

    /// <summary>
    /// The first unresolved window.
    /// </summary>
    public PhaseWindow? CurrentWindow()
    {
        return Windows.FirstOrDefault(static window => window.Status != PhaseWindowStatus.Resolved);
    }

    /// <summary>
    /// Players who currently owe an order in an open action window.
    /// </summary>
    public IReadOnlyList<Guid> RequiredOrderPlayers(Guid windowId)
    {
        _ = windowId;
        return
        [
            .. Forces
                .Where(force => !force.InBattle)
                .Select(static force => force.ControllerUserId)
                .Distinct(),
        ];
    }

    /// <summary>
    /// Players who have a force during an action window, including those whose forces are all in battle.
    /// </summary>
    public IReadOnlyList<Guid> ActionRosterPlayers()
    {
        return [.. Forces.Select(static force => force.ControllerUserId).Distinct()];
    }

    /// <summary>
    /// Whether the player has committed, or owes no action because every force is locked in battle
    /// or already preparing a random teleport.
    /// </summary>
    public bool IsActionCommitted(Guid windowId, Guid userId)
    {
        if (Commitments.Any(item => item.WindowId == windowId && item.UserId == userId))
        {
            return true;
        }

        var mine = Forces.Where(force => force.ControllerUserId == userId).ToArray();
        return mine.Length > 0
            && mine.All(static force => force.InBattle || force.PendingRandomTeleportDestinationId is not null);
    }

    /// <summary>
    /// Latest submission for a force in a window, if any.
    /// </summary>
    public OrderSubmission? LatestSubmission(Guid windowId, Guid forceId)
    {
        return Submissions
            .Where(item => item.WindowId == windowId && item.ForceId == forceId)
            .OrderByDescending(static item => item.SubmittedUtc)
            .FirstOrDefault();
    }

    /// <summary>
    /// Draft for a force in a window, if any.
    /// </summary>
    public OrderDraft? DraftFor(Guid windowId, Guid forceId)
    {
        return Drafts.FirstOrDefault(item => item.WindowId == windowId && item.ForceId == forceId);
    }

    /// <summary>
    /// Unique players who have a reporting force in a battle this window. Completed players
    /// stay in the list so the commitment denominator does not shrink.
    /// </summary>
    public IReadOnlyList<Guid> RequiredBattlePlayers(Guid windowId)
    {
        var userIds = new List<Guid>();
        foreach (var battle in Battles.Where(battle => battle.BattleWindowId == windowId))
        {
            foreach (var forceId in battle.ReportingForceIds)
            {
                AddController(forceId, userIds);
            }

            if (battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved)
            {
                continue;
            }

            foreach (var forceId in CampaignPlayRules.ForcesRequiredToRetreat(battle))
            {
                AddController(forceId, userIds);
            }
        }

        return [.. userIds.Distinct()];
    }

    /// <summary>
    /// Whether the player still owes a battle result or a required retreat in this window.
    /// </summary>
    public (bool NeedsResult, bool NeedsRetreat) PendingBattleDuties(Guid windowId, Guid userId)
    {
        var needsResult = false;
        var needsRetreat = false;
        var forceIds = Forces
            .Where(force => force.ControllerUserId == userId)
            .Select(static force => force.Id)
            .ToHashSet();
        foreach (var battle in Battles.Where(battle => battle.BattleWindowId == windowId))
        {
            if (battle.Status is BattleStatus.AwaitingResults or BattleStatus.Disputed
                && battle.ReportingForceIds.Any(forceIds.Contains)
                && LatestBattleSubmission(battle.Id, userId) is null)
            {
                var mineReporting = battle.ReportingForceIds.Where(forceIds.Contains).ToArray();
                if (mineReporting.Length == 0 || !mineReporting.All(battle.SurrenderedForceIds.Contains))
                {
                    needsResult = true;
                }
            }

            if (battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
            {
                foreach (var forceId in CampaignPlayRules.ForcesRequiredToRetreat(battle))
                {
                    if (forceIds.Contains(forceId) && !HasCommittedRetreat(battle.Id, forceId))
                    {
                        needsRetreat = true;
                    }
                }
            }
        }

        return (needsResult, needsRetreat);
    }

    /// <summary>
    /// Whether the player has finished every battle they are in: each result is submitted or
    /// staff-resolved, and every required retreat is committed.
    /// </summary>
    public bool HasCompletedBattleDuties(Guid windowId, Guid userId)
    {
        var pending = PendingBattleDuties(windowId, userId);
        return !pending.NeedsResult && !pending.NeedsRetreat;
    }

    /// <summary>
    /// Whether a committed retreat exists for this force in this battle.
    /// </summary>
    public bool HasCommittedRetreat(Guid battleId, Guid forceId)
    {
        return Retreats.Any(item => item.BattleId == battleId && item.ForceId == forceId && item.IsCommitted);
    }

    /// <summary>
    /// The latest stored retreat for this force in this battle, committed or still in draft.
    /// </summary>
    public RetreatOrder? RetreatFor(Guid battleId, Guid forceId)
    {
        return Retreats.FirstOrDefault(item => item.BattleId == battleId && item.ForceId == forceId);
    }

    private void AddController(Guid forceId, List<Guid> userIds)
    {
        var force = Forces.FirstOrDefault(item => item.Id == forceId);
        if (force is not null && !userIds.Contains(force.ControllerUserId))
        {
            userIds.Add(force.ControllerUserId);
        }
    }

    /// <summary>
    /// Latest battle submission for a user, if any.
    /// </summary>
    public BattleResultSubmission? LatestBattleSubmission(Guid battleId, Guid userId)
    {
        return BattleSubmissions
            .Where(item => item.BattleId == battleId && item.SubmitterUserId == userId)
            .OrderByDescending(static item => item.SubmittedUtc)
            .FirstOrDefault();
    }

    /// <summary>
    /// Returns the latest army list for a force in a battle, when any.
    /// </summary>
    public BattleArmyListSubmission? LatestArmyList(Guid battleId, Guid forceId)
    {
        return ArmyLists
            .Where(item => item.BattleId == battleId && item.ForceId == forceId)
            .OrderByDescending(static item => item.SubmittedUtc)
            .FirstOrDefault();
    }
}
