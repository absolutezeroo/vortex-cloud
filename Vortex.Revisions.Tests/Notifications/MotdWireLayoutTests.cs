using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Notifications;

/// <summary>
///     Locks the message-of-the-day response byte contract (count then each line). Content now comes
///     from the server-config grain, but the wire shape is unchanged.
/// </summary>
public sealed class MotdWireLayoutTests
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
    public void MotdSerializer_WritesCountThenEachLine()
    {
        MOTDNotificationEventMessageComposer composer = new()
        {
            Messages = new List<string> { "Welcome!", "Rules apply." },
        };

        ClientPacket body = SerializeAndReadBody(
            typeof(MOTDNotificationEventMessageComposer),
            composer
        );

        body.PopInt().Should().Be(2); // count
        body.PopString().Should().Be("Welcome!");
        body.PopString().Should().Be("Rules apply.");
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }
}
