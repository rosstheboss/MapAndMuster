using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class CampaignCompletionLogRulesTests
{
    [Fact]
    public void FormatStatesTheCampaignEndedWithScoresAndItems()
    {
        var text = CampaignCompletionLogRules.Format(
            [("northplayer", 12), ("southplayer", 8)],
            ["Sword held by northplayer", "Crown in Coast"],
            revised: false);

        Assert.Equal(
            "The campaign ended. Final scores: northplayer 12; southplayer 8. Item objectives: Sword held by northplayer; Crown in Coast.",
            text);
    }

    [Fact]
    public void FormatMarksALaterRevision()
    {
        var text = CampaignCompletionLogRules.Format(
            [("northplayer", 14)],
            [],
            revised: true);

        Assert.Equal("Updated final scores: northplayer 14. Item objectives: none remained.", text);
    }
}
