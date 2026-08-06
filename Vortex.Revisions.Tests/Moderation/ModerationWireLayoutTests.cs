using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Moderation;

/// <summary>
///     The mod tool's wire contract, decoded from the WIN63-202607011411 client. Two classes of bug
///     are locked down here. First, every moderation composer used to be missing from the revision's
///     serializer map, so PackageEncoder dropped it silently — indexing <c>Revision.Serializers</c>
///     fails loudly if that regresses. Second, three incoming layouts had their fields in the wrong
///     order or the wrong width, which desynchronises the read for everything after them.
/// </summary>
public sealed class ModerationWireLayoutTests
{
    private const int GetRoomChatlogMessageEvent = 1346;
    private const int GetRoomVisitsMessageEvent = 903;
    private const int ModerateRoomMessageEvent = 2939;
    private const int ModToolPreferencesEvent = 1415;
    private const int ModTradingLockMessageEvent = 3495;
    private const int ModMessageMessageEvent = 2579;

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
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void EveryModerationComposer_HasARegisteredSerializer()
    {
        Type[] composers =
        [
            typeof(CfhChatlogEventMessageComposer),
            typeof(IssueDeletedMessageComposer),
            typeof(IssueInfoMessageComposer),
            typeof(IssuePickFailedMessageComposer),
            typeof(ModeratorActionResultMessageComposer),
            typeof(ModeratorCautionEventMessageComposer),
            typeof(ModeratorInitMessageComposer),
            typeof(ModeratorMessageComposer),
            typeof(ModeratorRoomInfoEventMessageComposer),
            typeof(ModeratorToolPreferencesEventMessageComposer),
            typeof(ModeratorUserInfoEventMessageComposer),
            typeof(RoomChatlogEventMessageComposer),
            typeof(RoomVisitsEventMessageComposer),
            typeof(UserBannedMessageComposer),
            typeof(UserChatlogEventMessageComposer),
        ];

        Revision.Serializers.Keys.Should().Contain(composers);
    }

