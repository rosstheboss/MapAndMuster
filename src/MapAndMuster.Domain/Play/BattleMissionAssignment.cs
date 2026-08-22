namespace MapAndMuster.Domain.Play;

/// <summary>
/// The mission and optional attacker/defender roles chosen when a battle is created.
/// </summary>
/// <param name="MissionId">The chosen mission.</param>
/// <param name="AttackerForceId">The attacking force when the mission uses attacker/defender roles.</param>
/// <param name="DefenderForceId">The defending force when the mission uses attacker/defender roles.</param>
public sealed record BattleMissionAssignment(
    Guid MissionId,
    Guid? AttackerForceId,
    Guid? DefenderForceId);
