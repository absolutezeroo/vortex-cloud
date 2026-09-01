using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The bound on how many players one wired action acts on. The furni side of a selection has been
/// bounded since the beginning — an area selector stops at <c>WiredSelectorMaxAreaSize</c>, a box
/// holds <c>WiredSelectedItemsLimit</c> items — and the player side never was, so "give furni from
/// this chest to everyone the selector picked" ran a database write, a full inventory reload and a
/// client push per player, sequentially, inside the room's turn (WIRED-FANOUT-051).
/// </summary>
public sealed class WiredSelectionBoundTests
{
    [Fact]
    public async Task ASelectionOverTheLimit_IsTrimmedToIt()
    {
        FakeWiredRoomHost host = new() { WiredSelectedPlayersLimit = 3 };

        IWiredSelectionSet selection = await EffectiveSelectionAsync(
            host,
            players: [1, 2, 3, 4, 5]
        );

        selection.SelectedPlayerIds.Should().HaveCount(3);
        selection.SelectedPlayerIds.Should().BeSubsetOf([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task ATrimmedSelection_IsCounted()
    {
        FakeWiredRoomHost host = new() { WiredSelectedPlayersLimit = 3 };

        await EffectiveSelectionAsync(host, players: [1, 2, 3, 4, 5]);

        host.StopReasons.Should().Equal([WiredStopReason.SELECTION_LIMIT]);
    }

    [Fact]
    public async Task ASelectionAtTheLimit_IsLeftAloneAndCountsNothing()
    {
        FakeWiredRoomHost host = new() { WiredSelectedPlayersLimit = 3 };

        IWiredSelectionSet selection = await EffectiveSelectionAsync(host, players: [1, 2, 3]);

        selection.SelectedPlayerIds.Should().BeEquivalentTo([1, 2, 3]);
        host.StopReasons.Should().BeEmpty();
    }

    [Fact]
    public async Task TheFurniSideIsNotTouched()
    {
        // Furni is bounded by the selectors themselves; trimming it here as well would silently
        // halve limits a room has already been configured against.
        FakeWiredRoomHost host = new() { WiredSelectedPlayersLimit = 1 };

        IWiredSelectionSet selection = await EffectiveSelectionAsync(
            host,
            players: [1, 2, 3],
            furni: [10, 11, 12]
        );

        selection.SelectedPlayerIds.Should().HaveCount(1);
        selection.SelectedFurniIds.Should().BeEquivalentTo([10, 11, 12]);
    }

    // ---- harness -----------------------------------------------------------------------------

    private static Task<IWiredSelectionSet> EffectiveSelectionAsync(
        FakeWiredRoomHost host,
        int[] players,
        int[]? furni = null
    )
    {
        WiredSelectionSet pool = new();

        pool.SelectedPlayerIds.UnionWith(players);
        pool.SelectedFurniIds.UnionWith(furni ?? []);

        WiredExecutionContext ctx = new(host) { SelectorPool = pool };

        return ctx.GetEffectiveSelectionAsync(SelectorSourcedBox(), CancellationToken.None);
    }

    /// <summary>A box that takes both halves of its selection from the selector pool.</summary>
    private static IWiredBox SelectorSourcedBox() =>
        FakeProxy.Create<IWiredBox>(call =>
            call.Method.Name switch
            {
                "GetPlayerSources" => new List<WiredPlayerSourceType[]>
                {
                    new[] { WiredPlayerSourceType.SelectorUsers },
                },
                "GetFurniSources" => new List<WiredFurniSourceType[]>
                {
                    new[] { WiredFurniSourceType.SelectorItems },
                },
                _ => null,
            }
        );
}
