using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// "Write to logs": the box that makes the rest of the wired log worth reading. The engine's own
/// lines say which action ran; this one is how a builder marks where in their own logic the room
/// got to.
/// </summary>
public sealed class WiredWriteToLogTests
{
    [Theory]
    [InlineData(0, WiredLogLevel.Info)]
    [InlineData(1, WiredLogLevel.Warning)]
    [InlineData(2, WiredLogLevel.Error)]
    public async Task ItWritesTheMessageAtTheChosenLevel(int param, WiredLogLevel expected)
    {
        FakeFurniAccess furni = new();

        await Box(furni, param, "gate opened").ExecuteAsync(Execution(), CancellationToken.None);

        furni.RoomLog.Should().Equal((expected, "gate opened"));
    }

    [Fact]
    public async Task AnEmptyMessageWritesNothing()
    {
        // A log full of blank lines from a box somebody dragged out and never configured is worse
        // than no box.
        FakeFurniAccess furni = new();

        await Box(furni, 0, "   ").ExecuteAsync(Execution(), CancellationToken.None);

        furni.RoomLog.Should().BeEmpty();
    }

    [Fact]
    public async Task ItTrimsWhatItWrites()
    {
        FakeFurniAccess furni = new();

        await Box(furni, 0, "  gate opened \n").ExecuteAsync(Execution(), CancellationToken.None);

        furni.RoomLog.Should().Equal((WiredLogLevel.Info, "gate opened"));
    }

    [Fact]
    public void TheNegativeVariantIsOnTheOtherBranch()
    {
        FakeFurniAccess furni = new();

        TestNegative negative = new(
            WiredTestBoxes.Context(furniAccess: furni),
            new WiredData { IntParams = [0], StringParam = "refused" }
        );

        negative.IsNegative().Should().BeTrue();
        negative.WiredCode.Should().Be(50);
        Box(furni, 0, "ran").WiredCode.Should().Be(49);
    }

    private static TestWriteToLog Box(FakeFurniAccess furni, int level, string message)
    {
        WiredData data = new() { IntParams = [level], StringParam = message };
        TestWriteToLog box = new(WiredTestBoxes.Context(furniAccess: furni), data);

        data.AttatchRules(box.GetIntParamRules());

        return box;
    }

    private static IWiredExecutionContext Execution() =>
        FakeProxy.Create<IWiredExecutionContext>(_ => null);

    private sealed class TestWriteToLog : WiredActionWriteToLog
    {
        public TestWriteToLog(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    private sealed class TestNegative : WiredActionWriteToLogNegative
    {
        public TestNegative(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }
}
