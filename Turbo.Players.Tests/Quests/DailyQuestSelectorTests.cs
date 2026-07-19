using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Turbo.Players.Quests;
using Turbo.Primitives.Quests.Snapshots;
using Xunit;

namespace Turbo.Players.Tests.Quests;

public class DailyQuestSelectorTests
{
    private static QuestSnapshot Quest(int id) =>
        new()
        {
            CampaignCode = "daily",
            CompletedQuestsInCampaign = 0,
            QuestCountInCampaign = 3,
            ActivityPointType = 0,
            Id = id,
            Accepted = false,
            QuestType = "RoomEntry",
            ImageVersion = "",
            RewardCurrencyAmount = 10,
            LocalizationCode = "code" + id,
            CompletedSteps = 0,
            TotalSteps = 1,
            SortOrder = id,
            CatalogPageName = "",
            ChainCode = "daily",
            Easy = true,
            Seasonal = false,
            SecondsLeft = 0,
        };

    private static readonly List<QuestSnapshot> Pool = [Quest(1), Quest(2), Quest(3)];

    [Fact]
    public void Pick_EmptyPool_ReturnsNull()
    {
        DailyQuestSelector.Pick([], 42, new DateOnly(2026, 7, 19)).Should().BeNull();
    }

    [Fact]
    public void Pick_IsDeterministicForSamePlayerAndDay()
    {
        DateOnly date = new(2026, 7, 19);

        QuestSnapshot? first = DailyQuestSelector.Pick(Pool, 42, date);
        QuestSnapshot? second = DailyQuestSelector.Pick(Pool, 42, date);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void Pick_ReturnsAMemberOfThePool()
    {
        QuestSnapshot? pick = DailyQuestSelector.Pick(Pool, 7, new DateOnly(2026, 7, 19));

        Pool.Should().Contain(pick!);
    }

    [Fact]
    public void Pick_RotatesAcrossDays()
    {
        // Over a fortnight, a 3-quest pool should surface more than one distinct quest.
        IEnumerable<int> picks = Enumerable
            .Range(0, 14)
            .Select(offset =>
                DailyQuestSelector.Pick(Pool, 42, new DateOnly(2026, 7, 19).AddDays(offset))!.Id
            )
            .Distinct();

        picks.Should().HaveCountGreaterThan(1);
    }
}
