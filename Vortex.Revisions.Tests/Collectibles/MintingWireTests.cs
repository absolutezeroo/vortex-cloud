using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Collectibles;

/// <summary>
///     Converting furniture into a Relic: the minting half of the Collectors Guild.
///
///     Derived from <c>MintInventoryListTab</c> and the structs it reads — <c>_SafeCls_4500</c> for
///     a mintable type, <c>_SafeCls_2904</c> for a stamp bundle, <c>_SafeCls_4129</c> for the
///     result.
/// </summary>
public sealed class MintingWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    /// <summary>Repeated rather than referenced: the header table is internal to the revision.</summary>
    private const int GetCollectibleMintTokensMessageEvent = 1554;

    private const int MintItemMessageEvent = 2815;
    private const int PurchaseMintTokenMessageEvent = 67;

    [Fact]
    public void AMintableType_WritesTheKindLastAndAsAShort()
    {
        ClientPacket packet = Body(
            new CollectableMintableItemTypesMessageComposer
            {
                ItemTypes =
                [
                    new MintableItemTypeSnapshot
                    {
                        ItemTypeId = 4321,
                        StartTime = 1_700_000_000,
                        EndTime = 1_800_000_000,
                        RegionLocked = true,
                        Price = 7,
                        LimitedEdition = true,
                        ItemType = MintableItemKind.Wall,
                    },
                ],
            }
        );

        packet.PopInt().Should().Be(1);

        packet.PopInt().Should().Be(4321);
        packet.PopInt().Should().Be(1_700_000_000);
        packet.PopInt().Should().Be(1_800_000_000);
        packet.PopBoolean().Should().BeTrue();
        packet.PopInt().Should().Be(7);
        packet.PopBoolean().Should().BeTrue();

        // The only non-int in the row, and it comes last. The client reads it with readShort and
        // switches on it to pick which of its inventories to count the player's copies in.
        packet.PopShort().Should().Be(MintableItemKind.Wall);

        packet.Remaining.Should().Be(0);
    }

    /// <summary>
    ///     The trap worth a test of its own: the two messages of this tab number floor and wall the
    ///     opposite way round. A mintable type says 0 for floor, while the product struct sent
    ///     beside it says 0 for wall.
    ///
    ///     Swapping them is silent. The client looks the sprite id up in the other inventory, finds
    ///     nothing, and shows the row as one the player owns none of — the convert button simply
    ///     stays grey, with nothing on screen to say why.
    /// </summary>
    [Fact]
    public void TheKindShort_IsTheReverseOfTheProductTypeId()
    {
        MintableItemKind.ForFurniture(ProductType.Floor).Should().Be(0);
        MintableItemKind.ForFurniture(ProductType.Wall).Should().Be(1);

        CollectibleProductIdentity.ForFurniture(ProductType.Wall).Should().Be(0);
        CollectibleProductIdentity.ForFurniture(ProductType.Floor).Should().Be(1);
    }

    [Fact]
    public void AStampBundle_IsIdThenCodeThenPriceThenAmount()
    {
        ClientPacket packet = Body(
            new CollectibleMintTokenOffersMessageComposer
            {
                Offers =
                [
                    new MintTokenOfferSnapshot
                    {
                        OfferId = 12,
                        ProductCode = "stamps_10",
                        SilverPrice = 250,
                        AmountTokens = 10,
                    },
                ],
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(12);
        packet.PopString().Should().Be("stamps_10");
        packet.PopInt().Should().Be(250);
        packet.PopInt().Should().Be(10);

        packet.Remaining.Should().Be(0);
    }

    /// <summary>
    ///     Success is 1 here, where the store purchase, the claim and the transfer all succeed on 0.
    ///     The client compares the code to its own constant and treats everything else as a failure,
    ///     so the pairing has to be pinned rather than assumed from a sibling message: the same
    ///     obfuscated constant name means 0 in one parser and 1 in this one.
    /// </summary>
    [Fact]
    public void TheMintResult_SucceedsOnOneAndNotOnZero()
    {
        CollectibleMintableItemResultMessageComposer.Success.Should().Be(1);
        CollectibleMintableItemResultMessageComposer.Failed.Should().Be(0);

        Result(CollectibleMintableItemResultMessageComposer.Success).PopShort().Should().Be(1);
        Result(CollectibleMintableItemResultMessageComposer.Failed).PopShort().Should().NotBe(1);
    }

    /// <summary>
    ///     The conversion names the item by its <em>inventory</em> id. The parser used to read
    ///     nothing at all, which left the only field identifying what to convert on the floor.
    /// </summary>
    [Fact]
    public void TheMintRequest_IsTheItemIdThenTheWallet()
    {
        ServerPacket request = new(MintItemMessageEvent);
        request.WriteInteger(778_899).WriteString("0xabc");

        MintItemMessage parsed = (MintItemMessage)
            Revision
                .Parsers[MintItemMessageEvent]
                .Parse(new ClientPacket(MintItemMessageEvent, request.ToArray()));

        parsed.ItemId.Should().Be(778_899);
        parsed.Wallet.Should().Be("0xabc");
    }

    [Fact]
    public void TheStampPurchase_IsTheOfferIdThenTheWallet()
    {
        ServerPacket request = new(PurchaseMintTokenMessageEvent);
        request.WriteInteger(12).WriteString("0xabc");

        PurchaseMintTokenMessage parsed = (PurchaseMintTokenMessage)
            Revision
                .Parsers[PurchaseMintTokenMessageEvent]
                .Parse(new ClientPacket(PurchaseMintTokenMessageEvent, request.ToArray()));

        parsed.OfferId.Should().Be(12);
        parsed.Wallet.Should().Be("0xabc");
    }

    [Fact]
    public void TheBalanceRequest_IsJustTheWallet()
    {
        ServerPacket request = new(GetCollectibleMintTokensMessageEvent);
        request.WriteString("0xabc");

        GetCollectibleMintTokensMessage parsed = (GetCollectibleMintTokensMessage)
            Revision
                .Parsers[GetCollectibleMintTokensMessageEvent]
                .Parse(new ClientPacket(GetCollectibleMintTokensMessageEvent, request.ToArray()));

        parsed.Wallet.Should().Be("0xabc");
    }

    [Fact]
    public void TheBalance_IsOneInt()
    {
        ClientPacket packet = Body(new CollectibleMintTokenCountMessageComposer { Count = 42 });

        packet.PopInt().Should().Be(42);
        packet.Remaining.Should().Be(0);
    }

    private static ClientPacket Result(short status) =>
        Body(new CollectibleMintableItemResultMessageComposer { Status = status });

    private static ClientPacket Body<TComposer>(TComposer composer)
        where TComposer : IComposer
    {
        byte[] bytes = Revision.Serializers[typeof(TComposer)].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }
}
