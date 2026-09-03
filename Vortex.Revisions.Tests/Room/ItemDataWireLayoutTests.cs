using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Room;

/// <summary>
/// The four "furni data" requests, against the client classes that write them. All four were empty
/// stubs whose parsers dropped every field, so a sticky note opened blank and saved nothing.
/// </summary>
public sealed class ItemDataWireLayoutTests
{
    private const int GetItemDataEvent = 350;
    private const int SetItemDataEvent = 3498;
    private const int SetObjectDataEvent = 246;
    private const int SetClothingChangeDataEvent = 1220;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    private static ClientPacket SerializeAndReadBody(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        // AbstractSerializer prepends int length (4) + short header (2).
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void GetItemDataParser_ReadsTheItemId()
    {
        ClientPacket packet = BuildClientPacket(GetItemDataEvent, sp => sp.WriteInteger(4242));

        GetItemDataMessage message = Revision
            .Parsers[GetItemDataEvent]
            .Parse(packet)
            .Should()
            .BeOfType<GetItemDataMessage>()
            .Subject;

        message.ItemId.Should().Be(4242);
    }

    [Fact]
    public void SetItemDataParser_ReadsTheColourBeforeTheText()
    {
        // getMessageArray() is [objectId, colorHex, text], and the client's caller is
        // modifyRoomObjectData(objectId, 20, colorHex, text). The composer's constructor assigns its
        // parameters in a different order than it returns them, which is how these two get swapped.
        ClientPacket packet = BuildClientPacket(
            SetItemDataEvent,
            sp =>
            {
                sp.WriteInteger(4242);
                sp.WriteString("FFFF33");
                sp.WriteString("hello world");
            }
        );

        SetItemDataMessage message = Revision
            .Parsers[SetItemDataEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetItemDataMessage>()
            .Subject;

        message.ItemId.Should().Be(4242);
        message.ColorHex.Should().Be("FFFF33");
        message.Text.Should().Be("hello world");
    }

    [Fact]
    public void SetClothingChangeDataParser_ReadsTheGenderThenTheLook()
    {
        ClientPacket packet = BuildClientPacket(
            SetClothingChangeDataEvent,
            sp =>
            {
                sp.WriteInteger(7);
                sp.WriteString("F");
                sp.WriteString("hd-600-1.ch-665-92");
            }
        );

        SetClothingChangeDataMessage message = Revision
            .Parsers[SetClothingChangeDataEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetClothingChangeDataMessage>()
            .Subject;

        message.ItemId.Should().Be(7);
        message.Gender.Should().Be("F");
        message.Look.Should().Be("hd-600-1.ch-665-92");
    }

    [Fact]
    public void SetObjectDataParser_ReadsACountOfStringsNotOfPairs()
    {
        // The client writes `map.length * 2` and then pushes each key and each value, so two pairs
        // announce themselves as FOUR. Reading it as a pair count consumes half the message and
        // leaves the rest to be read as the next packet's header.
        ClientPacket packet = BuildClientPacket(
            SetObjectDataEvent,
            sp =>
            {
                sp.WriteInteger(99);
                sp.WriteInteger(4);
                sp.WriteString("state");
                sp.WriteString("on");
                sp.WriteString("colour");
                sp.WriteString("blue");
            }
        );

        SetObjectDataMessage message = Revision
            .Parsers[SetObjectDataEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetObjectDataMessage>()
            .Subject;

        message.ItemId.Should().Be(99);
        message.Pairs.Should().Equal(("state", "on"), ("colour", "blue"));
    }

    [Fact]
    public void SetObjectDataParser_CannotBeMadeToAllocateOnAnOversizedCount()
    {
        ClientPacket packet = BuildClientPacket(
            SetObjectDataEvent,
            sp =>
            {
                sp.WriteInteger(99);
                sp.WriteInteger(1_000_000_000);
                sp.WriteString("k");
                sp.WriteString("v");
            }
        );

        SetObjectDataMessage message = (SetObjectDataMessage)
            Revision.Parsers[SetObjectDataEvent].Parse(packet);

        message.Pairs.Should().Equal(("k", "v"));
    }

    [Fact]
    public void ItemDataUpdateSerializer_WritesTheIdAsAString()
    {
        // The client reads the id with readString() and casts it, so an integer here shifts every
        // later field: it would take the four id bytes as a length prefix.
        ClientPacket body = SerializeAndReadBody(
            typeof(ItemDataUpdateMessageComposer),
            new ItemDataUpdateMessageComposer { ObjectId = 4242, State = "FFFF33 hello" }
        );

        body.PopString().Should().Be("4242");
        body.PopString().Should().Be("FFFF33 hello");
        body.Remaining.Should().Be(0);
    }
}
