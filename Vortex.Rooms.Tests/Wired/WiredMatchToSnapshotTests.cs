using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// "Match furni to position and state": the box every reset button in every game room is built on.
/// <para>
/// Its four checkboxes are independent, and that is the whole test surface — a box that restores the
/// position when only "state" was ticked drags a builder's furniture around behind their back, and a
/// box that restores nothing when a box is ticked is a reset button that does not reset.
/// </para>
/// </summary>
public sealed class WiredMatchToSnapshotTests
{
    private const int Furni = 10;

    [Fact]
    public async Task PositionTicked_PutsTheFurniBack()
    {
        Harness harness = new(Item(Furni, x: 9, y: 9, z: 100, rotation: Rotation.East, state: 1));
        harness.Snapshot(Furni, x: 2, y: 3, z: 50, rotation: Rotation.North, state: 0);

        await harness.RunAsync(state: 0, direction: 0, position: 1, altitude: 0);

        // Position only: the rotation it arrived with is kept, and no altitude is asked for.
        harness.Moved.Should().Equal((Furni, 2, 3, (int)Rotation.East, (int?)null));
        harness.States.Should().BeEmpty();
    }

    [Fact]
    public async Task StateTicked_OnlyRestoresTheState()
    {
        Harness harness = new(Item(Furni, x: 9, y: 9, z: 100, rotation: Rotation.East, state: 1));
        harness.Snapshot(Furni, x: 2, y: 3, z: 50, rotation: Rotation.North, state: 0);

        await harness.RunAsync(state: 1, direction: 0, position: 0, altitude: 0);

        harness.States.Should().Equal((Furni, 0));
        harness.Moved.Should().BeEmpty();
    }

    [Fact]
    public async Task PositionAndAltitudeTogether_MoveTheFurniOnce()
    {
        Harness harness = new(Item(Furni, x: 9, y: 9, z: 100, rotation: Rotation.East, state: 0));
        harness.Snapshot(Furni, x: 2, y: 3, z: 50, rotation: Rotation.North, state: 0);

        await harness.RunAsync(state: 0, direction: 1, position: 1, altitude: 1);

        harness.Moved.Should().Equal((Furni, 2, 3, (int)Rotation.North, (int?)50));
    }

    [Fact]
    public async Task NothingTicked_DoesNothing()
    {
        Harness harness = new(Item(Furni, x: 9, y: 9, z: 100, rotation: Rotation.East, state: 1));
        harness.Snapshot(Furni, x: 2, y: 3, z: 50, rotation: Rotation.North, state: 0);

        await harness.RunAsync(state: 0, direction: 0, position: 0, altitude: 0);

        harness.Moved.Should().BeEmpty();
        harness.States.Should().BeEmpty();
    }

    [Fact]
    public async Task AlreadyMatching_DoesNothing()
    {
        Harness harness = new(Item(Furni, x: 2, y: 3, z: 50, rotation: Rotation.North, state: 0));
        harness.Snapshot(Furni, x: 2, y: 3, z: 50, rotation: Rotation.North, state: 0);

        await harness.RunAsync(state: 1, direction: 1, position: 1, altitude: 1);

        harness.Moved.Should().BeEmpty();
        harness.States.Should().BeEmpty();
    }

    [Fact]
    public async Task ARefusedDestination_LeavesThatFurniWhereItIs()
    {
        Harness harness = new(Item(Furni, x: 9, y: 9, z: 100, rotation: Rotation.East, state: 0));
        harness.Snapshot(Furni, x: 2, y: 3, z: 50, rotation: Rotation.North, state: 0);
        harness.Furni.PlacementAllowed = (_, _) => false;

        await harness.RunAsync(state: 0, direction: 0, position: 1, altitude: 0);

        harness.Moved.Should().BeEmpty();
    }

    [Fact]
    public async Task AFurniThatIsGone_IsSkipped()
    {
        // The snapshot outlives what it describes: somebody picked the furni up after the box was
        // saved.
        Harness harness = new();
        harness.Snapshot(Furni, x: 2, y: 3, z: 50, rotation: Rotation.North, state: 0);

        await harness.RunAsync(state: 1, direction: 1, position: 1, altitude: 1);

        harness.Moved.Should().BeEmpty();
        harness.States.Should().BeEmpty();
    }

