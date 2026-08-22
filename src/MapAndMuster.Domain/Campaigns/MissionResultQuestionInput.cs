namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied mission result question.
/// </summary>
public sealed class MissionResultQuestionInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the question text.</summary>
    public string? Prompt { get; init; }

    /// <summary>Gets Boolean or BattlePoints.</summary>
    public string? Kind { get; init; }

    /// <summary>Gets battle points awarded when a boolean answer is true.</summary>
    public int? BattlePoints { get; init; }

    /// <summary>Gets campaign points awarded when the question is scored.</summary>
    public int? CampaignPoints { get; init; }
}
