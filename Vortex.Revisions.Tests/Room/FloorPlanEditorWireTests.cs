using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Outgoing.Room.Chat;
using Vortex.Primitives.Messages.Outgoing.Room.Layout;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Room;

/// <summary>
///     Two more registered serializers with an empty <c>Serialize</c> body. These two are worth
///     pinning because an empty body is not a silent no-op here: the client reads a count first, so
///     it takes "no bytes" as "zero of them" and carries on as though the server had answered.
///
///     Re-derived from WIN63-202607011411: <c>_SafeCls_4415</c> (occupied tiles) and
///     <c>_SafeCls_3195</c> (room chat settings).
/// </summary>
public sealed class FloorPlanEditorWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket SerializeAndReadBody(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    /// <summary>
    ///     A count, then two integers per tile. Not the bytes the height map uses for the very same
    ///     coordinates — this parser calls <c>readInteger</c>.
    /// </summary>
    [Fact]
    public void OccupiedTiles_WritesTheCountThenAPairOfIntsPerTile()
    {
        ClientPacket packet = SerializeAndReadBody(
            typeof(RoomOccupiedTilesMessageComposer),
            new RoomOccupiedTilesMessageComposer { Tiles = [(3, 4), (0, 0), (11, 2)] }
        );

        packet.PopInt().Should().Be(3);

        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(4);

        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0);

        packet.PopInt().Should().Be(11);
        packet.PopInt().Should().Be(2);

        packet.Remaining.Should().Be(0);
    }

    /// <summary>
    ///     The empty room still has to send the zero. The client indexes <c>_reservedTiles</c> by
    ///     [y][x] and only resets the grid inside this handler, so skipping the message leaves the
    ///     previous room's reserved tiles in place rather than clearing them.
    /// </summary>
    [Fact]
    public void OccupiedTiles_WritesAZeroCountWhenNothingIsPlaced()
    {
        ClientPacket packet = SerializeAndReadBody(
            typeof(RoomOccupiedTilesMessageComposer),
            new RoomOccupiedTilesMessageComposer { Tiles = ImmutableArray<(int X, int Y)>.Empty }
        );

        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    /// <summary>
    ///     One int, where the same settings object inside GuestRoomData takes five. The client's
    ///     <c>fromFloodSensitivity</c> fills mode, bubble width and scroll speed with its own
    ///     constants, so writing the other four would desynchronise the read.
    /// </summary>
    [Theory]
    [InlineData(ChatFloodSensitivityType.Extra, 0)]
    [InlineData(ChatFloodSensitivityType.Normal, 1)]
    [InlineData(ChatFloodSensitivityType.Minimal, 2)]
    public void RoomChatSettings_WritesTheFloodSensitivityAndNothingElse(
        ChatFloodSensitivityType sensitivity,
        int expected
    )
    {
        ClientPacket packet = SerializeAndReadBody(
            typeof(RoomChatSettingsMessageComposer),
            new RoomChatSettingsMessageComposer { FloodSensitivity = sensitivity }
        );

        packet.PopInt().Should().Be(expected);
        packet.Remaining.Should().Be(0);
    }
}
