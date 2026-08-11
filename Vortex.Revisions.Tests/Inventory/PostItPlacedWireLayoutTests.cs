using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Outgoing.Inventory.Furni;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Inventory;

/// <summary>
///     Locks the post-it byte contract at two ints.
///
///     The serializer's body was empty while the header stayed registered, so this message went
///     out with a zero-length payload and the client
///     (unknowns/_SafePkg_2514/_SafeCls_2620.as) read two ints off the end of it. That is a desync
///     on the first post-it placed, not a missing feature, and nothing about the C# side looked
///     wrong from the C# side alone.
/// </summary>
public sealed class PostItPlacedWireLayoutTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket SerializeAndReadBody(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void PostItPlaced_WritesItemIdThenItemsLeft()
    {
        ClientPacket packet = SerializeAndReadBody(
            typeof(PostItPlacedEventMessageComposer),
            new PostItPlacedEventMessageComposer { ItemId = 4711, ItemsLeft = 17 }
        );

        packet.PopInt().Should().Be(4711);
        packet.PopInt().Should().Be(17);
    }

    /// <summary>An emptied stack still reports zero rather than sending nothing.</summary>
    [Fact]
    public void PostItPlaced_LastSheetStillWritesBothFields()
    {
        ClientPacket packet = SerializeAndReadBody(
            typeof(PostItPlacedEventMessageComposer),
            new PostItPlacedEventMessageComposer { ItemId = 1, ItemsLeft = 0 }
        );

        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(0);
    }
}