    [Fact]
    public void GetRoomChatlogParser_ReadsTheRoomTypeBeforeTheRoomId()
    {
        // _SafeCls_2601(roomType, roomId): reading the id first pinned every lookup to room 0 or 1.
        ClientPacket packet = BuildClientPacket(
            GetRoomChatlogMessageEvent,
            sp => sp.WriteInteger(0).WriteInteger(4271)
        );

        GetRoomChatlogMessage message = Revision
            .Parsers[GetRoomChatlogMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<GetRoomChatlogMessage>()
            .Subject;

        message.RoomType.Should().Be(0);
        message.RoomId.Should().Be(4271);
    }

    [Fact]
    public void ModTradingLockParser_ReadsTheDurationBeforeTheTopic()
    {
        // _SafeCls_3651(userId, message, actionLengthHours * 60, topicId, [issueId]).
        ClientPacket packet = BuildClientPacket(
            ModTradingLockMessageEvent,
            sp =>
                sp.WriteInteger(77)
                    .WriteString("no trading for you")
                    .WriteInteger(1440)
                    .WriteInteger(9)
                    .WriteInteger(31)
        );

        ModTradingLockMessage message = Revision
            .Parsers[ModTradingLockMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<ModTradingLockMessage>()
            .Subject;

        message.UserId.Should().Be(77);
        message.DurationMinutes.Should().Be(1440);
        message.TopicId.Should().Be(9);
        message.IssueId.Should().Be(31);
    }

    [Fact]
    public void ModTradingLockParser_DefaultsTheIssueIdWhenTheClientOmitsIt()
    {
        ClientPacket packet = BuildClientPacket(
            ModTradingLockMessageEvent,
            sp => sp.WriteInteger(77).WriteString("x").WriteInteger(60).WriteInteger(9)
        );

        ModTradingLockMessage message = (ModTradingLockMessage)
            Revision.Parsers[ModTradingLockMessageEvent].Parse(packet);

        message.IssueId.Should().Be(-1);
    }

    [Fact]
    public void ModerateRoomParser_ReadsTheFlagsAsIntsNotBooleans()
    {
        // _SafeCls_2501 pushes `flag ? 1 : 0` — AS3 ints, so four bytes each. Reading them as
        // single-byte booleans would leave the last two flags reading garbage.
        ClientPacket packet = BuildClientPacket(
            ModerateRoomMessageEvent,
            sp => sp.WriteInteger(512).WriteInteger(1).WriteInteger(0).WriteInteger(1)
        );

        ModerateRoomMessage message = Revision
            .Parsers[ModerateRoomMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<ModerateRoomMessage>()
            .Subject;

        message.RoomId.Should().Be(512);
        message.LockDoor.Should().BeTrue();
        message.ChangeName.Should().BeFalse();
        message.KickUsers.Should().BeTrue();
    }

    [Fact]
    public void GetRoomVisitsParser_ReadsAUserIdDespiteTheMessageName()
    {
        ClientPacket packet = BuildClientPacket(
            GetRoomVisitsMessageEvent,
            sp => sp.WriteInteger(4242)
        );

        GetRoomVisitsMessage message = Revision
            .Parsers[GetRoomVisitsMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<GetRoomVisitsMessage>()
            .Subject;

        message.UserId.Should().Be(4242);
    }

    [Fact]
    public void ModToolPreferencesParser_ReadsTheWindowRectangle()
    {
        ClientPacket packet = BuildClientPacket(
            ModToolPreferencesEvent,
            sp => sp.WriteInteger(10).WriteInteger(20).WriteInteger(640).WriteInteger(480)
        );

        ModToolPreferencesMessage message = Revision
            .Parsers[ModToolPreferencesEvent]
            .Parse(packet)
            .Should()
            .BeOfType<ModToolPreferencesMessage>()
            .Subject;

        message.WindowX.Should().Be(10);
        message.WindowY.Should().Be(20);
        message.WindowWidth.Should().Be(640);
        message.WindowHeight.Should().Be(480);
    }

    [Fact]
    public void ModMessageParser_SkipsTheTwoBlankStringsTheClientPads()
    {
        ClientPacket packet = BuildClientPacket(
            ModMessageMessageEvent,
            sp =>
                sp.WriteInteger(88)
                    .WriteString("behave")
                    .WriteString(string.Empty)
                    .WriteString(string.Empty)
                    .WriteInteger(4)
                    .WriteInteger(17)
        );

        ModMessageMessage message = Revision
            .Parsers[ModMessageMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<ModMessageMessage>()
            .Subject;

        message.UserId.Should().Be(88);
        message.Message.Should().Be("behave");
        message.TopicId.Should().Be(4);
        message.IssueId.Should().Be(17);
    }

    [Fact]
    public void ModeratorRoomInfoSerializer_StopsAfterTheFlagWhenTheRoomIsGone()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(ModeratorRoomInfoEventMessageComposer),
            new ModeratorRoomInfoEventMessageComposer
            {
                RoomId = 5,
                UserCount = 0,
                OwnerInRoom = false,
                OwnerId = 0,
                OwnerName = string.Empty,
                RoomExists = false,
                RoomName = "ignored",
            }
        );

        body.PopInt().Should().Be(5);
        body.PopInt().Should().Be(0);
        body.PopBoolean().Should().BeFalse();
        body.PopInt().Should().Be(0);
        body.PopString().Should().BeEmpty();
        body.PopBoolean().Should().BeFalse();
        body.End.Should().BeTrue("the client stops reading the room block once the flag is false");
    }

    [Fact]
    public void ModeratorRoomInfoSerializer_WritesTheRoomBlockWhenTheRoomExists()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(ModeratorRoomInfoEventMessageComposer),
            new ModeratorRoomInfoEventMessageComposer
            {
                RoomId = 512,
                UserCount = 7,
                OwnerInRoom = true,
                OwnerId = 99,
                OwnerName = "Frank",
                RoomExists = true,
                RoomName = "Lobby",
                RoomDescription = "the lobby",
                Tags = ["chill", "chat"],
            }
        );

        body.PopInt().Should().Be(512);
        body.PopInt().Should().Be(7);
        body.PopBoolean().Should().BeTrue();
        body.PopInt().Should().Be(99);
        body.PopString().Should().Be("Frank");
        body.PopBoolean().Should().BeTrue();
        body.PopString().Should().Be("Lobby");
        body.PopString().Should().Be("the lobby");
        body.PopInt().Should().Be(2);
        body.PopString().Should().Be("chill");
        body.PopString().Should().Be("chat");
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void ModeratorUserInfoSerializer_OmitsTheSanctionTailWhenThereIsNoHistory()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(ModeratorUserInfoEventMessageComposer),
            new ModeratorUserInfoEventMessageComposer
            {
                UserId = 1,
                UserName = "Bob",
                Figure = "hd-180-1",
                RegistrationAgeInMinutes = 60,
                MinutesSinceLastLogin = 5,
                Online = true,
            }
        );

        body.PopInt().Should().Be(1);
        body.PopString().Should().Be("Bob");
        body.PopString().Should().Be("hd-180-1");
        body.PopInt().Should().Be(60);
        body.PopInt().Should().Be(5);
        body.PopBoolean().Should().BeTrue();

        // cfh, abusiveCfh, caution, ban, tradingLock
        body.PopInt().Should().Be(0);
        body.PopInt().Should().Be(0);
        body.PopInt().Should().Be(0);
        body.PopInt().Should().Be(0);
        body.PopInt().Should().Be(0);

        body.PopString().Should().BeEmpty(); // tradingExpiryDate
        body.PopString().Should().BeEmpty(); // lastPurchaseDate
        body.PopInt().Should().Be(0); // identityId
        body.PopInt().Should().Be(0); // identityRelatedBanCount
        body.PopString().Should().BeEmpty(); // primaryEmailAddress
        body.PopString().Should().BeEmpty(); // userClassification

        body.End.Should()
            .BeTrue("the client only reads the sanction tail while bytes remain available");
    }

