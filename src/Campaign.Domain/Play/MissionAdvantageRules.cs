using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// Applies attacker/defender mission army-point and supply-point advantages, then clamps.
/// </summary>
public static class MissionAdvantageRules
{
    /// <summary>
    /// Returns army points after an optional signed number or percent change, never below 500.
    /// </summary>
    public static int ApplyArmyPoints(int armyPoints, MissionSetup mission, bool isAdvantagedSide)
    {
        ArgumentNullException.ThrowIfNull(mission);
        if (!isAdvantagedSide || !mission.HasArmyPointsAdvantage)
        {
            return armyPoints;
        }

        var next = mission.ArmyPointsAdvantageIsPercent
            ? armyPoints + (int)decimal.Floor(armyPoints * mission.ArmyPointsAdvantageAmount / 100m)
            : armyPoints + mission.ArmyPointsAdvantageAmount;
        return Math.Max(HuntInEstaliaDefaults.MinimumArmyPoints, next);
    }

    /// <summary>
    /// Returns supply points after an optional raw signed change, never below 1.
    /// </summary>
    public static int ApplySupplyPoints(int supplyPoints, MissionSetup mission, bool isAdvantagedSide)
    {
        ArgumentNullException.ThrowIfNull(mission);
        if (!isAdvantagedSide || !mission.HasSupplyPointsAdvantage)
        {
            return supplyPoints;
        }

        return Math.Max(HuntInEstaliaDefaults.MinimumSupplyPoints, supplyPoints + mission.SupplyPointsAdvantageAmount);
    }

    /// <summary>
    /// Whether a participating force is on the side that receives a configured advantage.
    /// </summary>
    public static bool IsAdvantagedSide(
        Guid forceId,
        MissionAdvantageSide side,
        Guid? attackerForceId,
        Guid? defenderForceId,
        IReadOnlyList<IReadOnlyList<CampaignForce>> sides)
    {
        ArgumentNullException.ThrowIfNull(sides);
        var roleForceId = side == MissionAdvantageSide.Attacker ? attackerForceId : defenderForceId;
        if (roleForceId is null)
        {
            return false;
        }

        var roleSide = sides.FirstOrDefault(group => group.Any(member => member.Id == roleForceId.Value));
        return roleSide is not null && roleSide.Any(member => member.Id == forceId);
    }
}
