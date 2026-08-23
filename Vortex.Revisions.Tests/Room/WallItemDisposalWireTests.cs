using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Room;

/// <summary>
/// The bin button on a sticky note. Its packet is a single object id and reads exactly like the
/// several others that are also a single object id, which is why it is worth pinning that this
/// header parses to the disposal message and not to one of its neighbours.
/// </summary>
public sealed class WallItemDisposalWireTests
{
    private const int RemoveItemMessageEvent = 141;

    /// <summary>Its neighbour in shape: also one object id, and it means the opposite.</summary>
    private const int PickupObjectMessageEvent = 1919;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void RemoveItemParser_ReadsTheObjectId()
    {
        ServerPacket sp = new(RemoveItemMessageEvent);
        sp.WriteInteger(90210);

        IMessageEvent parsed = Revision
            .Parsers[RemoveItemMessageEvent]
            .Parse(new ClientPacket(RemoveItemMessageEvent, sp.ToArray()));

        parsed.Should().BeOfType<RemoveItemMessage>();
        ((RemoveItemMessage)parsed).ObjectId.Value.Should().Be(90210);
    }

    [Fact]
    public void RemoveItemIsRegisteredSeparatelyFromPickup()
    {
        // Picking furniture up returns it to its owner; this destroys it. Sharing a parser between
        // the two would make one of those silently do the other.
        Revision.Parsers.Should().ContainKey(RemoveItemMessageEvent);
        Revision
            .Parsers[RemoveItemMessageEvent]
            .Should()
            .NotBeSameAs(Revision.Parsers[PickupObjectMessageEvent]);
    }
}
