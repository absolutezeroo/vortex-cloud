using FluentAssertions;
using Turbo.Players.Quests;
using Turbo.Primitives.Quests.Snapshots;
using Xunit;

namespace Turbo.Players.Tests.Quests;

public class QuestSnapshotBuilderTests
{
    private static QuestDefinitionSnapshot Definition(
        bool seasonal = false,
        int totalSteps = 5,
        int seasonalSeconds = 3600
    ) =>
        new()
        {
            Id = 42,
            CampaignCode = "explore",
            ChainCode = "explore",
            LocalizationCode = "explore1",
            QuestType = "RoomEntry",
            TotalSteps = totalSteps,
            RewardType = 0,
            RewardAmount = 25,
            CatalogPageName = "",
            ImageVersion = "",
            SortOrder = 3,
            Easy = true,
            Seasonal = seasonal,
            SeasonalSeconds = seasonalSeconds,
        };

    [Fact]
    public void Build_MapsDefinitionAndProgress()
    {
        QuestSnapshot s = QuestSnapshotBuilder.Build(
            Definition(),
            completedSteps: 2,
            accepted: true,
            completed: false,
            completedQuestsInCampaign: 1,
            questCountInCampaign: 3
        );

        s.Id.Should().Be(42);
        s.CampaignCode.Should().Be("explore");
        s.QuestType.Should().Be("RoomEntry");
        s.Accepted.Should().BeTrue();
        s.CompletedSteps.Should().Be(2);
        s.TotalSteps.Should().Be(5);
        s.CompletedQuestsInCampaign.Should().Be(1);
        s.QuestCountInCampaign.Should().Be(3);
        s.ActivityPointType.Should().Be(0);
        s.RewardCurrencyAmount.Should().Be(25);
        s.Seasonal.Should().BeFalse();
        s.SecondsLeft.Should().Be(0);
    }

    [Fact]
    public void Build_Completed_FillsStepsToTotal()
    {
        QuestSnapshot s = QuestSnapshotBuilder.Build(
            Definition(),
            completedSteps: 0,
            accepted: true,
            completed: true,
            completedQuestsInCampaign: 1,
            questCountInCampaign: 3
        );

        s.CompletedSteps.Should().Be(5);
    }

    [Fact]
    public void Build_ProgressBeyondTotal_IsClamped()
    {
        QuestSnapshot s = QuestSnapshotBuilder.Build(
            Definition(totalSteps: 3),
            completedSteps: 99,
            accepted: true,
            completed: false,
            completedQuestsInCampaign: 0,
            questCountInCampaign: 1
        );

        s.CompletedSteps.Should().Be(3);
    }

    [Fact]
    public void Build_Seasonal_SetsSecondsLeft()
    {
        QuestSnapshot s = QuestSnapshotBuilder.Build(
            Definition(seasonal: true, seasonalSeconds: 7200),
            completedSteps: 0,
            accepted: false,
            completed: false,
            completedQuestsInCampaign: 0,
            questCountInCampaign: 1
        );

        s.Seasonal.Should().BeTrue();
        s.SecondsLeft.Should().Be(7200);
    }
}
