using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
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
/// "Remove furni": the selected furniture leaves the room for its owner's inventory.
/// <para>
/// The two things worth pinning are both about what it must <em>not</em> take. A wired box that
/// removes the pile it is standing on deletes itself half way through its own execution; one that
/// removes the wired boxes around it silently dismantles the room's logic.
/// </para>
/// </summary>
public sealed class WiredRemoveFurniTests
{
    private const int Sofa = 10;

    private const int Lamp = 11;

    private const int ATrigger = 12;

    [Fact]
    public async Task ItRemovesTheSelectedFurni()
    {
        Harness harness = new();
        harness.PlaceFurni(Sofa);
        harness.PlaceFurni(Lamp);

        await harness.RunAsync();

        harness.Furni.RemovedFromWired.Should().BeEquivalentTo([Sofa, Lamp]);
    }

    [Fact]
    public async Task ItLeavesTheWiredBoxesAlone()
    {
        // The stack would otherwise delete itself mid-execution, and every action after this one
        // would run against an object the room no longer has.
        Harness harness = new();
        harness.PlaceFurni(Sofa);
        harness.PlaceWiredBox(ATrigger);

        await harness.RunAsync();

        harness.Furni.RemovedFromWired.Should().Equal(Sofa);
    }

    [Fact]
    public async Task AFurniThatIsGone_IsSkipped()
    {
        Harness harness = new();
        harness.Select(Sofa);

        await harness.RunAsync();

        harness.Furni.RemovedFromWired.Should().BeEmpty();
    }

    private sealed class Harness
    {
        public FakeFurniAccess Furni { get; } = new();

        private readonly FakeRoomLookup _lookup = new();

        private readonly List<int> _selected = [];

        public void PlaceFurni(int objectId)
        {
            RoomObjectId id = new(objectId);
            IFurnitureFloorLogic logic = FakeProxy.Create<IFurnitureFloorLogic>(_ => null);

            _lookup.ItemsById[id] = FakeProxy.Create<IRoomFloorItem>(call =>
                call.Method.Name switch
                {
                    "get_ObjectId" => id,
                    "get_Logic" => logic,
                    _ => null,
                }
            );

            Select(objectId);
        }

        /// <summary>A furni whose logic is a wired box, which is what the action has to recognise.
        /// </summary>
        public void PlaceWiredBox(int objectId)
        {
            RoomObjectId id = new(objectId);
            TestWiredBox box = new(WiredTestBoxes.Context(objectId), new WiredData());

            _lookup.ItemsById[id] = FakeProxy.Create<IRoomFloorItem>(call =>
                call.Method.Name switch
                {
                    "get_ObjectId" => id,
                    "get_Logic" => box,
                    _ => null,
                }
            );

            Select(objectId);
        }

        public void Select(int objectId) => _selected.Add(objectId);

        public Task<bool> RunAsync()
        {
            TestRemoveFurni action = new(
                WiredTestBoxes.Context(lookup: _lookup, furniAccess: Furni),
                new WiredData()
            );

            WiredSelectionSet selection = new();

            foreach (int id in _selected)
            {
                selection.SelectedFurniIds.Add(id);
            }

            return action.ExecuteAsync(
                FakeProxy.Create<IWiredExecutionContext>(call =>
                    call.Method.Name == "GetEffectiveSelectionAsync"
                        ? Task.FromResult<IWiredSelectionSet>(selection)
                        : null
                ),
                CancellationToken.None
            );
        }
    }

    private sealed class TestRemoveFurni : WiredActionRemoveFurni
    {
        public TestRemoveFurni(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    private sealed class TestWiredBox : WiredActionRemoveFurni
    {
        public TestWiredBox(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }
}
