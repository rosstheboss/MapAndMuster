namespace Campaign.Domain.Play;

/// <summary>
/// A player-submitted or system-created campaign action.
/// </summary>
public enum ActionKind
{
    /// <summary>Travel to an allowed adjacent territory.</summary>
    Move = 0,

    /// <summary>Remain in place.</summary>
    Hold = 1,

    /// <summary>Create an allowed structure in the current territory.</summary>
    Build = 2,

    /// <summary>Progress a structure toward pillaged or destroyed.</summary>
    Pillage = 3,

    /// <summary>Restore an eligible pillaged structure.</summary>
    Repair = 4,

    /// <summary>Create a second force in an eligible adjacent territory.</summary>
    Split = 5,

    /// <summary>Leave an alliance and force battle when co-located with a former ally.</summary>
    Backstab = 6,

    /// <summary>Move a losing force to an eligible territory or spawn.</summary>
    Retreat = 7,

    /// <summary>Automatic system action while a force is locked in battle.</summary>
    Battle = 8,
}

/// <summary>
/// Lifecycle of one stored action or battle window.
/// </summary>
public enum PhaseWindowStatus
{
    /// <summary>The window has not opened yet.</summary>
    Pending = 0,

    /// <summary>Players may draft, commit, uncommit, or submit battle results.</summary>
    Open = 1,

    /// <summary>The window closed and resolution is recorded.</summary>
    Resolved = 2,
}

/// <summary>
/// Lifecycle of one engagement.
/// </summary>
public enum BattleStatus
{
    /// <summary>Created by action resolution; waiting for a battle window.</summary>
    Pending = 0,

    /// <summary>Participants may submit or accept results.</summary>
    AwaitingResults = 1,

    /// <summary>Participants agreed, or a single timely submission became authoritative.</summary>
    Finalized = 2,

    /// <summary>Conflicting submissions; waiting for a manager.</summary>
    Disputed = 3,

    /// <summary>A manager recorded an authoritative result.</summary>
    GMResolved = 4,
}

/// <summary>
/// How a submitted order entered history.
/// </summary>
public enum OrderSource
{
    /// <summary>The player committed while the window was open.</summary>
    Commit = 0,

    /// <summary>The deadline submitted the latest draft.</summary>
    DeadlineDraft = 1,

    /// <summary>The deadline created Hold because no draft existed.</summary>
    DeadlineHold = 2,

    /// <summary>A manager in debug mode appended a corrected order without erasing the original.</summary>
    StaffCorrection = 3,
}

/// <summary>
/// A recorded fact in the public campaign play log.
/// </summary>
public enum PlayLogKind
{
    /// <summary>A force's order after an action window resolved.</summary>
    ResolvedAction = 0,

    /// <summary>The deadline submitted the player's latest draft.</summary>
    DeadlineDraftSubmitted = 1,

    /// <summary>A missing order became Hold.</summary>
    MissingOrderHold = 2,

    /// <summary>An invalid submitted order became Hold.</summary>
    InvalidOrderHold = 3,

    /// <summary>Competing builds on the same territory became Hold.</summary>
    ConflictingBuildHold = 4,

    /// <summary>Enemy forces created a battle after movement.</summary>
    BattleCreated = 5,

    /// <summary>Participants agreed, or a single timely result became authoritative.</summary>
    BattleFinalized = 6,

    /// <summary>Conflicting battle results need a manager.</summary>
    BattleDisputed = 7,

    /// <summary>A manager recorded an authoritative battle result.</summary>
    BattleGmResolved = 8,

    /// <summary>A player submitted a retreat after a loss.</summary>
    PlayerRetreat = 9,

    /// <summary>A missing retreat used the spawn fallback.</summary>
    DefaultRetreat = 10,

    /// <summary>A battle stayed open because resolution could not finish.</summary>
    UnresolvedBattleHeldOpen = 11,

    /// <summary>The campaign launched and the first phase opened.</summary>
    CampaignStarted = 12,

    /// <summary>A manager lengthened remaining phases or added rounds.</summary>
    ScheduleExtended = 13,

    /// <summary>Same-player forces occupying one territory rejoined.</summary>
    ForcesRejoined = 14,

    /// <summary>A campaign member posted a public chat message.</summary>
    PlayerChat = 15,

    /// <summary>A manager or administrator entered debug mode.</summary>
    DebugEntered = 16,

    /// <summary>A manager or administrator left debug mode.</summary>
    DebugExited = 17,

    /// <summary>A manager in debug mode corrected a force's order.</summary>
    DebugOrderCorrected = 18,

    /// <summary>A manager in debug mode re-resolved a prior action window.</summary>
    DebugActionReresolved = 19,

    /// <summary>A hidden item objective was found by occupying its territory.</summary>
    ItemObjectiveFound = 20,

    /// <summary>A revealed item objective was picked up by a force.</summary>
    ItemObjectivePickedUp = 21,

    /// <summary>A force dropped an item objective when it moved.</summary>
    ItemObjectiveDropped = 22,

    /// <summary>A manager in debug mode revealed hidden item objectives to players.</summary>
    ItemObjectivesStaffRevealed = 23,
}

/// <summary>
/// Structure condition used during play.
/// </summary>
public enum StructureCondition
{
    /// <summary>The structure is intact.</summary>
    Operational = 0,

    /// <summary>The structure is pillaged.</summary>
    Pillaged = 1,

    /// <summary>The structure is destroyed.</summary>
    Destroyed = 2,
}
