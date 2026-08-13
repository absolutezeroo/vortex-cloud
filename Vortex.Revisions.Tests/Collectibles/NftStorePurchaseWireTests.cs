using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Collectibles;

/// <summary>
///     Buying from the Collectors Guild shop: request 3196, answer 448.
///
///     The offer is named by its product code, not by an id — the client's offer struct carries no
///     id at all, and its purchase dialog sends the code straight back.
/// </summary>
public sealed class NftStorePurchaseWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    /// <summary>Repeated rather than referenced: the header table is internal to the revision.</summary>
    private const int NftStorePurchaseMessageEvent = 3196;

    [Fact]
    public void TheRequest_IsTheProductCodeThenTheWallet()
    {
        ServerPacket request = new(NftStorePurchaseMessageEvent);
        request.WriteString("nft_chair").WriteString("0xabc");

        NftStorePurchaseMessage parsed = (NftStorePurchaseMessage)
            Revision
                .Parsers[NftStorePurchaseMessageEvent]
                .Parse(new ClientPacket(NftStorePurchaseMessageEvent, request.ToArray()));

        parsed.ProductCode.Should().Be("nft_chair");
        parsed.Wallet.Should().Be("0xabc");
    }

    /// <summary>
    ///     The same trap as the transfer and the claim, and the one worth a test of its own: the
    ///     client raises its purchase-error alert on code 1 and celebrates on everything else, zero
    ///     included. A refusal that forgot to set the code would congratulate the buyer on a
    ///     purchase that never happened.
    /// </summary>
    [Fact]
    public void TheAnswer_TellsSuccessFromFailureByANonZeroCode()
    {
        Body(NftStorePurchaseMessageComposer.Success).PopShort().Should().Be(0);
        Body(NftStorePurchaseMessageComposer.Error).PopShort().Should().NotBe(0);
    }

    private static ClientPacket Body(short result)
    {
        byte[] bytes = Revision
            .Serializers[typeof(NftStorePurchaseMessageComposer)]
            .Serialize(new NftStorePurchaseMessageComposer { Result = result })
            .ToArray();

        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }
}
