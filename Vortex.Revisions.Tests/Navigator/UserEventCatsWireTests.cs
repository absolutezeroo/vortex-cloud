using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Protocol.Messages.Outgoing.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Navigator;

/// <summary>
/// Every entry of UserEventCatsComposer (1370) is int/string/<b>bool</b>. The serializer wrote only
/// the first two, so the client's per-entry read drifted one boolean into the next entry and the
/// packet died with "End of buffer" inside the second category's name.
/// </summary>
public sealed class UserEventCatsWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void EachCategory_WritesIdNameAndVisible()
    {
        ClientPacket body = Serialize(
            new NavigatorEventCategorySnapshot
            {
                Id = 1,
                Name = "Trading",
                Visible = true,
            },
            new NavigatorEventCategorySnapshot
            {
                Id = 2,
                Name = "Games",
                Visible = false,
            }
        );

        body.PopInt().Should().Be(2);

        body.PopInt().Should().Be(1);
        body.PopString().Should().Be("Trading");
        body.PopBoolean().Should().BeTrue();

        body.PopInt().Should().Be(2);
        body.PopString().Should().Be("Games");
        body.PopBoolean().Should().BeFalse();

        body.End.Should().BeTrue();
    }

    [Fact]
    public void NoCategories_WritesOnlyTheCount()
    {
        ClientPacket body = Serialize();

        body.PopInt().Should().Be(0);
        body.End.Should().BeTrue();
    }

    private static ClientPacket Serialize(params NavigatorEventCategorySnapshot[] categories)
    {
        byte[] bytes = Revision
            .Serializers[typeof(UserEventCatsMessageComposer)]
            .Serialize(new UserEventCatsMessageComposer { EventCategories = [.. categories] })
            .ToArray();

        byte[] payload = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, payload, 0, payload.Length);

        return new ClientPacket(0, payload);
    }
}
