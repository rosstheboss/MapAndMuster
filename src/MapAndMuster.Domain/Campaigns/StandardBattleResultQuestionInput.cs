namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied reusable battle-result question for campaign setup.
/// </summary>
public sealed class StandardBattleResultQuestionInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the question text.</summary>
    public string? Prompt { get; init; }

    /// <summary>Gets Boolean or BattlePoints.</summary>
    public string? Kind { get; init; }

    /// <summary>Gets standard battle points awarded when a boolean is true.</summary>
    public int? BattlePoints { get; init; }

    /// <summary>Gets standard campaign points awarded when the question is scored.</summary>
    public int? CampaignPoints { get; init; }
}
