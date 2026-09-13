using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// "Move as group": several furni travel together and arrive still arranged.
/// <para>
/// The two properties worth pinning are the ones a builder would notice the moment they break — the
/// arrangement survives the trip, and a move that does not fit moves nothing at all. A group move
/// that half-succeeds turns a build into rubble, and a room has no undo.
/// </para>
/// </summary>
public sealed class WiredMoveAsGroupTests
{
    private const int Anchor = 10;

    private const int Far = 11;

    private const int TargetFurni = 20;

    [Fact]
    public async Task TheGroupKeepsItsArrangement()
    {
        // Corner at (2,2), the other member two east and one south of it.
        Harness harness = new(Item(Anchor, 2, 2), Item(Far, 4, 3));
        harness.PlaceUser(10, 10);

        await harness.RunAsync(targetIsUser: 1, offsetX: 0, offsetY: 0);

        // The corner lands on the target; the second member keeps its +2/+1 relation to it.
        harness.Moved.Should().Equal((Anchor, 10, 10), (Far, 12, 11));
    }

    [Fact]
    public async Task TheOffsetMovesTheWholeGroup()
    {
        Harness harness = new(Item(Anchor, 2, 2), Item(Far, 4, 3));
        harness.PlaceUser(10, 10);

        await harness.RunAsync(targetIsUser: 1, offsetX: -3, offsetY: 5);

        harness.Moved.Should().Equal((Anchor, 7, 15), (Far, 9, 16));
    }

    [Fact]
    public async Task OneDestinationRefused_MovesNothingAtAll()
    {
        Harness harness = new(Item(Anchor, 2, 2), Item(Far, 4, 3));
        harness.PlaceUser(10, 10);

        // The corner fits; the second member does not.
        harness.Furni.PlacementAllowed = (x, y) => !(x == 12 && y == 11);

        await harness.RunAsync(targetIsUser: 1, offsetX: 0, offsetY: 0);

        harness.Moved.Should().BeEmpty();
    }

    [Fact]
    public async Task TheTargetFurniDoesNotTravelWithTheGroup()
    {
        // Every slot of a box resolves into one selection, so the destination furni arrives in the
        // same list as the group. It is the destination, not a passenger.
        Harness harness = new(Item(Anchor, 2, 2), Item(TargetFurni, 10, 10));

        await harness.RunAsync(targetIsUser: 0, offsetX: 0, offsetY: 0, targetIds: [TargetFurni]);

        harness.Moved.Should().Equal((Anchor, 10, 10));
    }

    [Fact]
    public async Task AlreadyThere_DoesNothing()
    {
        Harness harness = new(Item(Anchor, 10, 10));
        harness.PlaceUser(10, 10);

        await harness.RunAsync(targetIsUser: 1, offsetX: 0, offsetY: 0);

        harness.Moved.Should().BeEmpty();
    }

    private static IRoomFloorItem Item(int objectId, int x, int y)
    {
        RoomObjectId id = new(objectId);

        return FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name switch
            {
                "get_ObjectId" => id,
                "get_X" => x,
                "get_Y" => y,
                "get_Rotation" => Rotation.North,
                _ => null,
            }
        );
    }

    /// <summary>A room holding the furni under test, recording where the box asked to put them.</summary>
    private sealed class Harness
    {
        public FakeFurniAccess Furni { get; } = new();

        public List<(int ObjectId, int X, int Y)> Moved { get; } = [];

        private readonly FakeRoomLookup _lookup = new();

        private readonly List<IRoomFloorItem> _items;

        private readonly List<int> _players = [];

        public Harness(params IRoomFloorItem[] items)
        {
            _items = [.. items];

            foreach (IRoomFloorItem item in items)
            {
                _lookup.ItemsById[item.ObjectId] = item;
            }
        }

        public void PlaceUser(int x, int y)
        {
            const int PlayerId = 7;

            _players.Add(PlayerId);
            _lookup.AvatarsByPlayer[PlayerId] = FakeProxy.Create<IRoomAvatar>(call =>
                call.Method.Name switch
                {
                    "get_X" => x,
                    "get_Y" => y,
                    _ => null,
                }
            );
        }

        public Task<bool> RunAsync(
            int targetIsUser,
            int offsetX,
            int offsetY,
            List<int>? targetIds = null
        )
        {
            WiredData data = new()
            {
                IntParams = [targetIsUser, offsetX, offsetY],
                StuffIds = [.. _items.Select(i => i.ObjectId.Value)],
                StuffIds2 = targetIds ?? [],
            };

            data.AttatchRules(new TestMoveAsGroup(Context(), data).GetIntParamRules());

            return new TestMoveAsGroup(Context(), data).ExecuteAsync(
                Execution(),
                CancellationToken.None
            );
        }

        private IRoomFloorItemContext Context() =>
            WiredTestBoxes.Context(lookup: _lookup, furniAccess: Furni, map: Map());

        private IRoomMapAccess Map() =>
            FakeProxy.Create<IRoomMapAccess>(call =>
                call.Method.Name == "ToIdx" ? Idx((int)call.Args![0]!, (int)call.Args![1]!) : null
            );

        /// <summary>A tile index that stays decodable, so a recorded move can be read back as the
        /// coordinates the box asked for rather than as an opaque number.</summary>
        private static int Idx(int x, int y) => (x * 1000) + y;

        private IWiredExecutionContext Execution()
        {
            WiredSelectionSet selection = new();

            foreach (IRoomFloorItem item in _items)
            {
                selection.SelectedFurniIds.Add(item.ObjectId.Value);
            }

            foreach (int playerId in _players)
            {
                selection.SelectedPlayerIds.Add(playerId);
            }

            return FakeProxy.Create<IWiredExecutionContext>(call =>
            {
                switch (call.Method.Name)
                {
                    case "GetEffectiveSelectionAsync":
                        return Task.FromResult<IWiredSelectionSet>(selection);

                    case "get_Selected":
                        return selection;

                    case "ProcessFloorItemMovementAsync":
                        IRoomFloorItem item = (IRoomFloorItem)call.Args![0]!;
                        int idx = (int)call.Args![1]!;

                        Moved.Add((item.ObjectId.Value, idx / 1000, idx % 1000));

                        return Task.FromResult(true);

                    default:
                        return null;
                }
            });
        }
    }

    private sealed class TestMoveAsGroup : WiredActionMoveAsGroup
    {
        public TestMoveAsGroup(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }
}
