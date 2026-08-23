using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Collectibles;

/// <summary>
/// The wallet payload writes the Stardust address on its own, before the count-prefixed list of the
/// rest. The client pushes that first address into the same list it builds from the others, but
/// only when it is not empty — an empty string is how the wire says "no wallet linked".
/// <para>
/// That distinction is load-bearing rather than cosmetic: with no wallets at all, neither the
/// collections tab nor the reward-claims tab ever sends the per-wallet request that is the only
/// thing that clears their loading state, so both spin forever.
/// </para>
/// </summary>
public sealed class CollectibleWalletAddressesWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void TheStardustAddress_IsWrittenBeforeTheCountedList()
    {
        CollectibleWalletAddressesMessageComposer composer = new()
        {
            StardustWalletAddress = "0x0000000000000000000000000000000000000001",
            WalletAddresses = ["0xabc", "0xdef"],
        };

        ClientPacket body = SerializeAndReadBody(
            typeof(CollectibleWalletAddressesMessageComposer),
            composer
        );

        body.PopString()
            .Should()
            .Be(
                "0x0000000000000000000000000000000000000001",
                "the stardust address comes first, outside the list"
            );

        body.PopInt().Should().Be(2, "the remaining wallets are count-prefixed");
        body.PopString().Should().Be("0xabc");
        body.PopString().Should().Be("0xdef");
    }

    [Fact]
    public void NoLinkedWallets_StillWritesAnEmptyStardustStringAndAZeroCount()
    {
        CollectibleWalletAddressesMessageComposer composer = new();

        ClientPacket body = SerializeAndReadBody(
            typeof(CollectibleWalletAddressesMessageComposer),
            composer
        );

        body.PopString().Should().BeEmpty("an empty string is how the wire says 'none linked'");
        body.PopInt().Should().Be(0);
    }

    private static ClientPacket SerializeAndReadBody(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();

        // AbstractSerializer prepends int length (4) + short header (2).
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }
}
