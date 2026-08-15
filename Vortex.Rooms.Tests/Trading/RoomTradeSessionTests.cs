using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Grains;
using Xunit;

namespace Vortex.Rooms.Tests.Trading;

/// <summary>Locks the pure trade-session state machine the <c>RoomGrain</c> relies on: per-side
/// isolation of offers, the accept → confirm progression, and the rule that any change to an offer
/// invalidates both parties' agreement. These are the invariants that keep a trade from committing
/// on stale terms.</summary>
public sealed class RoomTradeSessionTests
{
    private static readonly PlayerId One = new(101);
    private static readonly PlayerId Two = new(202);

    private static RoomTradeSession NewSession() =>
        new()
        {
            UserOneId = One,
            UserTwoId = Two,
            UserOneObjectId = new RoomObjectId(1),
            UserTwoObjectId = new RoomObjectId(2),
        };

    [Fact]
    public void Participants_AndOther_ResolveBothSides()
    {
        RoomTradeSession session = NewSession();

        session.IsParticipant(One).Should().BeTrue();
        session.IsParticipant(Two).Should().BeTrue();
        session.IsParticipant(new PlayerId(999)).Should().BeFalse();

        session.OtherOf(One).Should().Be(Two);
        session.OtherOf(Two).Should().Be(One);
        session.ObjectIdOf(One).Should().Be(new RoomObjectId(1));
        session.ObjectIdOf(Two).Should().Be(new RoomObjectId(2));
    }

    [Fact]
    public void Offers_AreIsolatedPerSide()
    {
        RoomTradeSession session = NewSession();

        session.ItemsOf(One).Add(10);
        session.ItemsOf(One).Add(11);
        session.ItemsOf(Two).Add(20);

        session.ItemsOf(One).Should().Equal(10, 11);
        session.ItemsOf(Two).Should().Equal(20);
    }

    /// <summary>
    ///     Relics are held apart from the furniture ids: they live in another table, travel on
    ///     their own message, and the answer that carries them is written from the receiver's point
    ///     of view — so which side a Relic is on has to be unambiguous here.
    /// </summary>
    [Fact]
    public void RelicOffers_AreIsolatedPerSide_AndSeparateFromFurniture()
    {
        RoomTradeSession session = NewSession();

        session.ItemsOf(One).Add(10);
        session.AssetsOf(One).Add(500);
        session.AssetsOf(Two).Add(600);

        session.AssetsOf(One).Should().Equal(500);
        session.AssetsOf(Two).Should().Equal(600);

        // The same number can be a furniture id on one side and an asset id on the other: they
        // are keys of two different tables and must never be pooled.
        session.ItemsOf(One).Should().Equal(10);
        session.ItemsOf(Two).Should().BeEmpty();
    }

    [Fact]
    public void BothAccepted_OnlyWhenEachSideAccepts()
    {
        RoomTradeSession session = NewSession();

        session.SetAccepted(One, true);
        session.BothAccepted.Should().BeFalse();

        session.SetAccepted(Two, true);
        session.BothAccepted.Should().BeTrue();

        session.AcceptedOf(One).Should().BeTrue();
        session.AcceptedOf(Two).Should().BeTrue();
    }

    [Fact]
    public void BothConfirmed_OnlyWhenEachSideConfirms()
    {
        RoomTradeSession session = NewSession();

        session.SetConfirmed(One, true);
        session.BothConfirmed.Should().BeFalse();

        session.SetConfirmed(Two, true);
        session.BothConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ResetAgreement_ClearsAcceptanceAndReturnsToBuilding()
    {
        RoomTradeSession session = NewSession();

        session.SetAccepted(One, true);
        session.SetAccepted(Two, true);
        session.SetConfirmed(One, true);
        session.Phase = TradePhase.Confirming;

        session.ResetAgreement();

        session.UserOneAccepted.Should().BeFalse();
        session.UserTwoAccepted.Should().BeFalse();
        session.UserOneConfirmed.Should().BeFalse();
        session.UserTwoConfirmed.Should().BeFalse();
        session.BothAccepted.Should().BeFalse();
        session.BothConfirmed.Should().BeFalse();
        session.Phase.Should().Be(TradePhase.Building);
    }

    [Fact]
    public void SetAccepted_TogglesOnlyTheGivenSide()
    {
        RoomTradeSession session = NewSession();

        session.SetAccepted(One, true);

        session.AcceptedOf(One).Should().BeTrue();
        session.AcceptedOf(Two).Should().BeFalse();

        session.SetAccepted(One, false);
        session.AcceptedOf(One).Should().BeFalse();
    }
}