    [Fact]
    public void TheBoxDeclaresItRecordsASnapshot()
    {
        // Without this the save path records nothing and every firing above has no data to work
        // from -- the box would look configured and do nothing forever.
        new Harness()
            .Box()
            .HasStateSnapshot.Should()
            .BeTrue();
    }

    private static IRoomFloorItem Item(
        int objectId,
        int x,
        int y,
        int z,
        Rotation rotation,
        int state
    )
    {
        RoomObjectId id = new(objectId);
        IFurnitureFloorLogic logic = FakeProxy.Create<IFurnitureFloorLogic>(call =>
            call.Method.Name == "GetState" ? state : null
        );

        return FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name switch
            {
                "get_ObjectId" => id,
                "get_X" => x,
                "get_Y" => y,
                "get_Z" => Altitude.FromInt(z),
                "get_Rotation" => rotation,
                "get_Logic" => logic,
                _ => null,
            }
        );
    }

    /// <summary>A room holding the furni under test, recording what the box asked to change.</summary>
    private sealed class Harness
    {
        public FakeFurniAccess Furni { get; } = new();

        public List<(int ObjectId, int X, int Y, int Rotation, int? Z)> Moved { get; } = [];

        public List<(int ObjectId, int State)> States { get; } = [];

        private readonly FakeRoomLookup _lookup = new();

        private readonly List<WiredFurniStateSnapshot> _snapshots = [];

        public Harness(params IRoomFloorItem[] items)
        {
            foreach (IRoomFloorItem item in items)
            {
                _lookup.ItemsById[item.ObjectId] = item;
            }
        }

        public void Snapshot(int furniId, int x, int y, int z, Rotation rotation, int state) =>
            _snapshots.Add(
                new WiredFurniStateSnapshot
                {
                    FurniId = furniId,
                    X = x,
                    Y = y,
                    Z = z,
                    Rotation = (int)rotation,
                    State = state,
                }
            );

        public TestMatchToSnapshot Box()
        {
            WiredData data = new()
            {
                IntParams = [0, 0, 0, 0],
                StuffIds = [.. _snapshots.Select(s => s.FurniId)],
                Snapshots = _snapshots,
            };

            TestMatchToSnapshot box = new(Context(), data);

            data.AttatchRules(box.GetIntParamRules());

            return box;
        }

        public Task<bool> RunAsync(int state, int direction, int position, int altitude)
        {
            TestMatchToSnapshot box = Box();

            box.Configure(state, direction, position, altitude);

            return box.ExecuteAsync(Execution(), CancellationToken.None);
        }

        private IRoomFloorItemContext Context() =>
            WiredTestBoxes.Context(lookup: _lookup, furniAccess: Furni, map: Map());

        private static IRoomMapAccess Map() =>
            FakeProxy.Create<IRoomMapAccess>(call =>
                call.Method.Name == "ToIdx" ? Idx((int)call.Args![0]!, (int)call.Args![1]!) : null
            );

        private static int Idx(int x, int y) => (x * 1000) + y;

        private IWiredExecutionContext Execution() =>
            FakeProxy.Create<IWiredExecutionContext>(call =>
            {
                switch (call.Method.Name)
                {
                    case "ProcessFloorItemMovementAsync":
                        IRoomFloorItem item = (IRoomFloorItem)call.Args![0]!;
                        int idx = (int)call.Args![1]!;
                        Altitude? height = (Altitude?)call.Args![2];
                        Rotation rotation = (Rotation)call.Args![3]!;

                        Moved.Add(
                            (
                                item.ObjectId.Value,
                                idx / 1000,
                                idx % 1000,
                                (int)rotation,
                                height?.ToInt()
                            )
                        );

                        return Task.FromResult(true);

                    case "ProcessItemStateUpdateAsync":
                        IRoomItem stateItem = (IRoomItem)call.Args![0]!;

                        States.Add((stateItem.ObjectId.Value, (int)call.Args![1]!));

                        return Task.FromResult(true);

                    default:
                        return null;
                }
            });
    }

    internal sealed class TestMatchToSnapshot : WiredActionMatchToSnapshot
    {
        public TestMatchToSnapshot(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;

        public void Configure(int state, int direction, int position, int altitude) =>
            _wiredData.IntParams = [state, direction, position, altitude];
    }
}
