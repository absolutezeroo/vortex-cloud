using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The token a wired text placeholder answers to. The client tells the player in as many words:
/// "Use this by typing $(name) in Wired texts", and normalises the name it shows them — a server
/// matching anything else silently leaves the raw token in the chat bubble.
/// </summary>
public sealed class WiredPlaceholderTests
{
    [Fact]
    public void TheTokenIsTheNameInParentheses_Normalised()
    {
        WiredPlaceholder.BuildToken("winner").Should().Be("$(winner)");
        // The form lowercases and turns spaces into underscores as the player types.
        WiredPlaceholder.BuildToken("Top Score").Should().Be("$(top_score)");
    }

    [Fact]
    public void AnUnnamedBox_HasNoToken()
    {
        // An empty token would otherwise match at every position in the text.
        WiredPlaceholder.BuildToken(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void TheStringParam_IsTheNameThenTheDelimiter()
    {
        WiredPlaceholder.ParseConfiguration("winner").Should().Be(("winner", string.Empty));
        WiredPlaceholder.ParseConfiguration("winner\t, ").Should().Be(("winner", ", "));
        WiredPlaceholder.ParseConfiguration(null).Should().Be((string.Empty, string.Empty));
    }

    [Fact]
    public void SingleMode_UsesTheFirstValueOnly()
    {
        WiredPlaceholder
            .Substitute("gg $(winner)!", "$(winner)", ["alice", "bob"], false, ", ")
            .Should()
            .Be("gg alice!");
    }

    [Fact]
    public void MultipleMode_JoinsWithTheDelimiter()
    {
        WiredPlaceholder
            .Substitute("gg $(winner)!", "$(winner)", ["alice", "bob"], true, ", ")
            .Should()
            .Be("gg alice, bob!");
    }

    [Fact]
    public void EveryOccurrence_IsReplaced()
    {
        WiredPlaceholder
            .Substitute("$(x) vs $(x)", "$(x)", ["alice"], false, string.Empty)
            .Should()
            .Be("alice vs alice");
    }

    [Fact]
    public void NothingToSay_RemovesTheToken()
    {
        // Leaving "$(winner)" on screen would show the player the machinery.
        WiredPlaceholder
            .Substitute("gg $(winner)!", "$(winner)", [], false, ", ")
            .Should()
            .Be("gg !");
    }

    [Fact]
    public void ATextWithoutTheToken_IsUntouched()
    {
        WiredPlaceholder
            .Substitute("hello", "$(winner)", ["alice"], false, ", ")
            .Should()
            .Be("hello");
    }

    [Fact]
    public async Task TheUsernamePlaceholder_PutsTheActualNameInTheText()
    {
        IRoomPlayer alice = Player(7, "alice");
        IRoomPlayer bob = Player(8, "bob");

        TestUsernamePlaceholder box = new(
            StubContext(new FakeRoomLookup(alice, bob)),
            new WiredData { StringParam = "winner	, ", IntParams = [1] }
        );

        string said = await box.ApplyToTextAsync(
            "gg $(winner)!",
            Execution(7, 8),
            CancellationToken.None
        );

        said.Should().Be("gg alice, bob!");
    }

    [Fact]
    public async Task APlaceholderTheTextDoesNotMention_IsLeftAlone()
    {
        TestUsernamePlaceholder box = new(
            StubContext(new FakeRoomLookup(Player(7, "alice"))),
            new WiredData { StringParam = "winner", IntParams = [0] }
        );

        string said = await box.ApplyToTextAsync("hello", Execution(7), CancellationToken.None);

        said.Should().Be("hello");
    }

    [Fact]
    public async Task AUserWhoLeftBetweenTheTriggerAndTheText_IsSkipped()
    {
        // The selection still holds them; the room does not. An empty slot in the middle of the
        // sentence would read as a bug to everyone in the room.
        TestUsernamePlaceholder box = new(
            StubContext(new FakeRoomLookup(Player(7, "alice"))),
            new WiredData { StringParam = "winner	, ", IntParams = [1] }
        );

        string said = await box.ApplyToTextAsync(
            "gg $(winner)!",
            Execution(7, 8),
            CancellationToken.None
        );

        said.Should().Be("gg alice!");
    }

    // ---- harness -------------------------------------------------------------------------------

    private static IRoomPlayer Player(int playerId, string name) =>
        FakeProxy.Create<IRoomPlayer>(call =>
            call.Method.Name switch
            {
                "get_PlayerId" => new PlayerId(playerId),
                "get_Name" => name,
                _ => null,
            }
        );

    private static IWiredExecutionContext Execution(params int[] playerIds)
    {
        WiredSelectionSet selection = new();

        foreach (int id in playerIds)
        {
            selection.SelectedPlayerIds.Add(id);
        }

        return FakeProxy.Create<IWiredExecutionContext>(call =>
            call.Method.Name == "GetEffectiveSelectionAsync"
                ? Task.FromResult<IWiredSelectionSet>(selection)
                : null
        );
    }

    private static IRoomFloorItemContext StubContext(IRoomLookup lookup)
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "wf_xtra_text_output_username",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "wf_xtra_text_output_username",
            TotalStates = 1,
            Width = 1,
            Length = 1,
            StackHeight = default,
            CanStack = false,
            CanWalk = false,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = false,
            CanGroup = false,
            CanSell = false,
            UsagePolicy = FurnitureUsageType.Everybody,
            ExtraData = null,
            StuffDataType = StuffDataType.LegacyKey,
        };

        IRoomFloorItem item = FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name == "get_ExtraData" ? new ExtraData(null) : null
        );

        return FakeProxy.Create<IRoomFloorItemContext>(call =>
            call.Method.Name switch
            {
                "get_Definition" => definition,
                "get_RoomObject" => item,
                "get_Lookup" => lookup,
                _ => null,
            }
        );
    }

    private sealed class TestUsernamePlaceholder : WiredAddonUsernamePlaceholder
    {
        public TestUsernamePlaceholder(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }
}
