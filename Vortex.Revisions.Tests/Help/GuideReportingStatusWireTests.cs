using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Help;

/// <summary>
///     Header 3725, re-derived from WIN63's <c>_SafeCls_2138</c> and the ticket struct
///     <c>_SafeCls_2969</c>. The composer had no fields and was registered nowhere.
///
///     Two conditionals stacked on each other make this the one worth pinning: the ticket block
///     exists only for status 1, and inside it the tail depends on the ticket type — with type 3
///     depending on a boolean read three fields earlier.
/// </summary>
public sealed class GuideReportingStatusWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Body(GuideReportingStatusMessageComposer composer)
    {
        byte[] bytes = Revision
            .Serializers[typeof(GuideReportingStatusMessageComposer)]
            .Serialize(composer)
            .ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void NothingPending_IsTheStatusAlone()
    {
        // Status 0 is what opens the new-help window, and it carries no ticket block at all.
        ClientPacket packet = Body(new GuideReportingStatusMessageComposer { StatusCode = 0 });

        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void FeedbackStatus_CarriesNoTicketEither()
    {
        // Anything past 1 is a feedback message the client keys off statusCode - 2; there is no
        // struct behind it.
        ClientPacket packet = Body(new GuideReportingStatusMessageComposer { StatusCode = 5 });

        packet.PopInt().Should().Be(5);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void PlainTicket_WritesTheHeaderThenTwoStrings()
    {
        ClientPacket packet = Body(
            new GuideReportingStatusMessageComposer
            {
                StatusCode = 1,
                PendingTicket = new GuidePendingTicket
                {
                    TicketType = 0,
                    SecondsAgo = 42,
                    IsGuide = false,
                    OtherPartyName = "Ctuto",
                    OtherPartyFigure = "hd-180-1",
                },
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(42);
        packet.PopBoolean().Should().BeFalse();
        packet.PopString().Should().Be("Ctuto");
        packet.PopString().Should().Be("hd-180-1");
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void TicketTypeOne_AddsTheDescription()
    {
        ClientPacket packet = Body(
            new GuideReportingStatusMessageComposer
            {
                StatusCode = 1,
                PendingTicket = new GuidePendingTicket
                {
                    TicketType = 1,
                    SecondsAgo = 0,
                    IsGuide = false,
                    OtherPartyName = "A",
                    OtherPartyFigure = "F",
                    Description = "I need help",
                    RoomName = "ignored",
                },
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(0);
        packet.PopBoolean().Should().BeFalse();
        packet.PopString().Should().Be("A");
        packet.PopString().Should().Be("F");
        packet.PopString().Should().Be("I need help");
        packet.Remaining.Should().Be(0); // the room name belongs to type 3, not here
    }

    [Fact]
    public void TicketTypeThree_WritesItsStringsToARequester()
    {
        ClientPacket packet = Body(
            new GuideReportingStatusMessageComposer
            {
                StatusCode = 1,
                PendingTicket = new GuidePendingTicket
                {
                    TicketType = 3,
                    SecondsAgo = 7,
                    IsGuide = false,
                    OtherPartyName = "A",
                    OtherPartyFigure = "F",
                    RoomName = "Welcome Lounge",
                },
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(7);
        packet.PopBoolean().Should().BeFalse();
        packet.PopString().Should().Be("A");
        packet.PopString().Should().Be("F");
        packet.PopString().Should().Be("Welcome Lounge");
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void TicketTypeThree_WritesNoStringsToAGuide()
    {
        // The trap. For type 3 the client reads the three strings only when isGuide is false, so
        // writing them to a guide desynchronises every message that follows — and the field that
        // decides sits three fields earlier, not next to the strings.
        ClientPacket packet = Body(
            new GuideReportingStatusMessageComposer
            {
                StatusCode = 1,
                PendingTicket = new GuidePendingTicket
                {
                    TicketType = 3,
                    SecondsAgo = 7,
                    IsGuide = true,
                    OtherPartyName = "A",
                    OtherPartyFigure = "F",
                    RoomName = "Welcome Lounge",
                },
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(7);
        packet.PopBoolean().Should().BeTrue();
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void UnknownTicketType_EndsAfterTheBoolean()
    {
        // The client's switch falls through and returns, so anything more would be read as the
        // start of the next message.
        ClientPacket packet = Body(
            new GuideReportingStatusMessageComposer
            {
                StatusCode = 1,
                PendingTicket = new GuidePendingTicket
                {
                    TicketType = 9,
                    SecondsAgo = 1,
                    IsGuide = false,
                    OtherPartyName = "A",
                    OtherPartyFigure = "F",
                },
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(9);
        packet.PopInt().Should().Be(1);
        packet.PopBoolean().Should().BeFalse();
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void StatusOneWithoutATicket_FallsBackRatherThanLying()
    {
        // Status 1 promises a struct. With none to write, the status is downgraded to 0 instead:
        // the alternative is the client parsing whatever follows in the buffer as a ticket.
        ClientPacket packet = Body(
            new GuideReportingStatusMessageComposer { StatusCode = 1, PendingTicket = null }
        );

        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }
}
