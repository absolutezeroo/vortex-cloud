using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Variables;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The echo variable: a second name for a variable that already exists.
/// <para>
/// It owns no scope of its own — the client's form returns the echoed variable's target as its own —
/// so the interesting cases are all about what it forwards and what it refuses. The last test is the
/// one that matters most: two echoes pointed at each other is a configuration a player can build by
/// hand, and resolving it naively kills the room's activation rather than the box.
/// </para>
/// </summary>
public sealed class WiredEchoVariableTests
{
    private const int EchoBox = 40;

    private const int OtherEchoBox = 41;

    [Fact]
    public void ItRoutesOnTheClientsOwnCode()
    {
        Build(out TestEcho echo, out _, source: null);

        echo.WiredCode.Should().Be(7);
    }

    [Fact]
    public void ItMirrorsTheSourcesValueForTheSameTarget()
    {
        FakeWiredVariable source = new(WiredVariableTargetType.User) { Value = 42 };
        Build(out TestEcho echo, out _, source);

        echo.TryGetValue(
                KeyFor(echo, WiredVariableTargetType.User, 7),
                out WiredVariableValue value
            )
            .Should()
            .BeTrue();

        value.Value.Should().Be(42);

        // Same target id, not the box's: an echo of a user variable stays per-user.
        source.Read.Should().Equal((WiredVariableTargetType.User, 7));
    }

    [Fact]
    public void ItTakesTheSourcesTargetType()
    {
        Build(out TestEcho echo, out _, new FakeWiredVariable(WiredVariableTargetType.Furni));

        echo.GetVarSnapshot().TargetType.Should().Be(WiredVariableTargetType.Furni);
    }

    [Fact]
    public void WithNothingPicked_ItNamesNoTargetAndHoldsNoValue()
    {
        Build(out TestEcho echo, out _, source: null);

        echo.GetVarSnapshot().TargetType.Should().Be(WiredVariableTargetType.None);
        echo.TryGetValue(KeyFor(echo, WiredVariableTargetType.None, 0), out _).Should().BeFalse();
    }

    [Fact]
    public void ItPublishesTheSourcesReadFlagsAndNoneOfItsWriteFlags()
    {
        FakeWiredVariable source = new(WiredVariableTargetType.User)
        {
            Flags =
                WiredVariableFlags.HasValue
                | WiredVariableFlags.CanReadLastUpdateTime
                | WiredVariableFlags.CanWriteValue
                | WiredVariableFlags.CanCreateAndDelete,
        };

        Build(out TestEcho echo, out _, source);

        WiredVariableFlags flags = echo.GetVarSnapshot().Flags;

        flags.Has(WiredVariableFlags.HasValue).Should().BeTrue();
        flags.Has(WiredVariableFlags.CanReadLastUpdateTime).Should().BeTrue();

        // The write side is not established by any client class, so the box does not offer it.
        flags.Has(WiredVariableFlags.CanWriteValue).Should().BeFalse();
        flags.Has(WiredVariableFlags.CanCreateAndDelete).Should().BeFalse();
    }

    [Fact]
    public async Task ItRefusesEveryWrite_AndDoesNotTouchTheSource()
    {
        FakeWiredVariable source = new(WiredVariableTargetType.User) { Value = 42 };
        Build(out TestEcho echo, out _, source);

        WiredVariableKey key = KeyFor(echo, WiredVariableTargetType.User, 7);

        (await echo.GiveValueAsync(key, new WiredVariableValue(1))).Should().BeFalse();
        (await echo.SetValueAsync(null!, key, new WiredVariableValue(1))).Should().BeFalse();
        echo.RemoveValue(key).Should().BeFalse();

        source.Value.Should().Be(42);
        source.Written.Should().BeFalse();
    }

