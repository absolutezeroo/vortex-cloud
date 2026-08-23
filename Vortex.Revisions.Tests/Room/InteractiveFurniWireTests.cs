using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Room;

/// <summary>
///     The interactive-furni widgets (mannequin, sticky note, custom stack height) all parsed to
///     empty messages, so nothing they sent could ever be acted on. Two of the layouts have a shape
///     a hand-written parser gets wrong: the stack-height widget appends its multi-walk flag only
///     when the checkbox is what moved, and the sticky-note composer emits its colour and text in
///     the opposite order to its own constructor parameters.
/// </summary>
public sealed class InteractiveFurniWireTests
{
    private const int SetCustomStackingHeightEvent = 3045;
    private const int SetMannequinFigureEvent = 2301;
    private const int SetMannequinNameEvent = 606;
    private const int PlacePostItMessageEvent = 1122;
    private const int AddSpamWallPostItMessageEvent = 2684;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static T Parse<T>(int header, Action<ServerPacket> write)
        where T : class
    {
        ServerPacket sp = new(header);
        write(sp);

        return Revision
            .Parsers[header]
            .Parse(new ClientPacket(header, sp.ToArray()))
            .Should()
            .BeOfType<T>()
            .Subject;
    }

    [Fact]
    public void StackingHeightParser_ReadsTheOptionalMultiWalkFlagWhenPresent()
    {
        SetCustomStackingHeightMessage message = Parse<SetCustomStackingHeightMessage>(
            SetCustomStackingHeightEvent,
            sp => sp.WriteInteger(4711).WriteInteger(250).WriteBoolean(true)
        );

        message.ObjectId.Value.Should().Be(4711);
        message.Height.Should().Be(250);
        message.MultiWalkMode.Should().BeTrue();
    }

    [Fact]
    public void StackingHeightParser_LeavesTheFlagUnsetWhenOnlyTheHeightChanged()
    {
        SetCustomStackingHeightMessage message = Parse<SetCustomStackingHeightMessage>(
            SetCustomStackingHeightEvent,
            sp => sp.WriteInteger(4711).WriteInteger(250)
        );

        message.MultiWalkMode.Should().BeNull("the widget omits the flag unless it is what moved");
    }

    [Fact]
    public void StackingHeightParser_CarriesTheClearSentinelThrough()
    {
        SetCustomStackingHeightMessage message = Parse<SetCustomStackingHeightMessage>(
            SetCustomStackingHeightEvent,
            sp => sp.WriteInteger(4711).WriteInteger(-100)
        );

        message.Height.Should().Be(-100, "-100 is the widget's clear-the-custom-height sentinel");
    }

    [Fact]
    public void MannequinFigureParser_ReadsOnlyTheObjectId()
    {
        SetMannequinFigureMessage message = Parse<SetMannequinFigureMessage>(
            SetMannequinFigureEvent,
            sp => sp.WriteInteger(4711)
        );

        message.ObjectId.Value.Should().Be(4711);
    }

    [Fact]
    public void MannequinNameParser_ReadsTheObjectIdThenTheName()
    {
        SetMannequinNameMessage message = Parse<SetMannequinNameMessage>(
            SetMannequinNameEvent,
            sp => sp.WriteInteger(4711).WriteString("Summer look")
        );

        message.ObjectId.Value.Should().Be(4711);
        message.Name.Should().Be("Summer look");
    }

    [Fact]
    public void PlacePostItParser_ReadsTheObjectIdAndWallLocation()
    {
        SetPostItExpectations(
            Parse<PlacePostItMessage>(
                PlacePostItMessageEvent,
                sp => sp.WriteInteger(4711).WriteString(":w=3,7 l=10,20 r")
            )
        );

        static void SetPostItExpectations(PlacePostItMessage message)
        {
            message.ObjectId.Value.Should().Be(4711);
            message.Location.Should().Be(":w=3,7 l=10,20 r");
        }
    }

    [Fact]
    public void AddSpamWallPostItParser_ReadsTheColourBeforeTheText()
    {
        AddSpamWallPostItMessage message = Parse<AddSpamWallPostItMessage>(
            AddSpamWallPostItMessageEvent,
            sp =>
                sp.WriteInteger(4711)
                    .WriteString(":w=3,7 l=10,20 r")
                    .WriteString("FFFF33")
                    .WriteString("back in 5")
        );

        message.ObjectId.Value.Should().Be(4711);
        message.Location.Should().Be(":w=3,7 l=10,20 r");
        message.ColorHex.Should().Be("FFFF33");
        message.Text.Should().Be("back in 5");
    }
}