    [Fact]
    public void ModeratorUserInfoSerializer_AppendsTheSanctionTailAsAPair()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(ModeratorUserInfoEventMessageComposer),
            new ModeratorUserInfoEventMessageComposer
            {
                UserId = 1,
                UserName = "Bob",
                Figure = "hd-180-1",
                RegistrationAgeInMinutes = 60,
                MinutesSinceLastLogin = 5,
                Online = false,
                HasSanctionHistory = true,
                LastSanctionTime = "2026-08-01",
                SanctionAgeHours = 72,
            }
        );

        // The head is covered by the sibling test; skip past it to reach the optional tail.
        body.PopInt();
        body.PopString();
        body.PopString();
        body.PopInt();
        body.PopInt();
        body.PopBoolean();
        for (int i = 0; i < 5; i++)
        {
            body.PopInt();
        }

        body.PopString();
        body.PopString();
        body.PopInt();
        body.PopInt();
        body.PopString();
        body.PopString();

        body.PopString().Should().Be("2026-08-01");
        body.PopInt().Should().Be(72);
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void RoomVisitsSerializer_WritesTheUserThenOneRowPerVisit()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(RoomVisitsEventMessageComposer),
            new RoomVisitsEventMessageComposer
            {
                UserId = 4242,
                UserName = "Bob",
                Visits =
                [
                    new RoomVisitSnapshot
                    {
                        RoomId = 1,
                        RoomName = "Lobby",
                        EnterHour = 13,
                        EnterMinute = 45,
                    },
                ],
            }
        );

        body.PopInt().Should().Be(4242);
        body.PopString().Should().Be("Bob");
        body.PopInt().Should().Be(1);
        body.PopInt().Should().Be(1);
        body.PopString().Should().Be("Lobby");
        body.PopInt().Should().Be(13);
        body.PopInt().Should().Be(45);
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void IssueDeletedSerializer_WritesTheIdAsAStringBecauseTheClientParsesInt()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(IssueDeletedMessageComposer),
            new IssueDeletedMessageComposer { IssueId = 4711 }
        );

        body.PopString().Should().Be("4711");
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void IssuePickFailedSerializer_WritesTheConflictsThenTheRetryHint()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(IssuePickFailedMessageComposer),
            new IssuePickFailedMessageComposer
            {
                Conflicts =
                [
                    new IssuePickConflict
                    {
                        IssueId = 8,
                        PickerUserId = 3,
                        PickerUserName = "Mod",
                    },
                ],
                RetryEnabled = true,
                RetryCount = 2,
            }
        );

        body.PopInt().Should().Be(1);
        body.PopInt().Should().Be(8);
        body.PopInt().Should().Be(3);
        body.PopString().Should().Be("Mod");
        body.PopBoolean().Should().BeTrue();
        body.PopInt().Should().Be(2);
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void IssueInfoSerializer_WritesTheSameSixteenFieldBlockAsTheInitQueue()
    {
        CfhIssueQueueEntrySnapshot issue = new(
            IssueId: 42,
            State: CfhTicketState.Picked,
            CategoryId: 3,
            IssueAgeMs: 5000,
            Priority: 1,
            ReporterUserId: 10,
            ReporterUserName: "Alice",
            ReportedUserId: 11,
            ReportedUserName: "Bob",
            PickerUserId: 12,
            PickerUserName: "Mod",
            Message: "help"
        );

        ClientPacket body = SerializeAndReadBody(
            typeof(IssueInfoMessageComposer),
            new IssueInfoMessageComposer { Issue = issue }
        );

        body.PopInt().Should().Be(42); // issueId
        body.PopInt().Should().Be((int)CfhTicketState.Picked); // state
        body.PopInt().Should().Be(3); // categoryId
        body.PopInt().Should().Be(3); // reportedCategoryId
        body.PopInt().Should().Be(5000); // issueAgeMs
        body.PopInt().Should().Be(1); // priority
        body.PopInt().Should().Be(42); // groupingId
        body.PopInt().Should().Be(10);
        body.PopString().Should().Be("Alice");
        body.PopInt().Should().Be(11);
        body.PopString().Should().Be("Bob");
        body.PopInt().Should().Be(12);
        body.PopString().Should().Be("Mod");
        body.PopString().Should().Be("help");
        body.PopInt().Should().Be(42); // chatRecordId
        body.PopInt().Should().Be(0); // patternCount
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void ModeratorToolPreferencesSerializer_WritesTheWindowRectangle()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(ModeratorToolPreferencesEventMessageComposer),
            new ModeratorToolPreferencesEventMessageComposer
            {
                WindowX = 10,
                WindowY = 20,
                WindowWidth = 640,
                WindowHeight = 480,
            }
        );

        body.PopInt().Should().Be(10);
        body.PopInt().Should().Be(20);
        body.PopInt().Should().Be(640);
        body.PopInt().Should().Be(480);
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void ModeratorMessageSerializer_WritesTheMessageThenTheUrl()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(ModeratorMessageComposer),
            new ModeratorMessageComposer { Message = "behave", Url = "https://example.test" }
        );

        body.PopString().Should().Be("behave");
        body.PopString().Should().Be("https://example.test");
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }
}
