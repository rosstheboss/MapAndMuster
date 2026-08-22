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
        IReadOnlyList<BrokenAllySubfaction>? brokenAllySubfactions = null)
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
        IReadOnlyList<BrokenAllySubfaction>? brokenAllySubfactions = null)
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
            brokenAllySubfactions ?? BrokenAllySubfactions);
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
        return
        [
            .. Forces
                .Where(force => !force.InBattle)
                .Select(static force => force.ControllerUserId)
                .Distinct(),
        ];
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
    /// Latest battle submission for a user, if any.
    /// </summary>
    public BattleResultSubmission? LatestBattleSubmission(Guid battleId, Guid userId)
    {
        return BattleSubmissions
            .Where(item => item.BattleId == battleId && item.SubmitterUserId == userId)
            .OrderByDescending(static item => item.SubmittedUtc)
            .FirstOrDefault();
    }
}
