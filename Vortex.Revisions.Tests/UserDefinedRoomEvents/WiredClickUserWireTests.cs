using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.UserDefinedRoomEvents;

/// <summary>
///     The wired click-user round trip and the Inspection tab's second door onto the variable write.
/// </summary>
/// <remarks>
///     Every id and field order here was read out of the WIN63 client's own registry and composer
///     classes, not off a header comment. Two of the four ids had nothing bound to them at all, so
///     there was no existing behaviour to compare against and nothing but the client to be right
///     about.
/// </remarks>
public sealed class WiredClickUserWireTests
{
    // _composers[1953] = _SafeCls_2111(int) — HabboUserDefinedRoomEvents::userSelected()
    private const int WiredClickUserMessageEvent = 1953;

    // _composers[625] = _SafeCls_2426(...) — VariableManagementDetailView
    private const int WiredSetObjectVariableValueMessageEvent = 625;

    // _composers[689] = _SafeCls_3855(...) — WiredMenuInspectionTab
    private const int WiredSetObjectVariableValueFromInspectorMessageEvent = 689;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Body(IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composer.GetType()].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }

    private static ClientPacket Packet(params byte[] body) => new(0, body);

    private static byte[] Int32(int value) =>
        [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    [Fact]
    public void ClickUser_ReadsTheClickedObjectId()
    {
        IMessageEvent parsed = Revision
            .Parsers[WiredClickUserMessageEvent]
            .Parse(Packet([.. Int32(42)]));

        parsed.Should().BeOfType<WiredClickUserMessage>().Which.ObjectId.Should().Be(42);
    }

    /// <summary>
    ///     The Inspection tab sends the same five fields on its own id. Both doors have to land on
    ///     the same message, or an edit made from one tab works and the identical edit from the
    ///     other is dropped without a word.
    /// </summary>
    [Fact]
    public void BothVariableWriteHeaders_ParseToTheSameMessage()
    {
        byte[] body =
        [
            .. Int32(2), // entityType
            .. Int32(17), // entityId
            0,
            3,
            (byte)'m',
            (byte)'a',
            (byte)'x', // variableId
            .. Int32(99), // value
            .. Int32(2), // mode: delete
        ];

        WiredSetObjectVariableValueMessage FromHeader(int header) =>
            Revision
                .Parsers[header]
                .Parse(Packet(body))
                .Should()
                .BeOfType<WiredSetObjectVariableValueMessage>()
                .Subject;

        WiredSetObjectVariableValueMessage management = FromHeader(
            WiredSetObjectVariableValueMessageEvent
        );
        WiredSetObjectVariableValueMessage inspector = FromHeader(
            WiredSetObjectVariableValueFromInspectorMessageEvent
        );

        inspector.Should().BeEquivalentTo(management);
        inspector.EntityType.Should().Be(2);
        inspector.EntityId.Should().Be(17);
        inspector.VariableId.Should().Be("max");
        inspector.Value.Should().Be(99);
        inspector.Action.Should().Be(2);
    }

    /// <summary>
    ///     _SafeCls_3523: an int then a boolean. The index is what the client compares against the
    ///     click it is still waiting on, so an answer that does not echo it opens no menu at all.
    /// </summary>
    [Fact]
    public void ClickUserResponse_WritesIndexThenOpenMenu()
    {
        ClientPacket packet = Body(
            new WiredClickUserResponseMessageComposer { Index = 7, OpenMenu = true }
        );

        packet.PopInt().Should().Be(7);
        packet.PopBoolean().Should().BeTrue();
    }

    /// <summary>
    ///     _SafeCls_3496: a boolean, then — only if bytes remain — a count and that many strings.
    ///     The count is written even when the list is empty, so the reader never has to tell an
    ///     empty list from a sender that stopped early.
    /// </summary>
    [Fact]
    public void Environment_WritesTheFlagThenACountedListOfAchievements()
    {
        ClientPacket empty = Body(new WiredEnvironmentMessageComposer { HasClickUserWired = true });

        empty.PopBoolean().Should().BeTrue();
        empty.PopInt().Should().Be(0);

        ClientPacket named = Body(
            new WiredEnvironmentMessageComposer
            {
                HasClickUserWired = false,
                EnabledAchievements = ["RoomDecoFurniCount", "AllTimeHotelPresence"],
            }
        );

        named.PopBoolean().Should().BeFalse();
        named.PopInt().Should().Be(2);
        named.PopString().Should().Be("RoomDecoFurniCount");
        named.PopString().Should().Be("AllTimeHotelPresence");
    }
}
