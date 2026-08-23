using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.UserDefinedRoomEvents;

/// <summary>
///     The client stakes furniture on a chest deposit with <c>[remove, count, ...ids]</c>, and a
///     counted list read one field out of step takes the rest of the message with it.
/// </summary>
/// <remarks>
///     Written from the client's own composer — <c>WiredTradeUpdateItemsComposer</c> pushes
///     <c>[remove, itemIds.length, ...itemIds]</c> — rather than from a capture, because the
///     message is new on this side and there is nothing yet to capture. The bytes below are what
///     that composer produces.
/// </remarks>
public sealed class WiredTradeUpdateItemsTests
{
    private const int WiredTradeUpdateItemsEvent = 3111;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void AddingTwoItems_ReadsTheFlagThenEveryId()
    {
        WiredTradeUpdateItemsMessage message = Parse(remove: false, ids: [2033, 2034]);

        message.Remove.Should().BeFalse();
        message.ItemIds.Should().Equal(2033, 2034);
    }

    [Fact]
    public void RemovingOne_ReadsTheFlagThatTellsTheTwoApart()
    {
        WiredTradeUpdateItemsMessage message = Parse(remove: true, ids: [2033]);

        message.Remove.Should().BeTrue();
        message.ItemIds.Should().Equal(2033);
    }

    /// <summary>An empty stake is legal — the client sends it when the last item comes off.</summary>
    [Fact]
    public void AnEmptyList_LeavesNothingUnread()
    {
        WiredTradeUpdateItemsMessage message = Parse(remove: true, ids: []);

        message.ItemIds.Should().BeEmpty();
    }

    private static WiredTradeUpdateItemsMessage Parse(bool remove, int[] ids)
    {
        List<byte> body = [(byte)(remove ? 1 : 0)];

        body.AddRange(Int32(ids.Length));

        foreach (int id in ids)
        {
            body.AddRange(Int32(id));
        }

        List<byte> frame =
        [
            .. Int32(body.Count + 2),
            .. Int16(WiredTradeUpdateItemsEvent),
            .. body,
        ];

        ClientPacket packet = new(-1, frame.ToArray());

        _ = packet.PopInt();
        packet.Header = packet.PopShort();

        return Revision
            .Parsers[WiredTradeUpdateItemsEvent]
            .Parse(packet)
            .Should()
            .BeOfType<WiredTradeUpdateItemsMessage>()
            .Subject;
    }

    private static byte[] Int32(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);

        Array.Reverse(bytes);

        return bytes;
    }

    private static byte[] Int16(int value)
    {
        byte[] bytes = BitConverter.GetBytes((short)value);

        Array.Reverse(bytes);

        return bytes;
    }
}
