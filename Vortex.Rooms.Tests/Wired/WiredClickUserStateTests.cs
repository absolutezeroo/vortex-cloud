using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// What the room tells the client about its click-user boxes.
/// </summary>
/// <remarks>
/// Both answers are load-bearing on the client side and in opposite directions. Report absent and
/// the client never routes an avatar click through wired at all; report the menu blocked when it is
/// not and the info stand loses its buttons for the rest of the visit. Neither failure produces an
/// error anywhere — the client simply behaves as if the feature did not exist.
/// </remarks>
public sealed class WiredClickUserStateTests
{
    [Fact]
    public async Task ARoomWithNoClickUserBox_ReportsAbsentAndLeavesTheMenuAlone()
    {
        FakeWiredRoomHost room = new();

        WiredClickUserSnapshot state = await new RoomWiredSystem(room).GetClickUserStateAsync(
            CancellationToken.None
        );

        state.Present.Should().BeFalse();
        state.BlocksMenu.Should().BeFalse();
    }

    [Fact]
    public async Task AnUnconfiguredBox_IsPresentAndStillOpensTheMenu()
    {
        FakeWiredRoomHost room = new();
        room.With(ClickUserBox(1, blockMenuOpen: null));

        WiredClickUserSnapshot state = await new RoomWiredSystem(room).GetClickUserStateAsync(
            CancellationToken.None
        );

        state.Present.Should().BeTrue();
        // The client's own default: intParams default to false, so the menu opens as it always did.
        state.BlocksMenu.Should().BeFalse();
    }

    [Fact]
    public async Task ABoxAskingToBlockTheMenu_SuppressesIt()
    {
        FakeWiredRoomHost room = new();
        room.With(ClickUserBox(1, blockMenuOpen: true));

        WiredClickUserSnapshot state = await new RoomWiredSystem(room).GetClickUserStateAsync(
            CancellationToken.None
        );

        state.Present.Should().BeTrue();
        state.BlocksMenu.Should().BeTrue();
    }

    [Fact]
    public async Task ABoxExplicitlyNotBlocking_LeavesTheMenuOpen()
    {
        FakeWiredRoomHost room = new();
        room.With(ClickUserBox(1, blockMenuOpen: false));

        WiredClickUserSnapshot state = await new RoomWiredSystem(room).GetClickUserStateAsync(
            CancellationToken.None
        );

        state.Present.Should().BeTrue();
        state.BlocksMenu.Should().BeFalse();
    }

    /// <summary>
    /// A menu cannot be half-suppressed, so one box asking for it decides for the room. Reading only
    /// the first box found would make the answer depend on furniture order.
    /// </summary>
    [Fact]
    public async Task OneBlockingBoxAmongSeveral_DecidesForTheRoom()
    {
        FakeWiredRoomHost room = new();
        room.With(ClickUserBox(1, blockMenuOpen: false));
        room.With(ClickUserBox(2, blockMenuOpen: true));
        room.With(ClickUserBox(3, blockMenuOpen: false));

        WiredClickUserSnapshot state = await new RoomWiredSystem(room).GetClickUserStateAsync(
            CancellationToken.None
        );

        state.BlocksMenu.Should().BeTrue();
    }

    /// <summary>
    /// The registry starts dirty and is normally rebuilt by the tick. This is asked on room entry
    /// and on a click, neither of which is a tick — a room whose box was never indexed would report
    /// "no click-user box" and the client would cache that for the whole visit.
    /// </summary>
    [Fact]
    public async Task TheAnswerDoesNotWaitForATick()
    {
        FakeWiredRoomHost room = new();
        room.With(ClickUserBox(1, blockMenuOpen: true));

        RoomWiredSystem system = new(room);

        // No tick has run: the trigger registry has never been built.
        WiredClickUserSnapshot state = await system.GetClickUserStateAsync(CancellationToken.None);

        state.Present.Should().BeTrue();
    }

    private static IRoomFloorItem ClickUserBox(int objectId, bool? blockMenuOpen)
    {
        ExtraData extra = new(null);

        if (blockMenuOpen is bool blocks)
        {
            // Written the way a configured box is persisted, so hydration reads it through the same
            // path a real one does — including the param rules that decode the int back to a bool.
            extra.UpdateSection(
                ExtraDataSectionType.WIRED,
                JsonSerializer.SerializeToNode(new WiredData { IntParams = [blocks ? 1 : 0, 0] })
            );
        }

        return WiredTestBoxes.FloorItem(
            objectId,
            new WiredTriggerClickUser(
                FakeProxy.Create<IGrainFactory>(_ => null),
                new StuffDataFactory(),
                WiredTestBoxes.Context(objectId, extraData: extra)
            )
        );
    }
}
