using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Help;

/// <summary>
///     Only the room-based report was ever parsed; the other five entry points produced empty
///     messages, so reporting from an IM, a photo, a selfie or a guild forum reached the server as
///     nothing at all. Two of the five reorder their arguments on the way out — reportPhoto and
///     reportSelfie both put the topic and the message somewhere other than where their own method
///     signature suggests — which is exactly the kind of thing a round-trip test pins down.
/// </summary>
public sealed class CallForHelpVariantWireTests
{
    private const int CallForHelpFromIMMessageEvent = 838;
    private const int CallForHelpFromPhotoMessageEvent = 1964;
    private const int CallForHelpFromSelfieMessageEvent = 201;
    private const int CallForHelpFromForumThreadMessageEvent = 380;
    private const int CallForHelpFromForumMessageMessageEvent = 2991;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    private static T Parse<T>(int header, Action<ServerPacket> write)
        where T : class =>
        Revision
            .Parsers[header]
            .Parse(BuildClientPacket(header, write))
            .Should()
            .BeOfType<T>()
            .Subject;

    [Fact]
    public void FromImParser_ReadsTheEvidenceLinesAsUserIdAndTextPairs()
    {
        CallForHelpFromIMMessage message = Parse<CallForHelpFromIMMessage>(
            CallForHelpFromIMMessageEvent,
            sp =>
                sp.WriteString("they keep messaging me")
                    .WriteInteger(4)
                    .WriteInteger(77)
                    .WriteInteger(2)
                    .WriteInteger(77)
                    .WriteString("go away")
                    .WriteInteger(77)
                    .WriteString("seriously")
        );

        message.Message.Should().Be("they keep messaging me");
        message.TopicId.Should().Be(4);
        message.ReportedUserId.Should().Be(77);
        message.Evidence.Should().HaveCount(2);
        message.Evidence[0].UserId.Should().Be(77);
        message.Evidence[0].Text.Should().Be("go away");
        message.Evidence[1].Text.Should().Be("seriously");
    }

    [Fact]
    public void FromImParser_AcceptsAReportWithNoEvidenceSelected()
    {
        CallForHelpFromIMMessage message = Parse<CallForHelpFromIMMessage>(
            CallForHelpFromIMMessageEvent,
            sp => sp.WriteString("no context").WriteInteger(4).WriteInteger(77).WriteInteger(0)
        );

        message.Evidence.Should().BeEmpty();
    }

    [Fact]
    public void FromPhotoParser_ReadsTheTopicFourthNotSecond()
    {
        // reportPhoto(photoId, topicId, roomId, authorId, furniId) sends them as
        // (photoId, roomId, authorId, topicId, furniId).
        CallForHelpFromPhotoMessage message = Parse<CallForHelpFromPhotoMessage>(
            CallForHelpFromPhotoMessageEvent,
            sp =>
                sp.WriteString("photo-123")
                    .WriteInteger(512)
                    .WriteInteger(77)
                    .WriteInteger(9)
                    .WriteInteger(4711)
        );

        message.PhotoId.Should().Be("photo-123");
        message.RoomId.Should().Be(512);
        message.PhotoAuthorId.Should().Be(77);
        message.TopicId.Should().Be(9);
        message.FurniId.Should().Be(4711);
    }

    [Fact]
    public void FromSelfieParser_ReadsTheMessageFourthNotSecond()
    {
        // reportSelfie(url, message, roomId, authorId, furniId) sends them as
        // (url, roomId, authorId, message, furniId).
        CallForHelpFromSelfieMessage message = Parse<CallForHelpFromSelfieMessage>(
            CallForHelpFromSelfieMessageEvent,
            sp =>
                sp.WriteString("https://example.test/s/1")
                    .WriteInteger(512)
                    .WriteInteger(77)
                    .WriteString("this is not ok")
                    .WriteInteger(4711)
        );

        message.Url.Should().Be("https://example.test/s/1");
        message.RoomId.Should().Be(512);
        message.PhotoAuthorId.Should().Be(77);
        message.Message.Should().Be("this is not ok");
        message.FurniId.Should().Be(4711);
    }

    [Fact]
    public void FromForumThreadParser_ReadsTheGuildThreadTopicAndMessage()
    {
        CallForHelpFromForumThreadMessage message = Parse<CallForHelpFromForumThreadMessage>(
            CallForHelpFromForumThreadMessageEvent,
            sp =>
                sp.WriteInteger(12)
                    .WriteInteger(34)
                    .WriteInteger(9)
                    .WriteString("this whole thread is abusive")
        );

        message.GroupId.Should().Be(12);
        message.ThreadId.Should().Be(34);
        message.TopicId.Should().Be(9);
        message.Message.Should().Be("this whole thread is abusive");
    }

    [Fact]
    public void FromForumMessageParser_ReadsThePostIdBetweenTheThreadAndTheTopic()
    {
        CallForHelpFromForumMessageMessage message = Parse<CallForHelpFromForumMessageMessage>(
            CallForHelpFromForumMessageMessageEvent,
            sp =>
                sp.WriteInteger(12)
                    .WriteInteger(34)
                    .WriteInteger(56)
                    .WriteInteger(9)
                    .WriteString("this reply is abusive")
        );

        message.GroupId.Should().Be(12);
        message.ThreadId.Should().Be(34);
        message.PostId.Should().Be(56);
        message.TopicId.Should().Be(9);
        message.Message.Should().Be("this reply is abusive");
    }
}
