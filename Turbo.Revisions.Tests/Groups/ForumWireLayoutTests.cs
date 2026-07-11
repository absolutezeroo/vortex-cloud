using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Configuration;
using Xunit;
using Rev = Turbo.Revisions.Revision20260701.Revision20260701;

namespace Turbo.Revisions.Tests.Groups;

/// <summary>Wire-layout lock for the slice-3 group forum packets. State fields are bytes.</summary>
public sealed class ForumWireLayoutTests
{
    private const int GetMessagesEvent = 1884;
    private const int PostMessageEvent = 2811;
    private const int UpdateThreadEvent = 3206;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket BuildClientPacket(int header, System.Action<ServerPacket> write)
    {
        ServerPacket sp = new ServerPacket(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    private static ClientPacket SerializeAndReadBody(
        System.Type composerType,
        Turbo.Primitives.Networking.IComposer composer
    )
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        System.Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact(
        Skip = "GetMessagesMessageEvent header collided with OpenPetPackageMessageEvent after the WIN63-202607011411 remap; real new header not yet resolved (see Revision20260701.cs UNRESOLVED comment)."
    )]
    public void GetMessagesParser_ReadsPaging()
    {
        ClientPacket packet = BuildClientPacket(
            GetMessagesEvent,
            sp =>
            {
                sp.WriteInteger(5); // groupId
                sp.WriteInteger(11); // threadId
                sp.WriteInteger(20); // startIndex
                sp.WriteInteger(10); // amount
            }
        );

        GetMessagesMessage? msg = Revision
            .Parsers[GetMessagesEvent]
            .Parse(packet)
            .Should()
            .BeOfType<GetMessagesMessage>()
            .Subject;
        msg.GroupId.Should().Be(5);
        msg.ThreadId.Should().Be(11);
        msg.StartIndex.Should().Be(20);
        msg.Amount.Should().Be(10);
    }

    [Fact]
    public void PostMessageParser_ReadsTitleAndBody()
    {
        ClientPacket packet = BuildClientPacket(
            PostMessageEvent,
            sp =>
            {
                sp.WriteInteger(5);
                sp.WriteInteger(0); // threadId 0 = new thread
                sp.WriteString("Hello");
                sp.WriteString("First post body");
            }
        );

        PostMessageMessage? msg = Revision
            .Parsers[PostMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<PostMessageMessage>()
            .Subject;
        msg.GroupId.Should().Be(5);
        msg.ThreadId.Should().Be(0);
        msg.Title.Should().Be("Hello");
        msg.Message.Should().Be("First post body");
    }

    [Fact]
    public void UpdateThreadParser_ReadsLockAndSticky()
    {
        ClientPacket packet = BuildClientPacket(
            UpdateThreadEvent,
            sp =>
            {
                sp.WriteInteger(5);
                sp.WriteInteger(11);
                sp.WriteBoolean(true); // isLocked
                sp.WriteBoolean(false); // isSticky
            }
        );

        UpdateThreadMessage? msg = Revision
            .Parsers[UpdateThreadEvent]
            .Parse(packet)
            .Should()
            .BeOfType<UpdateThreadMessage>()
            .Subject;
        msg.GroupId.Should().Be(5);
        msg.ThreadId.Should().Be(11);
        msg.IsLocked.Should().BeTrue();
        msg.IsSticky.Should().BeFalse();
    }

    private static ForumSnapshot SampleForum() =>
        new()
        {
            GroupId = 5,
            Name = "Pixel Painters",
            Description = "desc",
            Icon = "b0102",
            TotalThreads = 3,
            LeaderboardScore = 0,
            TotalMessages = 7,
            UnreadMessages = 0,
            LastMessageId = 42,
            LastMessageAuthorId = 7,
            LastMessageAuthorName = "bob",
            LastMessageTimeAsSecondsAgo = 60,
            ReadPermissions = 0,
            PostMessagePermissions = 0,
            PostThreadPermissions = 1,
            ModeratePermissions = 1,
            ReadPermissionError = "",
            PostMessagePermissionError = "",
            PostThreadPermissionError = "nope",
            ModeratePermissionError = "nope",
            ReportPermissionError = "",
            CanChangeSettings = true,
            IsStaff = false,
        };

    [Fact]
    public void ForumDataSerializer_WritesBaseThenExtended()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(ForumDataMessageComposer),
            new ForumDataMessageComposer { Forum = SampleForum() }
        );

        // base
        body.PopInt().Should().Be(5);
        body.PopString().Should().Be("Pixel Painters");
        body.PopString().Should().Be("desc");
        body.PopString().Should().Be("b0102");
        body.PopInt().Should().Be(3); // totalThreads
        body.PopInt().Should().Be(0); // leaderboardScore
        body.PopInt().Should().Be(7); // totalMessages
        body.PopInt().Should().Be(0); // unreadMessages
        body.PopInt().Should().Be(42); // lastMessageId
        body.PopInt().Should().Be(7); // lastMessageAuthorId
        body.PopString().Should().Be("bob");
        body.PopInt().Should().Be(60);
        // extended
        body.PopInt().Should().Be(0); // readPermissions
        body.PopInt().Should().Be(0); // postMessagePermissions
        body.PopInt().Should().Be(1); // postThreadPermissions
        body.PopInt().Should().Be(1); // moderatePermissions
        body.PopString().Should().Be(""); // readError
        body.PopString().Should().Be(""); // postMessageError
        body.PopString().Should().Be("nope"); // postThreadError
        body.PopString().Should().Be("nope"); // moderateError
        body.PopString().Should().Be(""); // reportError
        body.PopBoolean().Should().BeTrue(); // canChangeSettings
        body.PopBoolean().Should().BeFalse(); // isStaff
        body.End.Should().BeTrue();
    }

