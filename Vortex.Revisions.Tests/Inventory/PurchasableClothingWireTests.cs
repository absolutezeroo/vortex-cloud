using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Inventory.Clothing;
using Vortex.Primitives.Messages.Outgoing.Inventory.Clothing;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Inventory;

/// <summary>
///     Binding a clothing furni: request 3637, and the inventory list that answers it.
///
///     There is no result message. The client waits up to five seconds for a FigureSetIds whose
///     bound-furniture list contains the classname it sent, and only then applies the outfit it has
///     already previewed — so the shape of that list is the whole acknowledgement.
/// </summary>
public sealed class PurchasableClothingWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    /// <summary>Repeated rather than referenced: the header table is internal to the revision.</summary>
    private const int RedeemPurchasableClothingMessageEvent = 3637;

    [Fact]
    public void TheRequest_IsTheRoomObjectId()
    {
        ServerPacket request = new(RedeemPurchasableClothingMessageEvent);
        request.WriteInteger(90_210);

        RedeemPurchasableClothingMessage parsed = (RedeemPurchasableClothingMessage)
            Revision
                .Parsers[RedeemPurchasableClothingMessageEvent]
                .Parse(new ClientPacket(RedeemPurchasableClothingMessageEvent, request.ToArray()));

        parsed.RoomObjectId.Should().Be(90_210);
    }

    /// <summary>
    ///     Two lists, sets then names, each a count followed by its values. They are read
    ///     independently — the client asks "do I own set N" and "have I bound furni X" as separate
    ///     questions — so they are not pairs and need not be the same length.
    /// </summary>
    [Fact]
    public void TheAnswer_IsTheSetIdsThenTheBoundNames()
    {
        ClientPacket packet = Body(
            new FigureSetIdsEventMessageComposer
            {
                FigureSetIds = [6310, 6311, 6312],
                BoundFurnitureNames = ["clothing_elvenoutfit"],
            }
        );

        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(6310);
        packet.PopInt().Should().Be(6311);
        packet.PopInt().Should().Be(6312);

        packet.PopInt().Should().Be(1);
        packet.PopString().Should().Be("clothing_elvenoutfit");

        packet.Remaining.Should().Be(0);
    }

    /// <summary>
    ///     The shape a new account gets at login. It has to be sent: the client keeps whatever it
    ///     last heard, and this message is the only thing that ever sets the two lists.
    /// </summary>
    [Fact]
    public void NothingUnlocked_IsStillTwoCounts()
    {
        ClientPacket packet = Body(
            new FigureSetIdsEventMessageComposer
            {
                FigureSetIds = ImmutableArray<int>.Empty,
                BoundFurnitureNames = ImmutableArray<string>.Empty,
            }
        );

        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    private static ClientPacket Body(FigureSetIdsEventMessageComposer composer)
    {
        byte[] bytes = Revision
            .Serializers[typeof(FigureSetIdsEventMessageComposer)]
            .Serialize(composer)
            .ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }
}
