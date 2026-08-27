using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Trading;

/// <summary>
/// Taking a Relic back off the trade table.
/// </summary>
/// <remarks>
/// Until 2026-08-27 an offered Relic could not be removed at all: a fabricated header was deleted —
/// correctly — and the codebase then recorded in two places that the client had no such message. It
/// has one, at 521. These tests exist so that is a tested behaviour rather than a comment.
/// <para>
/// The session is seeded straight into the room's live state, the way the harness already places
/// avatars and pets: what is under test is what happens to an offer, not how the offer got opened.
/// </para>
/// </remarks>
public sealed class RemoveNftFromTradeTests
{
    private static readonly PlayerId Owner = RoomHarness.Owner;
    private static readonly PlayerId Guest = RoomHarness.Stranger;

    private const int Relic = 5001;
    private const int OtherRelic = 5002;

    private static async Task<(RoomHarness Harness, RoomTradeSession Session)> OpenTradeAsync()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.PutRealPlayerInRoom(Owner, 2, 2);
        harness.PutRealPlayerInRoom(Guest, 3, 3);

        harness.OwnedAssets[Owner] = [Relic, OtherRelic];
        harness.OwnedAssets[Guest] = [];

        RoomTradeSession session = new()
        {
            UserOneId = Owner,
            UserTwoId = Guest,
            UserOneObjectId = new RoomObjectId(Owner.Value),
            UserTwoObjectId = new RoomObjectId(Guest.Value),
        };

        session.AssetsOf(Owner).Add(Relic);

        harness.Grain._state.TradeSessionsByPlayerId[Owner] = session;
        harness.Grain._state.TradeSessionsByPlayerId[Guest] = session;

        return (harness, session);
    }

    [Fact]
    public async Task RemovingAnOfferedRelic_TakesItOffTheTable()
    {
        (RoomHarness harness, RoomTradeSession session) = await OpenTradeAsync()
            .ConfigureAwait(true);

        harness.ComposersSentTo.Clear();

        await harness
            .Grain.TradingSystem.RemoveTradeAssetAsync(Owner, Relic, CancellationToken.None)
            .ConfigureAwait(true);

        session.AssetsOf(Owner).Should().BeEmpty();

        // The table is a per-player message, not a room broadcast: each side is sent its own list
        // with the columns the other way round. Both have to be told, or one of them is looking at
        // an offer that no longer exists.
        harness.ComposersSentTo.Should().Contain(Owner).And.Contain(Guest);
    }

    /// <summary>
    /// The one thing that actually matters here. Without the reset a player could pull a Relic after
    /// the other side had accepted, and the offer that settles would not be the one they agreed to.
    /// </summary>
    [Fact]
    public async Task RemovingAnOfferedRelic_ResetsBothAcceptances()
    {
        (RoomHarness harness, RoomTradeSession session) = await OpenTradeAsync()
            .ConfigureAwait(true);

        await harness
            .Grain.TradingSystem.SetTradeAcceptAsync(Guest, true, CancellationToken.None)
            .ConfigureAwait(true);

        session.AcceptedOf(Guest).Should().BeTrue();

        await harness
            .Grain.TradingSystem.RemoveTradeAssetAsync(Owner, Relic, CancellationToken.None)
            .ConfigureAwait(true);

        session.AcceptedOf(Guest).Should().BeFalse();
        session.AcceptedOf(Owner).Should().BeFalse();
    }

    [Fact]
    public async Task RemovingARelicNobodyOffered_ChangesNothing()
    {
        (RoomHarness harness, RoomTradeSession session) = await OpenTradeAsync()
            .ConfigureAwait(true);

        await harness
            .Grain.TradingSystem.SetTradeAcceptAsync(Guest, true, CancellationToken.None)
            .ConfigureAwait(true);

        await harness
            .Grain.TradingSystem.RemoveTradeAssetAsync(Owner, 99_999, CancellationToken.None)
            .ConfigureAwait(true);

        session.AssetsOf(Owner).Should().Equal(Relic);
        // Nothing left the table, so nothing invalidates an acceptance already given.
        session.AcceptedOf(Guest).Should().BeTrue();
    }

    /// <summary>
    /// The offer list is keyed by the requester, so this cannot reach across the table — worth a
    /// test rather than a reading of the code, because it is the whole authorization story.
    /// </summary>
    [Fact]
    public async Task OnePlayerCannotRemoveFromTheOthersOffer()
    {
        (RoomHarness harness, RoomTradeSession session) = await OpenTradeAsync()
            .ConfigureAwait(true);

        await harness
            .Grain.TradingSystem.RemoveTradeAssetAsync(Guest, Relic, CancellationToken.None)
            .ConfigureAwait(true);

        session.AssetsOf(Owner).Should().Equal(Relic);
    }

    [Fact]
    public async Task RemovingOutsideTheBuildingPhase_IsRefused()
    {
        (RoomHarness harness, RoomTradeSession session) = await OpenTradeAsync()
            .ConfigureAwait(true);

        await harness
            .Grain.TradingSystem.SetTradeAcceptAsync(Owner, true, CancellationToken.None)
            .ConfigureAwait(true);
        await harness
            .Grain.TradingSystem.SetTradeAcceptAsync(Guest, true, CancellationToken.None)
            .ConfigureAwait(true);

        session.Phase.Should().Be(TradePhase.Confirming);

        await harness
            .Grain.TradingSystem.RemoveTradeAssetAsync(Owner, Relic, CancellationToken.None)
            .ConfigureAwait(true);

        // The terms freeze once both sides have accepted; changing them here would settle a trade
        // neither party confirmed.
        session.AssetsOf(Owner).Should().Equal(Relic);
    }

    [Fact]
    public async Task RemovingWithNoTradeOpen_IsANoOp()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        await harness
            .Grain.TradingSystem.RemoveTradeAssetAsync(Owner, Relic, CancellationToken.None)
            .ConfigureAwait(true);

        harness.ComposersSentTo.Should().BeEmpty("there is no table to tell anybody about");
    }
}
