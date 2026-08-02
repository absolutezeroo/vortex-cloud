using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Avatar;

/// <summary>
///     ChangePostureMessageComposer and SignMessageComposer both send a single int (verified against
///     the WIN63 AS3 ctors). The posture parser used to drop it and the room toggled the seat
///     instead, which desyncs as soon as the client and the room disagree on the current posture --
///     the client's own constants are POSTURE_STAND = 0 / POSTURE_SIT = 1.
/// </summary>
public sealed class PostureAndSignWireLayoutTests
{
    private const int ChangePostureMessageEvent = 3181;
    private const int SignMessageEvent = 211;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    [Theory]
    [InlineData(0, AvatarPostureType.Stand)]
    [InlineData(1, AvatarPostureType.Sit)]
    public void ChangePostureParser_ReadsThePostureTheClientAskedFor(
        int wireValue,
        AvatarPostureType expected
    )
    {
        ClientPacket packet = BuildClientPacket(
            ChangePostureMessageEvent,
            sp => sp.WriteInteger(wireValue)
        );

        ChangePostureMessage message = Revision
            .Parsers[ChangePostureMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<ChangePostureMessage>()
            .Subject;

        message.PostureType.Should().Be(expected);
    }

    [Fact]
    public void SignParser_ReadsTheSignId()
    {
        ClientPacket packet = BuildClientPacket(SignMessageEvent, sp => sp.WriteInteger(11));

        SignMessage message = Revision
            .Parsers[SignMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SignMessage>()
            .Subject;

        message.SignId.Should().Be(11);
    }
}
