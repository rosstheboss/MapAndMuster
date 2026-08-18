namespace Campaign.Domain.Play;

/// <summary>
/// One answer to a mission result question.
/// </summary>
public sealed class BattleQuestionAnswer
{
    /// <summary>
    /// Initializes a question answer.
    /// </summary>
    /// <param name="questionId">The catalog question.</param>
    /// <param name="booleanValue">The true/false answer, when the question is boolean.</param>
    /// <param name="battlePointsValue">The reported battle-point amount, when the question asks for one.</param>
    public BattleQuestionAnswer(Guid questionId, bool? booleanValue, int? battlePointsValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(battlePointsValue ?? 0);
        QuestionId = questionId;
        BooleanValue = booleanValue;
        BattlePointsValue = battlePointsValue;
    }

    /// <summary>Gets the catalog question.</summary>
    public Guid QuestionId { get; }

    /// <summary>Gets the true/false answer, when applicable.</summary>
    public bool? BooleanValue { get; }

    /// <summary>Gets the reported battle-point amount, when applicable.</summary>
    public int? BattlePointsValue { get; }
}
