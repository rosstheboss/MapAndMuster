namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A campaign-manager-written question asked when reporting a mission result.
/// </summary>
public sealed class MissionResultQuestionSetup
{
    /// <summary>
    /// Initializes a mission result question.
    /// </summary>
    /// <param name="id">The question identifier.</param>
    /// <param name="prompt">The question text.</param>
    /// <param name="kind">Whether the answer is true/false or a battle-point amount.</param>
    /// <param name="battlePoints">Battle points awarded when the boolean is true, or ignored for amount questions.</param>
    /// <param name="campaignPoints">Campaign points awarded when the boolean is true or when an amount is reported.</param>
    public MissionResultQuestionSetup(
        Guid id,
        string prompt,
        MissionResultQuestionKind kind,
        int battlePoints,
        int campaignPoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentOutOfRangeException.ThrowIfNegative(battlePoints);
        ArgumentOutOfRangeException.ThrowIfNegative(campaignPoints);
        Id = id;
        Prompt = prompt;
        Kind = kind;
        BattlePoints = battlePoints;
        CampaignPoints = campaignPoints;
    }

    /// <summary>Gets the question identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the question text written by the campaign manager.</summary>
    public string Prompt { get; }

    /// <summary>Gets how the question is answered.</summary>
    public MissionResultQuestionKind Kind { get; }

    /// <summary>
    /// Gets battle points awarded when a boolean answer is true. Amount questions use the reported value instead.
    /// </summary>
    public int BattlePoints { get; }

    /// <summary>Gets campaign points awarded when the question is scored.</summary>
    public int CampaignPoints { get; }
}
