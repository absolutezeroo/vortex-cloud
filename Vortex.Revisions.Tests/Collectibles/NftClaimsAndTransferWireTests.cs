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
///     The claims and transfer half of the collectibles tab, none of which existed: two requests
///     with no parser, one with no header at all, and four answers with no serializer.
/// </summary>
public sealed class NftClaimsAndTransferWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Body(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    /// <summary>
    ///     The one that would have lied. The client reads success as <c>resultCode == 0</c>, and
    ///     this composer used to write nothing — an empty body reads as zero, so every refused
    ///     transfer announced itself as a success.
    /// </summary>
    [Fact]
    public void TransferResult_RefusalIsNonZero()
    {
        ClientPacket packet = Body(
            typeof(NftTransferAssetsResultMessageComposer),
            new NftTransferAssetsResultMessageComposer
            {
                ResultCode = NftTransferAssetsResultMessageComposer.NotAvailable,
            }
        );

        packet.PopShort().Should().NotBe(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void TransferResult_ZeroIsTheSuccessCode()
    {
        ClientPacket packet = Body(
            typeof(NftTransferAssetsResultMessageComposer),
            new NftTransferAssetsResultMessageComposer { ResultCode = 0 }
        );

        packet.PopShort().Should().Be(0);
    }

    [Fact]
    public void Claims_EmptyListIsStillAnAnswer()
    {
        ClientPacket packet = Body(
            typeof(NftClaimsMessageComposer),
            new NftClaimsMessageComposer { Claims = ImmutableArray<NftClaimSnapshot>.Empty }
        );

        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void Claims_WritesElevenScalarsThenTheItemThenItsTwoExtras()
    {
        ClientPacket packet = Body(
            typeof(NftClaimsMessageComposer),
            new NftClaimsMessageComposer
            {
                Claims =
                [
                    new NftClaimSnapshot
                    {
                        ClaimId = "c1",
                        Status = 2,
                        ClaimedAmount = 1,
                        ClaimLimit = 5,
                        ValidFrom = 10L,
                        ValidTo = 20L,
                        CreatedAt = 30L,
                        UpdatedAt = 40L,
                        Collection = "coll",
                        ProductCode = "prod",
                        Wallet = "0xabc",
                        ClaimItem = new NftClaimItemSnapshot
                        {
                            Product = new CollectibleProductItemSnapshot
                            {
                                ProductTypeId = 1,
                                ItemTypeId = "s",
                                Score = 7,
                                Amount = 2,
                                PetFigureString = string.Empty,
                                FigureSetIds = ImmutableArray<int>.Empty,
                                ProductCode = "prod",
                                Rarity = "epic",
                            },
                            SetId = "set1",
                            DefaultCollectionName = "Default",
                        },
                    },
                ],
            }
        );

        packet.PopInt().Should().Be(1);

        packet.PopString().Should().Be("c1");
        packet.PopInt().Should().Be(2);
        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(5);
        packet.PopLong().Should().Be(10L);
        packet.PopLong().Should().Be(20L);
        packet.PopLong().Should().Be(30L);
        packet.PopLong().Should().Be(40L);
        packet.PopString().Should().Be("coll");
        packet.PopString().Should().Be("prod");
        packet.PopString().Should().Be("0xabc");

        // The claim item is the product struct first -- the client's class extends it -- with the
        // amount partway down, then the two strings that class adds.
        packet.PopShort().Should().Be(1);
        packet.PopString().Should().Be("s");
        packet.PopInt().Should().Be(7);
        packet.PopInt().Should().Be(2);
        packet.PopString().Should().BeEmpty();
        packet.PopInt().Should().Be(0);
        packet.PopString().Should().Be("prod");
        packet.PopString().Should().Be("epic");

        packet.PopString().Should().Be("set1");
        packet.PopString().Should().Be("Default");

        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void ClaimResult_IsOneShort()
    {
        ClientPacket packet = Body(
            typeof(NftClaimResultMessageComposer),
            new NftClaimResultMessageComposer { Status = NftClaimStatus.Failed }
        );

        packet.PopShort().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void LootBoxState_WritesStateOpenerThenTheReward()
    {
        ClientPacket packet = Body(
            typeof(RedeemNftLootBoxStateMessageComposer),
            new RedeemNftLootBoxStateMessageComposer
            {
                State = 1,
                OpenerAvatarId = 42,
                Reward = new CollectibleProductItemSnapshot
                {
                    ProductTypeId = 3,
                    ItemTypeId = "i",
                    Score = 0,
                    Amount = 1,
                    PetFigureString = string.Empty,
                    FigureSetIds = ImmutableArray<int>.Empty,
                    ProductCode = "box_prize",
                    Rarity = "common",
                },
            }
        );

        packet.PopShort().Should().Be(1);
        packet.PopInt().Should().Be(42);
        packet.PopShort().Should().Be(3);
        packet.PopString().Should().Be("i");
        packet.Remaining.Should().BeGreaterThan(0);
    }
}
