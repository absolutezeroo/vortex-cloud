using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Incoming.Userclassification;
using Vortex.Primitives.Messages.Outgoing.Userclassification;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Moderation;

/// <summary>
///     Wire contract for the two mod-tool features that had no server side at all: the room tool's
///     caution/message broadcast, and the staff <c>:uc</c> user-classification commands. Both were
///     decoded from the WIN63-202607011411 client rather than guessed, so these tests pin the
///     layouts that reading was based on.
/// </summary>
public sealed class ModerationWireAdditionsTests
{
    private const int ModToolRoomAlertMessageEvent = 2735;
    private const int RoomUsersClassificationMessageEvent = 157;
    private const int PeerUsersClassificationMessageEvent = 628;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void RoomAlert_ReadsActionTypeAsAFourByteIntAndConsumesTheTrailingString()
    {
        // _SafeCls_3239(param1:int, param2:String, param3:String) -- param1 is a real AS3 int, so it
        // is four bytes and not a one-byte boolean.
        ServerPacket sp = new(ModToolRoomAlertMessageEvent);
        sp.WriteInteger(4).WriteString("clean this up").WriteString(string.Empty);

        ClientPacket packet = new(ModToolRoomAlertMessageEvent, sp.ToArray());

        ModToolRoomAlertMessage message = (ModToolRoomAlertMessage)
            Revision.Parsers[ModToolRoomAlertMessageEvent].Parse(packet);

        message.ActionType.Should().Be(4);
        message.Message.Should().Be("clean this up");

        // Everything after the message must have been consumed, or the next packet in the buffer
        // starts mid-string.
        packet.Remaining.Should().Be(0);
    }

    [Theory]
    // determineAction(isCaution, kickUsers): 0/1 are the caution pair, 3/4 the message pair.
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void RoomAlert_SplitsCautionFromMessageOnTheClientsOwnConstants(
        int actionType,
        bool expectedCaution
    )
    {
        new ModToolRoomAlertMessage { ActionType = actionType, Message = "x" }
            .IsCaution.Should()
            .Be(expectedCaution);
    }

    [Fact]
    public void RoomUsersClassification_ReadsTheClassificationKeyword()
    {
        // _SafeCls_3149(param1:String) -- ":anew" sends the literal "new".
        ServerPacket sp = new(RoomUsersClassificationMessageEvent);
        sp.WriteString("new");

        RoomUsersClassificationMessage message = (RoomUsersClassificationMessage)
            Revision
                .Parsers[RoomUsersClassificationMessageEvent]
                .Parse(new ClientPacket(RoomUsersClassificationMessageEvent, sp.ToArray()));

        message.Classification.Should().Be("new");
    }

    [Fact]
    public void PeerUsersClassification_ReadsTheClassificationKeyword()
    {
        ServerPacket sp = new(PeerUsersClassificationMessageEvent);
        sp.WriteString("paying");

        PeerUsersClassificationMessage message = (PeerUsersClassificationMessage)
            Revision
                .Parsers[PeerUsersClassificationMessageEvent]
                .Parse(new ClientPacket(PeerUsersClassificationMessageEvent, sp.ToArray()));

        message.Classification.Should().Be("paying");
    }

    [Fact]
    public void UserClassification_SerializesCountThenIdNameLabelPerRow()
    {
        // _SafeCls_3183.parse builds two id-keyed maps, so every row is (int, string, string).
        UserClassificationMessageComposer composer = new()
        {
            Entries =
            [
                new UserClassificationEntry(11, "Alice", UserClassifications.New),
                new UserClassificationEntry(12, "Bob", UserClassifications.Paying),
            ],
        };

        ClientPacket body = SerializeBody(composer);

        body.PopInt().Should().Be(2);

        body.PopInt().Should().Be(11);
        body.PopString().Should().Be("Alice");
        body.PopString().Should().Be("new");

        body.PopInt().Should().Be(12);
        body.PopString().Should().Be("Bob");
        body.PopString().Should().Be("paying");

        body.Remaining.Should().Be(0);
    }

    [Fact]
    public void UserClassification_HasARegisteredSerializer()
    {
        // A written-but-unmapped serializer is this repo's most-repeated moderation bug: the packet
        // is dropped by PackageEncoder with nothing but a counter to show for it.
        Revision.Serializers.Should().ContainKey(typeof(UserClassificationMessageComposer));
    }

    [Fact]
    public void RoomAlert_HasARegisteredParser()
    {
        Revision.Parsers.Should().ContainKey(ModToolRoomAlertMessageEvent);
    }

    private static ClientPacket SerializeBody(IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composer.GetType()].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }
}
