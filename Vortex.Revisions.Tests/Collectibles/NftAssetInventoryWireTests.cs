using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Collectibles;

/// <summary>
///     The inventory's Collectibles tab, header 2247, derived from <c>_SafeCls_3182</c> and its
///     asset struct <c>_SafeCls_3102</c>.
///
///     This answer did not exist — only the header constant did — and its absence was not a quiet
///     one: the tab's loading state is literally "the list was never initialised", and this is the
///     only message that initialises it, so the spinner stayed up over a hidden grid for as long as
///     the inventory was open.
/// </summary>
public sealed class NftAssetInventoryWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Body(TradeNftAssetInventoryMessageComposer composer)
    {
        byte[] bytes = Revision
            .Serializers[typeof(TradeNftAssetInventoryMessageComposer)]
            .Serialize(composer)
            .ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void NoAssets_IsStillAnAnswer()
    {
        // The shape this hotel actually sends. One int, and the tab leaves its loading state for its
        // empty state instead of spinning.
        ClientPacket packet = Body(
            new TradeNftAssetInventoryMessageComposer
            {
                Assets = ImmutableArray<CollectibleAssetSnapshot>.Empty,
            }
        );

        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void AnAsset_WritesItsIdBeforeTheProductStruct()
    {
        ClientPacket packet = Body(
            new TradeNftAssetInventoryMessageComposer
            {
                Assets =
                [
                    new CollectibleAssetSnapshot
                    {
                        AssetId = 9_000_000_001L,
                        Product = new CollectibleProductItemSnapshot
                        {
                            ProductTypeId = 1,
                            ItemTypeId = "s",
                            Score = 12,
                            // Deliberately non-zero: an asset is one item, and the client reads it
                            // through the base struct, which has no amount field at all.
                            Amount = 4,
                            PetFigureString = string.Empty,
                            FigureSetIds = ImmutableArray<int>.Empty,
                            ProductCode = "nft_lamp",
                            Rarity = "legendary",
                        },
                    },
                ],
            }
        );

        packet.PopInt().Should().Be(1);

        // The id first -- the client's class reads its own field before chaining to the base
        // constructor, so writing the struct first would shift everything.
        packet.PopLong().Should().Be(9_000_000_001L);

        packet.PopShort().Should().Be(1);
        packet.PopString().Should().Be("s");
        packet.PopInt().Should().Be(12);
        packet.PopString().Should().BeEmpty();
        packet.PopInt().Should().Be(0);
        packet.PopString().Should().Be("nft_lamp");
        packet.PopString().Should().Be("legendary");

        packet.Remaining.Should().Be(0);
    }
}