    [Fact]
    public void TwoEchoesPointingAtEachOther_Terminate()
    {
        // Build the cycle by hand: each box's picker holds the other's variable id.
        FakeFurniAccess furni = new();

        TestEcho first = new(
            WiredTestBoxes.Context(objectId: EchoBox, furniAccess: furni),
            Picking(WiredVariableIdBuilder.CreateFromBoxId(OtherEchoBox))
        );

        TestEcho second = new(
            WiredTestBoxes.Context(objectId: OtherEchoBox, furniAccess: furni),
            Picking(WiredVariableIdBuilder.CreateFromBoxId(EchoBox))
        );

        furni.Variables[WiredVariableIdBuilder.CreateFromBoxId(EchoBox)] = first;
        furni.Variables[WiredVariableIdBuilder.CreateFromBoxId(OtherEchoBox)] = second;

        // Not an assertion about the answer so much as about returning at all: without the guard
        // each snapshot builds the other's forever and the room's activation dies on the stack.
        first.GetVarSnapshot().TargetType.Should().Be(WiredVariableTargetType.None);
        first.TryGetValue(KeyFor(first, WiredVariableTargetType.None, 0), out _).Should().BeFalse();
    }

    /// <summary>An echo box with <paramref name="source"/> registered under the id its picker
    /// holds, or with an empty picker when there is none.</summary>
    private static void Build(
        out TestEcho echo,
        out FakeFurniAccess furni,
        FakeWiredVariable? source
    )
    {
        furni = new FakeFurniAccess();

        WiredVariableId sourceId = WiredVariableIdBuilder.CreateFromBoxId(OtherEchoBox);

        if (source is not null)
        {
            furni.Variables[sourceId] = source;
        }

        echo = new TestEcho(
            WiredTestBoxes.Context(objectId: EchoBox, furniAccess: furni),
            source is null ? new WiredData() : Picking(sourceId)
        );
    }

    private static WiredData Picking(WiredVariableId id) =>
        new() { VariableIds = [id.Value.ToString()] };

    private static WiredVariableKey KeyFor(
        TestEcho echo,
        WiredVariableTargetType targetType,
        int targetId
    ) => new(echo.GetVarSnapshot().VariableId, targetType, targetId);

    private sealed class TestEcho : WiredVariableEcho
    {
        public TestEcho(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    /// <summary>A plain variable for the echo to mirror, recording what it was asked for.</summary>
    private sealed class FakeWiredVariable(WiredVariableTargetType targetType) : IWiredVariable
    {
        public int Value { get; set; }

        public bool Written { get; private set; }

        public WiredVariableFlags Flags { get; init; } =
            WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

        public List<(WiredVariableTargetType TargetType, int TargetId)> Read { get; } = [];

        public bool CanBind(in WiredVariableKey key) => key.TargetType == targetType;

        public WiredVariableSnapshot GetVarSnapshot() =>
            new()
            {
                VariableId = WiredVariableIdBuilder.CreateFromBoxId(OtherEchoBox),
                VariableName = "source",
                VariableType = WiredVariableType.Created,
                VariableHash = default,
                AvailabilityType = WiredAvailabilityType.RoomActive,
                TargetType = targetType,
                Flags = Flags,
                TextConnectors = [],
            };

        public bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
        {
            Read.Add((key.TargetType, key.TargetId));
            value = new WiredVariableValue(Value);

            return true;
        }

        public Task<bool> GiveValueAsync(
            WiredVariableKey key,
            WiredVariableValue value,
            bool replace = false
        )
        {
            Written = true;

            return Task.FromResult(true);
        }

        public Task<bool> SetValueAsync(
            IWiredExecutionContext ctx,
            WiredVariableKey key,
            WiredVariableValue value
        )
        {
            Written = true;

            return Task.FromResult(true);
        }

        public bool RemoveValue(WiredVariableKey key)
        {
            Written = true;

            return true;
        }

        public bool TryGetTimestamps(
            in WiredVariableKey key,
            out long createdAtMs,
            out long updatedAtMs
        )
        {
            createdAtMs = 1;
            updatedAtMs = 2;

            return true;
        }
    }
}
