using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.UserDefinedRoomEvents;

/// <summary>
///     <c>WiredObjectInspectionData</c> reads the target id only when the type is Furni (0) or
///     User (1); for Global (-10) it reads none. The target type is echoed straight back from the
///     client's own request, so writing the id unconditionally shifted every variable in the list
///     as soon as the wired menu asked about globals.
/// </summary>
public sealed class WiredVariablesForObjectWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Serialize(WiredVariableTargetType targetType)
    {
        WiredVariablesForObjectEventMessageComposer composer = new()
        {
            TargetType = targetType,
            TargetId = 777,
            VariableValues = [(new WiredVariableId(42), new WiredVariableValue(5))],
            ConfiguredInWireds = [],
        };

        byte[] bytes = Revision
            .Serializers[typeof(WiredVariablesForObjectEventMessageComposer)]
            .Serialize(composer)
            .ToArray();

        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }

    [Fact]
    public void AFurniTargetCarriesItsId()
    {
        ClientPacket body = Serialize(WiredVariableTargetType.Furni);

        body.PopInt().Should().Be(0); // type
        body.PopInt().Should().Be(777, "the client reads an id for Furni");
        body.PopInt().Should().Be(1); // variable count
        body.PopString().Should().Be("42");
        body.PopInt().Should().Be(5);
        body.PopInt().Should().Be(0, "the configured-in-wireds list follows for Furni only");

        body.End.Should().BeTrue();
    }

    [Fact]
    public void AGlobalTargetCarriesNoIdBecauseTheClientReadsNone()
    {
        ClientPacket body = Serialize(WiredVariableTargetType.Global);

        body.PopInt().Should().Be(-10); // type
        body.PopInt()
            .Should()
            .Be(1, "the variable count must come next, not an id the client never reads");
        body.PopString().Should().Be("42");
        body.PopInt().Should().Be(5);

        body.End.Should().BeTrue("no configured-in-wireds list outside Furni");
    }
}
