using System;
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
/// The collections payload has two traps, both of which shift every field after them rather than
/// failing outright.
/// <para>
/// An item's amount is read <em>between</em> its score and its pet figure — the client gets it
/// through a <c>readAdditionalParams</c> hook partway down the struct, not at the end where the
/// getter sits. And a collection's two claims are written after its status, under the same booleans
/// that announced the items they belong to, rather than beside those items.
/// </para>
/// </summary>
public sealed class NftCollectionsWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void AnItem_PutsItsAmountBetweenTheScoreAndThePetFigure()
    {
        NftCollectionsMessageComposer composer = new()
        {
            Collections =
            [
                new NftCollectionSnapshot
                {
                    CollectionId = "summer",
                    CollectionName = "Summer",
                    Items =
                    [
                        new CollectibleProductItemSnapshot
                        {
                            ProductTypeId = 1,
                            ItemTypeId = "throne",
                            Score = 40,
                            Amount = 3,
                            ProductCode = "throne",
                            Rarity = "rare",
                        },
                    ],
                },
            ],
        };

        ClientPacket body = SerializeAndReadBody(typeof(NftCollectionsMessageComposer), composer);

        body.PopInt().Should().Be(1, "one collection");
        body.PopInt().Should().Be(1, "one item in it");

        body.PopShort().Should().Be(1, "product type is a short, not an int");
        body.PopString().Should().Be("throne");
        body.PopInt().Should().Be(40, "score");
        body.PopInt().Should().Be(3, "the amount lands here, before the pet figure");
        body.PopString().Should().BeEmpty("pet figure");
        body.PopInt().Should().Be(0, "no figure set ids");
        body.PopString().Should().Be("throne", "product code");
        body.PopString().Should().Be("rare");
    }

    [Fact]
    public void ACollectionWithAReward_WritesItsClaimAfterTheStatusRatherThanBesideTheItem()
    {
        NftCollectionsMessageComposer composer = new()
        {
            Collections =
            [
                new NftCollectionSnapshot
                {
                    CollectionId = "summer",
                    CollectionName = "Summer",
                    Items = [],
                    CollectionScore = 10,
                    CollectionTotalScore = 60,
                    CollectionBoostScore = 5,
                    RewardItem = new CollectibleProductItemSnapshot
                    {
                        ProductTypeId = 0,
                        ItemTypeId = "reward_sofa",
                        Score = 0,
                        ProductCode = "reward_sofa",
                    },
                    ReleasedTimeMs = 1_700_000_000_000,
                    SnapshotTimeMs = 1_700_000_600_000,
                    Status = 2,
                    RewardItemClaim = new CollectibleItemClaimSnapshot
                    {
                        ClaimId = "summer:reward",
                        ClaimedAmount = 0,
                        ClaimLimit = 1,
                        Status = CollectibleClaimStatus.NotClaimable,
                    },
                },
            ],
        };

        ClientPacket body = SerializeAndReadBody(typeof(NftCollectionsMessageComposer), composer);

        body.PopInt().Should().Be(1);
        body.PopInt().Should().Be(0, "no items");
        body.PopString().Should().Be("summer");
        body.PopString().Should().Be("Summer");
        body.PopInt().Should().Be(10, "collection score");
        body.PopInt().Should().Be(60, "total score");
        body.PopInt().Should().Be(5, "boost score");

        body.PopBoolean().Should().BeFalse("no bonus item");
        body.PopBoolean().Should().BeTrue("there is a reward item");

        // The reward item itself, inline.
        body.PopShort().Should().Be(0);
        body.PopString().Should().Be("reward_sofa");
        body.PopInt().Should().Be(0, "score");
        body.PopInt().Should().Be(0, "amount");
        body.PopString().Should().BeEmpty("pet figure");
        body.PopInt().Should().Be(0, "figure set ids");
        body.PopString().Should().Be("reward_sofa");
        body.PopString().Should().BeEmpty("rarity");

        body.PopLong().Should().Be(1_700_000_000_000);
        body.PopLong().Should().Be(1_700_000_600_000);
        body.PopShort().Should().Be(2, "status is a short");

        // Only now does the claim arrive, and only the reward's — the bonus announced false.
        body.PopString().Should().Be("summer:reward");
        body.PopInt().Should().Be(0, "claimed amount");
        body.PopInt().Should().Be(1, "claim limit");
        body.PopShort().Should().Be((short)CollectibleClaimStatus.NotClaimable);
    }

    [Fact]
    public void ACollectionWithAnItemButNoClaim_StillWritesOne()
    {
        // The client reads a claim struct for every announced item. Skipping it because the server
        // happens to have none would leave everything after it read from the wrong offset.
        NftCollectionsMessageComposer composer = new()
        {
            Collections =
            [
                new NftCollectionSnapshot
                {
                    CollectionId = "summer",
                    CollectionName = "Summer",
                    Items = [],
                    BonusItem = new CollectibleProductItemSnapshot
                    {
                        ProductTypeId = 0,
                        ItemTypeId = "bonus_lamp",
                        Score = 0,
                        ProductCode = "bonus_lamp",
                    },
                    BonusItemClaim = null,
                },
            ],
        };

        ClientPacket body = SerializeAndReadBody(typeof(NftCollectionsMessageComposer), composer);

        body.PopInt();
        body.PopInt();
        body.PopString();
        body.PopString();
        body.PopInt();
        body.PopInt();
        body.PopInt();
        body.PopBoolean().Should().BeTrue("a bonus item was announced");

        // Skip the inline bonus item.
        body.PopShort();
        body.PopString();
        body.PopInt();
        body.PopInt();
        body.PopString();
        body.PopInt();
        body.PopString();
        body.PopString();

        body.PopBoolean().Should().BeFalse("no reward item");
        body.PopLong();
        body.PopLong();
        body.PopShort();

        body.PopString()
            .Should()
            .Be("summer:bonus", "a stand-in claim is written rather than none");
    }

    [Fact]
    public void TheCollectorScore_IsScoreThenHighestThenLevel()
    {
        NftCollectionsScoreMessageComposer composer = new()
        {
            Score = 120,
            HighestScore = 340,
            Level = 2,
        };

        ClientPacket body = SerializeAndReadBody(
            typeof(NftCollectionsScoreMessageComposer),
            composer
        );

        body.PopInt().Should().Be(120);
        body.PopInt().Should().Be(340);
        body.PopInt().Should().Be(2);
    }

    [Fact]
    public void TheWalletList_WritesTheStardustAddressOnItsOwnFirst()
    {
        CollectibleWalletAddressesMessageComposer composer = new();

        ClientPacket body = SerializeAndReadBody(
            typeof(CollectibleWalletAddressesMessageComposer),
            composer
        );

        body.PopString().Should().BeEmpty("an empty stardust address is how none-linked is said");
        body.PopInt().Should().Be(0, "and no other wallets");
    }

    [Fact]
    public void MintingEnabled_IsASingleBoolean()
    {
        CollectibleMintingEnabledMessageComposer composer = new() { Enabled = false };

        ClientPacket body = SerializeAndReadBody(
            typeof(CollectibleMintingEnabledMessageComposer),
            composer
        );

        body.PopBoolean().Should().BeFalse();
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
