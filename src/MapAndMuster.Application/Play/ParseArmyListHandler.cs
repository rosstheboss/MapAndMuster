using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

/// <summary>
/// Parses pasted army-list text so a player can review auto-filled supply amounts before submitting.
/// </summary>
public sealed class ParseArmyListHandler
{
    private readonly ICampaignStore _campaigns;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public ParseArmyListHandler(ICampaignStore campaigns)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        _campaigns = campaigns;
    }

    /// <summary>
    /// Returns parsed army points and category supply amounts, or a failed parse.
    /// </summary>
    public async Task<OperationResult<ArmyListParseDetail>> HandleAsync(
        ParseArmyListCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<ArmyListParseDetail>(
                ErrorCodes.CampaignNotFound,
                "The campaign was not found.");
        }

        if (!ArmyListRules.TryNormalizeText(command.Text, out var error, out var text))
        {
            return OperationResults.Failure<ArmyListParseDetail>([error]);
        }

        var gameSystem = ArmyListRules.NormalizeGameSystem(command.GameSystem)
            ?? ArmyListGameSystems.WarhammerTheOldWorld;
        var builder = ArmyListRules.ParseBuilder(command.Builder);
        if (builder is ArmyListBuilder.Other || string.IsNullOrWhiteSpace(text) || !ArmyListGameSystems.CanParse(gameSystem))
        {
            return OperationResults.Success(new ArmyListParseDetail { Parsed = false });
        }

        var parsed = OldWorldArmyListParser.Parse(text, builder);
        if (!parsed.Parsed)
        {
            return OperationResults.Success(
                new ArmyListParseDetail
                {
                    Parsed = false,
                    Message = ArmyListRules.ParseFailedMessage,
                });
        }

        return OperationResults.Success(
            new ArmyListParseDetail
            {
                Parsed = true,
                ArmyPoints = parsed.ArmyPoints,
                SupplyCostingUnitCount = parsed.SupplyCostingUnitCount,
                Categories =
                [
                    .. parsed.Categories.Select(static category => new ArmyListSupplyCategoryDetail
                    {
                        Name = category.Name,
                        UnitCount = category.UnitCount,
                        SupplyPoints = category.SupplyPoints,
                        CostsSupply = category.CostsSupply,
                    }),
                ],
            });
    }
}
