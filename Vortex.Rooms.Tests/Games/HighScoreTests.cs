using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The persistent high-score boards. Their identity is written in the CLASSNAME
/// (<c>highscore_&lt;scoretype&gt;*&lt;variant&gt;</c>, Arcturus-style) — parse it wrong and every
/// board displays the wrong header or wipes on the wrong schedule. The windowed variants prune by
/// entry timestamp at rebuild time, and everything must survive the JSON round trip through the
/// item's STUFF section or the boards reload empty — the exact failure the seed comment predicted
/// ("the boards will render empty until the wired scoring subsystem fills them").
/// </summary>
public sealed class HighScoreTests
{
    [Theory]
    [InlineData("highscore_classic*1", 2, 0)]
    [InlineData("highscore_classic*2", 2, 1)]
    [InlineData("highscore_perteam*3", 0, 2)]
    [InlineData("highscore_perteam*1", 0, 0)]
    [InlineData("highscore_mostwin*4", 1, 3)]
    [InlineData("highscore_classic", 2, 0)]
    public async Task TheBoardsIdentity_ComesFromItsClassname(
        string classname,
        int expectedScoreType,
        int expectedClearType
    )
    {
        FurnitureHighScoreLogic board = Build(classname);

        await board.OnAttachAsync(CancellationToken.None);

        IHighscoreStuffData data = (IHighscoreStuffData)board.StuffData;
        data.ScoreType.Should().Be(expectedScoreType);
        data.ClearType.Should().Be(expectedClearType);
    }

    [Fact]
    public void ADailyBoard_PrunesEntriesOlderThanItsWindow()
    {
        IHighscoreStuffData data = NewData(scoreType: 2, clearType: 1);
        long now = DateTime.UtcNow.Ticks;

        data.RecordEntry(
            new HighscoreEntry
            {
                Score = 10,
                Win = true,
                RecordedUtcTicks = now - TimeSpan.FromDays(2).Ticks,
                Names = ["Old"],
            },
            now
        );
        data.RecordEntry(
            new HighscoreEntry
            {
                Score = 5,
                Win = true,
                RecordedUtcTicks = now,
                Names = ["Fresh"],
            },
            now
        );

        data.Entries.Should().ContainSingle().Which.Names.Should().Equal("Fresh");
        data.HighscoreData.Should().ContainKey(5).WhoseValue.Should().Equal("Fresh");
        data.HighscoreData.Should().NotContainKey(10);
    }

    [Fact]
    public void AnAlltimeBoard_KeepsEverything()
    {
        IHighscoreStuffData data = NewData(scoreType: 2, clearType: 0);
        long now = DateTime.UtcNow.Ticks;

        data.RecordEntry(
            new HighscoreEntry
            {
                Score = 10,
                Win = true,
                RecordedUtcTicks = now - TimeSpan.FromDays(400).Ticks,
                Names = ["Ancient"],
            },
            now
        );

        data.Entries.Should().ContainSingle();
        data.HighscoreData.Should().ContainKey(10);
    }

    [Fact]
    public void AMostWinsBoard_CountsVictoriesPerNameSet()
    {
        IHighscoreStuffData data = NewData(scoreType: 1, clearType: 0);
        long now = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 3; i++)
        {
            data.RecordEntry(
                new HighscoreEntry
                {
                    Score = 1,
                    Win = true,
                    RecordedUtcTicks = now,
                    Names = ["Alice", "Bob"],
                },
                now
            );
        }

        data.RecordEntry(
            new HighscoreEntry
            {
                Score = 1,
                Win = false,
                RecordedUtcTicks = now,
                Names = ["Carol"],
            },
            now
        );

        // Three wins for the Alice+Bob set; Carol's losing entry counts nothing.
        data.HighscoreData.Should().ContainKey(3).WhoseValue.Should().Equal("Alice", "Bob");
        data.HighscoreData.Should().HaveCount(1);
    }

    [Fact]
    public void AScoreBoard_MergesSameScoreNames_AndCapsItsRows()
    {
        IHighscoreStuffData data = NewData(scoreType: 2, clearType: 0);
        long now = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 60; i++)
        {
            data.RecordEntry(
                new HighscoreEntry
                {
                    Score = i,
                    Win = true,
                    RecordedUtcTicks = now,
                    Names = [$"P{i}"],
                },
                now
            );
        }

        // 60 distinct scores, 50 displayed — the top ones.
        data.HighscoreData.Should().HaveCount(50);
        data.HighscoreData.Keys.Min().Should().Be(10);
    }

    [Fact]
    public void TheEntries_SurviveThePersistenceRoundTrip()
    {
        StuffDataFactory factory = new();
        IHighscoreStuffData data = NewData(scoreType: 1, clearType: 2, factory);
        long now = DateTime.UtcNow.Ticks;

        data.RecordEntry(
            new HighscoreEntry
            {
                Score = 7,
                Win = true,
                RecordedUtcTicks = now,
                Names = ["Alice"],
            },
            now
        );

        // The write path serializes the concrete stuff data into the STUFF section (this is the
        // exact call PersistStuffDataAsync makes); reading it back must restore the entries, not
        // just the display rows.
        IExtraData extraData = new ExtraData(null);
        extraData.UpdateSection(
            ExtraDataSectionType.STUFF,
            JsonSerializer.SerializeToNode(data, data.GetType())
        );

        IHighscoreStuffData reloaded = (IHighscoreStuffData)
            factory.CreateStuffDataFromExtraData(StuffDataType.HighscoreKey, extraData);

        reloaded.ScoreType.Should().Be(1);
        reloaded.ClearType.Should().Be(2);
        HighscoreEntry entry = reloaded.Entries.Should().ContainSingle().Subject;
        entry.Score.Should().Be(7);
        entry.Win.Should().BeTrue();
        entry.RecordedUtcTicks.Should().Be(now);
        entry.Names.Should().Equal("Alice");
    }

    private static IHighscoreStuffData NewData(
        int scoreType,
        int clearType,
        StuffDataFactory? factory = null
    )
    {
        IHighscoreStuffData data = (IHighscoreStuffData)
            (factory ?? new StuffDataFactory()).CreateStuffData(StuffDataType.HighscoreKey);

        data.SetScoreType(scoreType);
        data.SetClearType(clearType);

        return data;
    }

    private static FurnitureHighScoreLogic Build(string classname)
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = classname,
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "wf_highscore",
            TotalStates = 2,
            Width = 1,
            Length = 1,
            StackHeight = default,
            CanStack = false,
            CanWalk = false,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = true,
            CanGroup = false,
            CanSell = true,
            UsagePolicy = FurnitureUsageType.Controller,
            ExtraData = null,
            StuffDataType = StuffDataType.HighscoreKey,
        };

        IExtraData extraData = new ExtraData(null);

        IRoomFloorItem item = FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name switch
            {
                "get_ExtraData" => extraData,
                "get_Definition" => definition,
                _ => null,
            }
        );

        IRoomFloorItemContext ctx = FakeProxy.Create<IRoomFloorItemContext>(call =>
            call.Method.Name switch
            {
                "get_Definition" => definition,
                "get_RoomObject" => item,
                _ => null,
            }
        );

        return new FurnitureHighScoreLogic(new StuffDataFactory(), ctx);
    }
}