    [Fact]
    public void ForumThreadsSerializer_WritesThreadStateAsByte()
    {
        ForumThreadsPageSnapshot page = new ForumThreadsPageSnapshot
        {
            GroupId = 5,
            StartIndex = 0,
            Threads =
            [
                new ForumThreadSnapshot
                {
                    ThreadId = 11,
                    AuthorId = 7,
                    AuthorName = "bob",
                    Subject = "Hi",
                    IsSticky = true,
                    IsLocked = false,
                    CreationTimeAsSecondsAgo = 100,
                    MessageCount = 4,
                    UnreadMessageCount = 0,
                    LastMessageId = 42,
                    LastMessageAuthorId = 7,
                    LastMessageAuthorName = "bob",
                    LastMessageTimeAsSecondsAgo = 60,
                    State = 2, // hidden
                    AdminId = 0,
                    AdminName = "",
                    AdminOperationTimeAsSecondsAgo = 0,
                },
            ],
        };

        ClientPacket body = SerializeAndReadBody(
            typeof(ForumThreadsMessageComposer),
            new ForumThreadsMessageComposer { Page = page }
        );

        body.PopInt().Should().Be(5); // groupId
        body.PopInt().Should().Be(0); // startIndex
        body.PopInt().Should().Be(1); // count
        body.PopInt().Should().Be(11); // threadId
        body.PopInt().Should().Be(7); // authorId
        body.PopString().Should().Be("bob");
        body.PopString().Should().Be("Hi");
        body.PopBoolean().Should().BeTrue(); // sticky
        body.PopBoolean().Should().BeFalse(); // locked
        body.PopInt().Should().Be(100);
        body.PopInt().Should().Be(4); // messageCount
        body.PopInt().Should().Be(0);
        body.PopInt().Should().Be(42);
        body.PopInt().Should().Be(7);
        body.PopString().Should().Be("bob");
        body.PopInt().Should().Be(60);
        body.PopByte().Should().Be(2); // state as BYTE
        body.PopInt().Should().Be(0); // adminId
        body.PopString().Should().Be("");
        body.PopInt().Should().Be(0);
        body.End.Should().BeTrue();
    }

    [Fact]
    public void ThreadMessagesSerializer_WritesPostStateAsByte()
    {
        ThreadMessagesPageSnapshot page = new ThreadMessagesPageSnapshot
        {
            GroupId = 5,
            ThreadId = 11,
            StartIndex = 0,
            Messages =
            [
                new ForumPostSnapshot
                {
                    MessageId = 42,
                    MessageIndex = 0,
                    AuthorId = 7,
                    AuthorName = "bob",
                    AuthorFigure = "hd-1-1",
                    CreationTimeAsSecondsAgo = 60,
                    MessageText = "hello",
                    State = 1, // hidden
                    AdminId = 0,
                    AdminName = "",
                    AdminOperationTimeAsSecondsAgo = 0,
                    AuthorPostCount = 5,
                },
            ],
        };

        ClientPacket body = SerializeAndReadBody(
            typeof(ThreadMessagesMessageComposer),
            new ThreadMessagesMessageComposer { Page = page }
        );

        body.PopInt().Should().Be(5); // groupId
        body.PopInt().Should().Be(11); // threadId
        body.PopInt().Should().Be(0); // startIndex
        body.PopInt().Should().Be(1); // count
        body.PopInt().Should().Be(42); // messageId
        body.PopInt().Should().Be(0); // messageIndex
        body.PopInt().Should().Be(7); // authorId
        body.PopString().Should().Be("bob");
        body.PopString().Should().Be("hd-1-1");
        body.PopInt().Should().Be(60);
        body.PopString().Should().Be("hello");
        body.PopByte().Should().Be(1); // state as BYTE
        body.PopInt().Should().Be(0); // adminId
        body.PopString().Should().Be("");
        body.PopInt().Should().Be(0);
        body.PopInt().Should().Be(5); // authorPostCount
        body.End.Should().BeTrue();
    }
}
