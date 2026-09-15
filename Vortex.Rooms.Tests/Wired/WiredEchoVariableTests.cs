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

    /// <summary>The mirrored variable's id: the echo's picker holds this, and every write the echo
    /// forwards has to arrive under it.</summary>
    private static readonly WiredVariableId SourceId = WiredVariableIdBuilder.CreateFromBoxId(
        OtherEchoBox
    );

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
    public void ItPublishesTheSourcesFlagsUnmasked()
    {
        // The write flags matter as much as the read ones: the box exists so the variable add-ons
        // can stack on a variable that is not user-created, and that includes editing it through the
        // give/remove/modify effects when the original supports modification.
        FakeWiredVariable source = new(WiredVariableTargetType.User)
        {
            Flags =
                WiredVariableFlags.HasValue
                | WiredVariableFlags.CanReadLastUpdateTime
                | WiredVariableFlags.CanWriteValue
                | WiredVariableFlags.CanCreateAndDelete,
        };

        Build(out TestEcho echo, out _, source);

        echo.GetVarSnapshot().Flags.Should().Be(source.Flags);
    }

    [Fact]
    public void MirroringNothing_ItOffersAValueSlotAndNoOperations()
    {
        Build(out TestEcho echo, out _, source: null);

        WiredVariableFlags flags = echo.GetVarSnapshot().Flags;

        flags.Has(WiredVariableFlags.CanWriteValue).Should().BeFalse();
        flags.Has(WiredVariableFlags.CanCreateAndDelete).Should().BeFalse();
    }

    [Fact]
    public async Task ItsWritesReachTheSource_UnderTheSourcesName()
    {
        FakeWiredVariable source = new(WiredVariableTargetType.User) { Value = 42 };
        Build(out TestEcho echo, out _, source);

        WiredVariableKey key = KeyFor(echo, WiredVariableTargetType.User, 7);

        (await echo.GiveValueAsync(key, new WiredVariableValue(1))).Should().BeTrue();
        (await echo.SetValueAsync(null!, key, new WiredVariableValue(2))).Should().BeTrue();
        echo.RemoveValue(key).Should().BeTrue();

        // Rebound onto the mirrored variable, same target: nothing is stored on the echo, so a write
        // that kept the echo's own id would land in a slot no other box can read.
        source
            .Written.Should()
            .Equal(
                (SourceId, WiredVariableTargetType.User, 7),
                (SourceId, WiredVariableTargetType.User, 7),
                (SourceId, WiredVariableTargetType.User, 7)
            );
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
    /// <summary>
    /// The echo names the variable it mirrors, which is what lets the room restate that variable's
    /// changes under this box's name.
    /// </summary>
    /// <remarks>
    /// Without it the box was offered in the "variable changed" picker — it republishes the source's
    /// flags unmasked, <c>CanInterceptChanges</c> included — and could never fire: the write is a
    /// pass-through, so the only change event raised carries the source's id.
    /// </remarks>
    [Fact]
    public void ItNamesTheVariableItMirrors_SoTheRoomCanRestateItsChanges()
    {
        FakeWiredVariable source = new(WiredVariableTargetType.User) { Value = 42 };
        Build(out TestEcho echo, out _, source);

        echo.SourceVariableId.Should().Be(SourceId);
        echo.ValueFor(42).Should().Be(42, "an echo is the same number under another name");
    }

    [Fact]
    public void MirroringNothing_ItNamesNoSource()
    {
        Build(out TestEcho echo, out _, source: null);

        echo.SourceVariableId.Should().Be(default(WiredVariableId));
    }

    private static void Build(
        out TestEcho echo,
        out FakeFurniAccess furni,
        FakeWiredVariable? source
    )
    {
        furni = new FakeFurniAccess();

        if (source is not null)
        {
            furni.Variables[SourceId] = source;
        }

        echo = new TestEcho(
            WiredTestBoxes.Context(objectId: EchoBox, furniAccess: furni),
            source is null ? new WiredData() : Picking(SourceId)
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

        /// <summary>Every write the echo forwarded, as the key it arrived under.</summary>
        public List<(
            WiredVariableId Id,
            WiredVariableTargetType TargetType,
            int TargetId
        )> Written { get; } = [];

        public WiredVariableFlags Flags { get; init; } =
            WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

        public List<(WiredVariableTargetType TargetType, int TargetId)> Read { get; } = [];

        public bool CanBind(in WiredVariableKey key) => key.TargetType == targetType;

        public WiredVariableSnapshot GetVarSnapshot() =>
            new()
            {
                VariableId = SourceId,
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
            Written.Add((key.VariableId, key.TargetType, key.TargetId));

            return Task.FromResult(true);
        }

        public Task<bool> SetValueAsync(
            IWiredExecutionContext ctx,
            WiredVariableKey key,
            WiredVariableValue value
        )
        {
            Written.Add((key.VariableId, key.TargetType, key.TargetId));

            return Task.FromResult(true);
        }

        public bool RemoveValue(WiredVariableKey key)
        {
            Written.Add((key.VariableId, key.TargetType, key.TargetId));

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
