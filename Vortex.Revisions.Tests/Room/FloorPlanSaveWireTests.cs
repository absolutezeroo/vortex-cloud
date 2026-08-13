using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Room.Layout;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Room;

/// <summary>
///     Saving the floor-plan editor, header 2937 (<c>_SafeCls_2580</c>).
///
///     One composer, three body lengths. It sends the model string alone when every other argument
///     is -1, six fields when the wall height is left unset, and seven otherwise — so a fixed-width
///     parser throws on two of the three forms it will actually receive. The parser used to read
///     nothing at all, which threw on none of them and lost every field.
/// </summary>
public sealed class FloorPlanSaveWireTests
{
    private const int UpdateFloorProperties = 2937;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void ReadsTheFullSevenFieldForm()
    {
        UpdateFloorPropertiesMessage message = Parse(
            new Writer().String("xxxx\rx000\rx000").Int(1).Int(2).Int(4).Int(0).Int(1).Int(12)
        );

        message.Model.Should().Be("xxxx\rx000\rx000");
        message.DoorX.Should().Be(1);
        message.DoorY.Should().Be(2);
        message.DoorRotation.Should().Be(4);
        message.WallThickness.Should().Be(0);
        message.FloorThickness.Should().Be(1);
        message.WallHeight.Should().Be(12);
    }

    [Fact]
    public void ReadsTheSixFieldFormAndLeavesTheWallHeightUnset()
    {
        // What the editor sends whenever its wall-height setting is not ticked, which is the
        // common case — so this is the form a fixed-width parser would break on first.
        UpdateFloorPropertiesMessage message = Parse(
            new Writer().String("x00\rx00").Int(1).Int(1).Int(2).Int(0).Int(0)
        );

        message.DoorX.Should().Be(1);
        message.FloorThickness.Should().Be(0);
        message.WallHeight.Should().Be(-1);
    }

    [Fact]
    public void ReadsTheModelOnlyForm()
    {
        // Sent when every other argument is -1. Everything else stays unset rather than defaulting
        // to zero: a door at (0,0) is a real position, "not sent" is not.
        UpdateFloorPropertiesMessage message = Parse(new Writer().String("x0\rx0"));

        message.Model.Should().Be("x0\rx0");
        message.DoorX.Should().Be(-1);
        message.DoorY.Should().Be(-1);
        message.DoorRotation.Should().Be(-1);
        message.WallThickness.Should().Be(-1);
        message.FloorThickness.Should().Be(-1);
        message.WallHeight.Should().Be(-1);
    }

    [Fact]
    public void StopsAtAPartialTrailingField()
    {
        // Three stray bytes are not an int. Reading them would take the string length of whatever
        // came next in the buffer.
        Writer writer = new Writer().String("x0").Int(3);
        List<byte> bytes = [.. writer.ToArray(), 0, 0, 1];

        UpdateFloorPropertiesMessage message = (UpdateFloorPropertiesMessage)
            Revision.Parsers[UpdateFloorProperties].Parse(new ClientPacket(0, bytes.ToArray()));

        message.DoorX.Should().Be(3);
        message.DoorY.Should().Be(-1);
    }

    private static UpdateFloorPropertiesMessage Parse(Writer writer) =>
        (UpdateFloorPropertiesMessage)
            Revision.Parsers[UpdateFloorProperties].Parse(new ClientPacket(0, writer.ToArray()));

    private sealed class Writer
    {
        private readonly List<byte> _bytes = [];

        public Writer Int(int value)
        {
            _bytes.Add((byte)(value >> 24));
            _bytes.Add((byte)(value >> 16));
            _bytes.Add((byte)(value >> 8));
            _bytes.Add((byte)value);
            return this;
        }

        public Writer String(string value)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(value);
            _bytes.Add((byte)(utf8.Length >> 8));
            _bytes.Add((byte)utf8.Length);
            _bytes.AddRange(utf8);
            return this;
        }

        public byte[] ToArray() => [.. _bytes];
    }
}
