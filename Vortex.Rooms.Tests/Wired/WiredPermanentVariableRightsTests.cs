using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Database.Context;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Who may write a permanent wired variable.
///
/// Nobody, was the answer the server gave: the packet handler checked that a player id and a room id
/// were non-zero and passed everything else straight through, and the grain method took no actor at
/// all. Any connected player could set — or hard-delete — any variable of any target in the hotel,
/// and those variables gate wired chains whose actions include kicking and muting.
///
/// Two client surfaces send this message (the variable manager's detail view and the wired menu's
/// inspection tab) and both arrive at the same handler, which is why the guard belongs on the grain:
/// a grain method is public to the whole cluster, so the wired menu's own precondition — rights in
/// the room it was opened from — has to be re-established here.
/// </summary>
public sealed class WiredPermanentVariableRightsTests
{
    private const int ActionSet = 0;
    private const int ActionDelete = 2;
    private const string VariableId = "score";

    /// <summary>
    /// The harness hands <see cref="RoomHarness.Stranger"/> build rights unless asked not to, which
    /// is the right default for most room tests and exactly wrong for this one.
    /// </summary>
    private static Task<RoomHarness> RoomWhereTheStrangerHoldsNothingAsync() =>
        RoomHarness.CreateAsync(canManipulate: false);

    private static Task<bool> WriteAsync(
        RoomHarness harness,
        Vortex.Primitives.Players.PlayerId actor,
        WiredVariableTargetType targetType,
        int targetId,
        int action
    ) =>
        harness.Grain.SetPermanentVariableAsync(
            harness.ContextFor(actor),
            targetType,
            targetId,
            VariableId,
            7,
            action,
            CancellationToken.None
        );

    private static int StoredCount(RoomHarness harness)
    {
        using VortexDbContext dbCtx = harness.NewDbContext();

        return dbCtx.WiredPermanentVariables.Count(v => v.VariableId == VariableId);
    }

    /// <summary>The control: the owner of the room still gets to write.</summary>
    [Fact]
    public async Task TheRoomOwner_CanWriteAVariable()
    {
        RoomHarness harness = await RoomWhereTheStrangerHoldsNothingAsync().ConfigureAwait(true);

        bool written = await WriteAsync(
                harness,
                RoomHarness.Owner,
                WiredVariableTargetType.User,
                RoomHarness.Owner.Value,
                ActionSet
            )
            .ConfigureAwait(true);

        written.Should().BeTrue();
        StoredCount(harness).Should().Be(1);
    }

    /// <summary>And so does somebody the owner handed rights to.</summary>
    [Fact]
    public async Task APlayerHoldingRights_CanWriteAVariable()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        bool written = await WriteAsync(
                harness,
                RoomHarness.Stranger,
                WiredVariableTargetType.User,
                RoomHarness.Owner.Value,
                ActionSet
            )
            .ConfigureAwait(true);

        written.Should().BeTrue();
        StoredCount(harness).Should().Be(1);
    }

    /// <summary>A visitor holding nothing writes nothing, least of all on somebody else's id.</summary>
    [Fact]
    public async Task AVisitorWithNoRights_WritesNothing()
    {
        RoomHarness harness = await RoomWhereTheStrangerHoldsNothingAsync().ConfigureAwait(true);

        bool written = await WriteAsync(
                harness,
                RoomHarness.Stranger,
                WiredVariableTargetType.User,
                RoomHarness.Owner.Value,
                ActionSet
            )
            .ConfigureAwait(true);

        written.Should().BeFalse();
        StoredCount(harness).Should().Be(0);
    }

    /// <summary>
    /// The delete is a hard delete, so it is the half worth being sure about: a visitor must not be
    /// able to drop a variable the room's wired depends on.
    /// </summary>
    [Fact]
    public async Task AVisitorWithNoRights_CannotDeleteAVariable()
    {
        RoomHarness harness = await RoomWhereTheStrangerHoldsNothingAsync().ConfigureAwait(true);

        await WriteAsync(
                harness,
                RoomHarness.Owner,
                WiredVariableTargetType.User,
                RoomHarness.Owner.Value,
                ActionSet
            )
            .ConfigureAwait(true);

        bool deleted = await WriteAsync(
                harness,
                RoomHarness.Stranger,
                WiredVariableTargetType.User,
                RoomHarness.Owner.Value,
                ActionDelete
            )
            .ConfigureAwait(true);

        deleted.Should().BeFalse();
        StoredCount(harness).Should().Be(1);
    }

    /// <summary>
    /// A furni variable names a furni: the id is otherwise any object id in the hotel, and the owner
    /// of one room could reach into the contents of another.
    /// </summary>
    [Fact]
    public async Task AFurniTargetThatIsNotInTheRoom_IsRefusedEvenForTheOwner()
    {
        RoomHarness harness = await RoomWhereTheStrangerHoldsNothingAsync().ConfigureAwait(true);

        bool written = await WriteAsync(
                harness,
                RoomHarness.Owner,
                WiredVariableTargetType.Furni,
                987654,
                ActionSet
            )
            .ConfigureAwait(true);

        written.Should().BeFalse();
        StoredCount(harness).Should().Be(0);
    }
}
