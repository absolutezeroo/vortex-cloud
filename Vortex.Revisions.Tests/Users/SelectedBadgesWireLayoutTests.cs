using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Users;

/// <summary>
///     Locks the "view a user's selected badges" byte contract against the Flash client
///     (GetSelectedBadges request carries the target userId; HabboUserBadges response = userId,
///     count, then per equipped badge slotId + code). Both were empty stubs.
/// </summary>
public sealed class SelectedBadgesWireLayoutTests
{
    private const int GetSelectedBadgesEvent = 3726;

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
    public void GetSelectedBadgesParser_ReadsUserId()
    {
        ClientPacket packet = BuildClientPacket(
            GetSelectedBadgesEvent,
            sp => sp.WriteInteger(4711)
        );

        GetSelectedBadgesMessage message = Revision
            .Parsers[GetSelectedBadgesEvent]
            .Parse(packet)
            .Should()
            .BeOfType<GetSelectedBadgesMessage>()
            .Subject;

        message.UserId.Should().Be(4711);
    }

    [Fact]
    public void HabboUserBadgesSerializer_WritesUserIdCountAndBadges()
    {
        HabboUserBadgesMessageComposer composer = new()
        {
            UserId = 4711,
            Badges = ImmutableArray.Create(
                new PlayerBadgeSnapshot { SlotId = 1, BadgeCode = "ADM" },
                new PlayerBadgeSnapshot { SlotId = 2, BadgeCode = "ACH_Login5" }
            ),
        };

        ClientPacket body = SerializeAndReadBody(typeof(HabboUserBadgesMessageComposer), composer);

        body.PopInt().Should().Be(4711); // userId
        body.PopInt().Should().Be(2); // count
        body.PopInt().Should().Be(1); // slot
        body.PopString().Should().Be("ADM");
        body.PopInt().Should().Be(2);
        body.PopString().Should().Be("ACH_Login5");
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }
}
