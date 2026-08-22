using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.UserDefinedRoomEvents;

/// <summary>
///     A real UpdateAction (2197) captured from the WIN63 client, kept byte for byte.
/// </summary>
/// <remarks>
///     The client's composer ends with two array lengths pushed unconditionally -- eight bytes even
///     when both lists are empty -- and the wire carries two. The parser used to read the first of
///     those lengths regardless and threw <c>Not enough data: need 4, remaining 2</c>, which dropped
///     the whole message: a player configuring the box saw their settings silently refused.
///     <para>
///         Everything before the tail is present and correct, and this test pins that: the box id,
///         its six int params, the empty string, no stuff ids, the action delay, two furni sources
///         and one user source. The tail is whatever the client chose to send and is not read.
///     </para>
/// </remarks>
public sealed class UpdateWiredDataTailTests
{
    private const int UpdateActionMessageEvent = 2197;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    /// <summary>
    ///     Captured 2026-08-22 from a give-furni-from-chest box being configured. Length prefix and
    ///     header included, because that is what a ClientPacket is handed.
    /// </summary>
    private static readonly byte[] Captured =
    [
        0x00,
        0x00,
        0x00,
        0x42, // body length: 66
        0x08,
        0x95, // header 2197
        0x00,
        0x00,
        0x01,
        0xa1, // id 417
        0x00,
        0x00,
        0x00,
        0x06, // six int params
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x01,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00, // string param: empty
        0x00,
        0x00,
        0x00,
        0x00, // no stuff ids
        0x00,
        0x00,
        0x00,
        0x00, // action delay 0
        0x00,
        0x00,
        0x00,
        0x02, // two furni sources
        0x00,
        0x00,
        0x00,
        0x01,
        0x00,
        0x00,
        0x00,
        0xc9,
        0x00,
        0x00,
        0x00,
        0x01, // one user source
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00, // the tail the composer says should be eight bytes
    ];

    /// <summary>
    ///     A packet as the decoder hands it over: the whole frame, with the length and the header
    ///     already consumed. ClientPacketDecoder does exactly this before a parser ever sees it, and
    ///     a test that starts at byte zero would be reading the length as the box id.
    /// </summary>
    private static ClientPacket AsDecoded(byte[] frame)
    {
        ClientPacket packet = new(-1, frame);

        _ = packet.PopInt();
        packet.Header = packet.PopShort();

        return packet;
    }

    [Fact]
    public void UpdateAction_WithAShortTail_KeepsEveryFieldTheClientDidSend()
    {
        ClientPacket packet = AsDecoded(Captured);

        UpdateActionMessage message = Revision
            .Parsers[UpdateActionMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<UpdateActionMessage>()
            .Subject;

        message.Id.Should().Be(417);
        message.IntParams.Should().Equal(0, 1, 0, 0, 0, 0);
        message.StringParam.Should().BeEmpty();
        message.StuffIds.Should().BeEmpty();
        message.DefinitionSpecifics.Should().Equal([0]);
        message.FurniSources.Should().HaveCount(2);
        message.PlayerSources.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateAction_WithNoTailAtAll_StillParses()
    {
        byte[] truncated = new byte[Captured.Length - 2];
        Array.Copy(Captured, truncated, truncated.Length);

        ClientPacket packet = AsDecoded(truncated);

        Revision
            .Parsers[UpdateActionMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<UpdateActionMessage>()
            .Subject.IntParams.Should()
            .Equal(0, 1, 0, 0, 0, 0);
    }
}
