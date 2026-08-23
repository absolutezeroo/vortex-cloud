using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.UserDefinedRoomEvents;

/// <summary>
///     A contract is saved and read back through two halves of the same format, and the client uses
///     one class for both — so the two have to agree field for field. They are checked against each
///     other here: what the editor saves is serialized back out and read again.
/// </summary>
/// <remarks>
///     The type is a <em>short</em> where the id is an int, and it decides the tail. Getting that
///     wrong does not throw at either end — it consumes the next message — which is exactly why the
///     round trip is asserted rather than the bytes.
/// </remarks>
public sealed class WiredContractWireTests
{
    private const int SaveWiredContractEvent = 1908;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void APaymentContract_SurvivesTheRoundTrip()
    {
        List<byte> body = [.. Int32(4200), .. Int16(0)];

        // youGive: one alternative of two terms — 3 of a sofa, and 50 coins.
        body.Add(1);
        body.AddRange(Int32(1));
        body.AddRange(Int32(2));
        body.Add(1);
        body.AddRange(Int32(3));
        body.Add(0);
        body.AddRange(Int32(1234));
        body.AddRange(Str(""));
        body.Add(0);
        body.AddRange(Int32(50));

        // youGet: nothing.
        body.Add(0);

        // The payment tail.
        body.AddRange(Int16(1));
        body.AddRange(Str("a sofa and fifty coins"));
        body.AddRange(Str("layout_b"));

        SaveWiredContractMessage saved = Parse(body);

        saved.Contract.ContractId.Should().Be(4200);
        saved.Contract.ContractType.Should().Be(0);
        saved.Contract.YouGiveRules!.Value.Should().HaveCount(1);
        saved.Contract.YouGiveRules.Value[0].Nodes.Should().HaveCount(2);
        saved.Contract.YouGiveRules.Value[0].Nodes[0].IsFurni.Should().BeTrue();
        saved.Contract.YouGiveRules.Value[0].Nodes[0].ItemType!.SpriteId.Should().Be(1234);
        saved.Contract.YouGiveRules.Value[0].Nodes[1].Amount.Should().Be(50);
        saved.Contract.YouGetRule.Should().BeNull();
        saved.Contract.PaymentMode.Should().Be(1);
        saved.Contract.ReceiveText.Should().Be("a sofa and fifty coins");
        saved.Contract.LayoutType.Should().Be("layout_b");

        // And back out: the editor reads its own save from the reply.
        ClientPacket reply = Serialize(saved);

        reply.PopInt().Should().Be(4200);
        reply.PopShort().Should().Be(0);
        reply.PopBoolean().Should().BeTrue();
        reply.PopInt().Should().Be(1);
        reply.PopInt().Should().Be(2);
        reply.PopByte().Should().Be(1);
        reply.PopInt().Should().Be(3);
        reply.PopBoolean().Should().BeFalse();
        reply.PopInt().Should().Be(1234);
        reply.PopString().Should().BeEmpty();
        reply.PopByte().Should().Be(0);
        reply.PopInt().Should().Be(50);
        reply.PopBoolean().Should().BeFalse();
        reply.PopShort().Should().Be(1);
        reply.PopString().Should().Be("a sofa and fifty coins");
        reply.PopString().Should().Be("layout_b");
        reply.End.Should().BeTrue("a payment contract ends after its layout");
    }

    /// <summary>A reward contract carries three different trailing fields, in its own order.</summary>
    [Fact]
    public void ARewardContract_ReadsItsOwnTail()
    {
        List<byte> body = [.. Int32(7), .. Int16(2)];

        body.Add(0); // no give side
        body.Add(1); // one receive rule
        body.AddRange(Int32(1));
        body.Add(0);
        body.AddRange(Int32(25));

        body.AddRange(Int16(13));
        body.Add(1);
        body.AddRange(Str("well done"));

        SaveWiredContractMessage saved = Parse(body);

        saved.Contract.YouGiveRules.Should().BeNull();
        saved.Contract.YouGetRule!.Nodes[0].Amount.Should().Be(25);
        saved.Contract.RewardCategory.Should().Be(13);
        saved.Contract.ShowDialog.Should().BeTrue();
        saved.Contract.RewardText.Should().Be("well done");

        ClientPacket reply = Serialize(saved);

        reply.PopInt();
        reply.PopShort();
        reply.PopBoolean().Should().BeFalse();
        reply.PopBoolean().Should().BeTrue();
        reply.PopInt().Should().Be(1);
        reply.PopByte().Should().Be(0);
        reply.PopInt().Should().Be(25);
        reply.PopShort().Should().Be(13);
        reply.PopBoolean().Should().BeTrue();
        reply.PopString().Should().Be("well done");
        reply.End.Should().BeTrue();
    }

    /// <summary>A trade contract has no tail at all, and reading one would eat the next message.</summary>
    [Fact]
    public void ATradeContract_HasNoTail()
    {
        List<byte> body = [.. Int32(9), .. Int16(1), 0, 0];

        SaveWiredContractMessage saved = Parse(body);

        saved.Contract.ContractType.Should().Be(1);
        saved.Contract.YouGiveRules.Should().BeNull();
        saved.Contract.YouGetRule.Should().BeNull();

        ClientPacket reply = Serialize(saved);

        reply.PopInt().Should().Be(9);
        reply.PopShort().Should().Be(1);
        reply.PopBoolean().Should().BeFalse();
        reply.PopBoolean().Should().BeFalse();
        reply.End.Should().BeTrue();
    }

    private static SaveWiredContractMessage Parse(List<byte> body) =>
        (SaveWiredContractMessage)
            Revision.Parsers[SaveWiredContractEvent].Parse(new ClientPacket(0, body.ToArray()));

    private static ClientPacket Serialize(SaveWiredContractMessage saved)
    {
        byte[] bytes = Revision
            .Serializers[typeof(WiredContractContentsMessageComposer)]
            .Serialize(new WiredContractContentsMessageComposer { Contract = saved.Contract })
            .ToArray();

        byte[] payload = new byte[bytes.Length - 6];

        Array.Copy(bytes, 6, payload, 0, payload.Length);

        return new ClientPacket(0, payload);
    }

    private static byte[] Int32(int value) =>
        [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] Int16(int value) => [(byte)(value >> 8), (byte)value];

    private static byte[] Str(string value)
    {
        byte[] text = Encoding.UTF8.GetBytes(value);

        return [.. Int16(text.Length), .. text];
    }
}
