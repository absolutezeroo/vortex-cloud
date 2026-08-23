using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Collectibles;

/// <summary>
///     The collectibles shop tab, header 3272, re-derived from <c>ShopTab::onNftStoreOffers</c> and
///     the parser <c>_SafeCls_4058</c> with its offer struct <c>_SafeCls_3898</c>.
///
///     Nothing existed for this at all — the header constant was declared and then no message, no
///     parser, no map entry and no composer. The request (1809) was dropped, which is worse than an
///     empty answer here: the tab raises a waiting flag when it asks and clears it only on this
///     reply, so it spun for as long as it stayed open.
/// </summary>
public sealed class NftStoreOffersWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Body(NftStoreOffersMessageComposer composer)
    {
        byte[] bytes = Revision
            .Serializers[typeof(NftStoreOffersMessageComposer)]
            .Serialize(composer)
            .ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void NoOffers_IsStillAnAnswer()
    {
        // The shape this hotel actually sends: no chain, so nothing is for sale. One int, and the
        // tab renders "nothing here" and marks itself ready.
        ClientPacket packet = Body(
            new NftStoreOffersMessageComposer
            {
                Offers = ImmutableArray<NftStoreOfferSnapshot>.Empty,
            }
        );

        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void AnOffer_WritesItsSixScalarsThenTheProductStruct()
    {
        ClientPacket packet = Body(
            new NftStoreOffersMessageComposer
            {
                Offers =
                [
                    new NftStoreOfferSnapshot
                    {
                        ProductCode = "nft_chair",
                        EmeraldPrice = 250,
                        IsFeatured = true,
                        IsLimited = false,
                        MintLimit = 100,
                        MintedCount = 7,
                        ProductInfo = new CollectibleProductItemSnapshot
                        {
                            ProductTypeId = 1,
                            ItemTypeId = "s",
                            Score = 30,
                            // Deliberately non-zero: the base struct must never write it.
                            Amount = 1,
                            PetFigureString = string.Empty,
                            FigureSetIds = ImmutableArray<int>.Empty,
                            ProductCode = "nft_chair",
                            Rarity = "rare",
                        },
                    },
                ],
            }
        );

        packet.PopInt().Should().Be(1);

        packet.PopString().Should().Be("nft_chair");
        packet.PopInt().Should().Be(250);
        packet.PopBoolean().Should().BeTrue();
        packet.PopBoolean().Should().BeFalse();
        packet.PopInt().Should().Be(100);
        packet.PopInt().Should().Be(7);

        // The nested product struct in its BASE shape: the client reads _SafeCls_2582 directly,
        // whose readAdditionalParams hook is a no-op — the pet figure follows the score with no
        // amount in between. Only the collections list's subclass reads an amount there.
        packet.PopShort().Should().Be(1);
        packet.PopString().Should().Be("s");
        packet.PopInt().Should().Be(30);
        packet.PopString().Should().BeEmpty();
        packet.PopInt().Should().Be(0);
        packet.PopString().Should().Be("nft_chair");
        packet.PopString().Should().Be("rare");

        packet.Remaining.Should().Be(0);
    }
}
