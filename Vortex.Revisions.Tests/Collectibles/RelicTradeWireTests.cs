using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Collectibles;

/// <summary>
///     Handing a Relic to another player: request 2481, answer 850.
///
///     This is what stands in for the wallet transfer, and the client was already built for it —
///     <c>CollectiblesModel.requestAddTrading</c> and the trade window's own Relic lists both
///     existed with nothing on the server behind them.
/// </summary>
public sealed class RelicTradeWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    /// <summary>Repeated rather than referenced: the header table is internal to the revision.</summary>
    private const int AddNftToTradeEvent = 2481;

    [Fact]
    public void TheRequest_IsACountThenThatManyAssetIds()
    {
        ServerPacket request = new(AddNftToTradeEvent);
        request.WriteInteger(2).WriteInteger(41).WriteInteger(42);

        AddNftToTradeMessage parsed = (AddNftToTradeMessage)
            Revision
                .Parsers[AddNftToTradeEvent]
                .Parse(new ClientPacket(AddNftToTradeEvent, request.ToArray()));

        parsed.AssetIds.Should().Equal(41, 42);
    }

    /// <summary>
    ///     The count arrives before the ids, so a client claiming a few million of them would be
    ///     believed and allocated for. Bounded by the same per-side trade limit as furniture.
    /// </summary>
    [Fact]
    public void TheRequest_RefusesAnImpossibleCount()
    {
        ServerPacket request = new(AddNftToTradeEvent);
        request.WriteInteger(int.MaxValue);

        Action parse = () =>
            Revision
                .Parsers[AddNftToTradeEvent]
                .Parse(new ClientPacket(AddNftToTradeEvent, request.ToArray()));

        parse.Should().Throw<System.IO.InvalidDataException>();
    }

    /// <summary>
    ///     Mine first, theirs second — and the client believes it. The two participants therefore
    ///     have to receive two different packets with the lists swapped, unlike the ordinary trade
    ///     item list, which names both sides by room-object id and can go out unchanged to both.
    /// </summary>
    [Fact]
    public void TheAnswer_IsWrittenFromTheReceiversSide()
    {
        ClientPacket packet = Body(
            new TradeNftAssetsMessageComposer
            {
                MyAssets = [Asset(11, "nft_lamp")],
                TheirAssets = [Asset(22, "nft_chair"), Asset(23, "nft_sofa")],
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopLong().Should().Be(11);
        SkipProductStruct(packet).Should().Be("nft_lamp");

        packet.PopInt().Should().Be(2);
        packet.PopLong().Should().Be(22);
        SkipProductStruct(packet).Should().Be("nft_chair");
        packet.PopLong().Should().Be(23);
        SkipProductStruct(packet).Should().Be("nft_sofa");

        packet.Remaining.Should().Be(0);
    }

    /// <summary>
    ///     An empty offer still has a shape: two counts. The list is re-sent whole on every change
    ///     because it is also what the inventory derives its Relic locks from.
    /// </summary>
    [Fact]
    public void AnEmptyTable_IsTwoCounts()
    {
        ClientPacket packet = Body(
            new TradeNftAssetsMessageComposer { MyAssets = [], TheirAssets = [] }
        );

        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    private static CollectibleAssetSnapshot Asset(long assetId, string productCode) =>
        new()
        {
            AssetId = assetId,
            Product = new CollectibleProductItemSnapshot
            {
                ProductTypeId = CollectibleProductIdentity.Floor,
                ItemTypeId = "7",
                Score = 3,
                // Deliberately non-zero: an asset is read through the base struct, which has no
                // amount field at all, so this must not reach the wire.
                Amount = 5,
                ProductCode = productCode,
            },
        };

    /// <summary>Reads the base product struct and returns the classname, which is what identifies
    /// which asset was written.</summary>
    private static string SkipProductStruct(ClientPacket packet)
    {
        packet.PopShort();
        packet.PopString();
        packet.PopInt();
        packet.PopString();
        packet.PopInt();

        string productCode = packet.PopString();

        packet.PopString();

        return productCode;
    }

    private static ClientPacket Body(TradeNftAssetsMessageComposer composer)
    {
        byte[] bytes = Revision
            .Serializers[typeof(TradeNftAssetsMessageComposer)]
            .Serialize(composer)
            .ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }
}
