using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Protocol.Messages.Incoming.Game.Lobby;
using Vortex.Protocol.Messages.Outgoing.Game.Lobby;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Quests;

/// <summary>
///     The resolution statue, re-derived from WIN63-202607011411: <c>_SafeCls_3983</c> (picker),
///     <c>_SafeCls_3262</c> (progress), <c>_SafeCls_3054</c> (completed), the per-row struct
///     <c>_SafeCls_2410</c>, and the two composers the client sends — <c>_SafeCls_2562</c> at 1760
///     and <c>_SafeCls_3330</c> at 916.
///
///     All three serializers shipped with an empty body and the request parser read nothing at all,
///     so every field here is new. Two of them are the sort that only show up in play: the countdown
///     is written after the list rather than before it, and the completed screen carries the stuff
///     code first even though the client's own handler forwards the badge first.
/// </summary>
public sealed class AchievementResolutionWireTests
{
    private const int GetResolutionAchievements = 1760;
    private const int ResetResolutionAchievement = 916;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Body(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void Picker_WritesTheStuffIdThenEachRowThenTheCountdown()
    {
        ClientPacket packet = Body(
            typeof(AchievementResolutionsMessageComposer),
            new AchievementResolutionsMessageComposer
            {
                StuffId = 4711,
                SecondsLeft = 604_800,
                Achievements =
                [
                    new AchievementResolutionSnapshot
                    {
                        AchievementId = 6,
                        Level = 2,
                        BadgeId = "ACH_FriendCount3",
                        RequiredLevel = 3,
                        State = AchievementResolutionState.Selectable,
                    },
                    new AchievementResolutionSnapshot
                    {
                        AchievementId = 2,
                        Level = 5,
                        BadgeId = "ACH_Login5",
                        RequiredLevel = 5,
                        State = AchievementResolutionState.AllLevelsCompleted,
                    },
                ],
            }
        );

        packet.PopInt().Should().Be(4711);
        packet.PopInt().Should().Be(2);

        packet.PopInt().Should().Be(6);
        packet.PopInt().Should().Be(2);
        packet.PopString().Should().Be("ACH_FriendCount3");
        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(0);

        packet.PopInt().Should().Be(2);
        packet.PopInt().Should().Be(5);
        packet.PopString().Should().Be("ACH_Login5");
        packet.PopInt().Should().Be(5);
        packet.PopInt().Should().Be(1);

        // The countdown is last. Writing it before the list would be read as the first row's
        // achievement id and desynchronise everything after it.
        packet.PopInt().Should().Be(604_800);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void Picker_WithNoOffersStillWritesTheStuffIdAndTheCountdown()
    {
        // The client returns early on an empty vector and never shows the window, but it reads all
        // three fields first.
        ClientPacket packet = Body(
            typeof(AchievementResolutionsMessageComposer),
            new AchievementResolutionsMessageComposer
            {
                StuffId = 1,
                SecondsLeft = 0,
                Achievements = ImmutableArray<AchievementResolutionSnapshot>.Empty,
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void Progress_WritesSixFieldsWithTheBadgeInTheMiddle()
    {
        ClientPacket packet = Body(
            typeof(AchievementResolutionProgressMessageComposer),
            new AchievementResolutionProgressMessageComposer
            {
                StuffId = 4711,
                AchievementId = 6,
                RequiredLevelBadgeCode = "ACH_FriendCount3",
                UserProgress = 2,
                TotalProgress = 3,
                SecondsLeft = 86_400,
            }
        );

        packet.PopInt().Should().Be(4711);
        packet.PopInt().Should().Be(6);
        packet.PopString().Should().Be("ACH_FriendCount3");
        packet.PopInt().Should().Be(2);
        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(86_400);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void Completed_WritesTheStuffCodeBeforeTheBadge()
    {
        // The client's onAchievementResolutionCompleted calls show(badgeCode, stuffCode) — reading
        // that call site instead of the parser writes these two the wrong way round, and the
        // congratulations screen shows a furni name where the badge belongs.
        ClientPacket packet = Body(
            typeof(AchievementResolutionCompletedMessageComposer),
            new AchievementResolutionCompletedMessageComposer
            {
                StuffCode = "ny2013_res",
                BadgeCode = "ACH_FriendCount3",
            }
        );

        packet.PopString().Should().Be("ny2013_res");
        packet.PopString().Should().Be("ACH_FriendCount3");
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void Request_ReadsBothIntsNotAnEmptyBody()
    {
        GetResolutionAchievementsMessage message = (GetResolutionAchievementsMessage)
            Revision.Parsers[GetResolutionAchievements].Parse(Packet(4711, 6));

        message.StuffId.Should().Be(4711);
        message.AchievementId.Should().Be(6);
    }

    [Fact]
    public void Request_CarriesAZeroAchievementWhenTheStatueIsOnlyBeingOpened()
    {
        GetResolutionAchievementsMessage message = (GetResolutionAchievementsMessage)
            Revision.Parsers[GetResolutionAchievements].Parse(Packet(4711, 0));

        message.StuffId.Should().Be(4711);
        message.AchievementId.Should().Be(0);
    }

    [Fact]
    public void Reset_ReadsTheStuffId()
    {
        ResetResolutionAchievementMessage message = (ResetResolutionAchievementMessage)
            Revision.Parsers[ResetResolutionAchievement].Parse(Packet(4711));

        message.StuffId.Should().Be(4711);
    }

    private static ClientPacket Packet(params int[] values)
    {
        byte[] body = new byte[values.Length * 4];

        for (int i = 0; i < values.Length; i++)
        {
            body[(i * 4) + 0] = (byte)(values[i] >> 24);
            body[(i * 4) + 1] = (byte)(values[i] >> 16);
            body[(i * 4) + 2] = (byte)(values[i] >> 8);
            body[(i * 4) + 3] = (byte)values[i];
        }

        return new ClientPacket(0, body);
    }
}
