using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.RaidProtection;
using Vortex.Protocol.Messages.Incoming.Room.RaidProtection;
using Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Room;

/// <summary>
/// The raid-protection panel, read against the AIR 1.0.31 client that owns it.
/// </summary>
/// <remarks>
/// The layout is not guessable and one field of it is actively counter-intuitive: the save reply
/// writes the room id, then the result code, then the <em>rest</em> of the snapshot — because the
/// client's parser calls <c>readAfterRoomId</c> with an int it read before the code
/// (<c>_SafePkg_4207/_SafeCls_4512.parse</c>). Get that wrong and the client reads the result code
/// as a room id, drops the reply as belonging to another room, and the panel hangs on "saving"
/// forever with no error anywhere.
/// </remarks>
public sealed class RaidProtectionWireLayoutTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void Capability_IsARoomIdAndAFlag()
    {
        ClientPacket body = Serialize(
            new RaidProtectionCapabilityMessageComposer { RoomId = 42, CanManage = true }
        );

        body.PopInt().Should().Be(42, "roomId");
        body.PopBoolean().Should().BeTrue("canManage");
        body.End.Should().BeTrue();
    }

    [Fact]
    public void Settings_MatchTheClientParserFieldForField()
    {
        ClientPacket body = Serialize(
            new RaidProtectionSettingsMessageComposer { Settings = Snapshot() }
        );

        ReadSnapshotAfterRoomId(body, expectedRoomId: 42);
        body.End.Should().BeTrue();
    }

    [Fact]
    public void SaveResult_WritesTheRoomIdBeforeTheCodeAndTheSnapshotAfterIt()
    {
        ClientPacket body = Serialize(
            new RaidProtectionSettingsResultMessageComposer
            {
                Result = RaidProtectionSaveResult.InvalidSettings,
                Settings = Snapshot(),
            }
        );

        body.PopInt().Should().Be(42, "roomId comes first, ahead of the code");
        body.PopInt().Should().Be(3, "resultCode — InvalidSettings, written as its numeric value");

        // And the room id is written once, not twice: the client already has it.
        body.PopBoolean().Should().BeTrue("enabled");
        body.PopInt().Should().Be(2, "detectionSensitivity");
        body.PopInt().Should().Be(1, "actionType");
        body.PopInt().Should().Be(86400, "banDurationSeconds");
        body.PopBoolean().Should().BeTrue("guardEnabled");
        body.PopInt().Should().Be(1800, "guardDurationSeconds");
        body.PopInt().Should().Be(0, "guardSensitivity");
        body.PopBoolean().Should().BeTrue("incidentActive");
        body.PopInt().Should().Be(1_760_000_000, "lastRaidAtEpochSeconds");
        body.End.Should().BeTrue();
    }

    /// <summary>
    /// The draft the panel sends: nine fields, and the three booleans are one byte each — the
    /// client's encoder writes an AS3 <c>Boolean</c> with <c>ByteArray.writeBoolean</c>
    /// (<c>wireformat/_SafeCls_3973.as:45</c>), not as a four-byte int like some of this client's
    /// other flags.
    /// </summary>
    [Fact]
    public void SaveRequest_ParsesTheNineFieldsTheClientPushes()
    {
        ServerPacket packet = new(0);

        packet.WriteInteger(42);
        packet.WriteBoolean(true);
        packet.WriteInteger(1);
        packet.WriteInteger(0);
        packet.WriteInteger(300);
        packet.WriteBoolean(false);
        packet.WriteInteger(10800);
        packet.WriteInteger(2);
        packet.WriteBoolean(true);

        // No trimming here: a bare ServerPacket writes no length/header preamble, unlike a
        // serializer's output, so what it holds is already the body the parser reads.
        IMessageEvent parsed = Revision
            .Parsers[Headers.SaveRaidProtection]
            .Parse(new ClientPacket(Headers.SaveRaidProtection, packet.ToArray()));

        SaveRaidProtectionSettingsMessage message = parsed
            .Should()
            .BeOfType<SaveRaidProtectionSettingsMessage>()
            .Subject;

        message.RoomId.Should().Be(42);
        message.Enabled.Should().BeTrue();
        message.DetectionSensitivity.Should().Be(1);
        message.ActionType.Should().Be(0);
        message.BanDurationSeconds.Should().Be(300);
        message.GuardEnabled.Should().BeFalse();
        message.GuardDurationSeconds.Should().Be(10800);
        message.GuardSensitivity.Should().Be(2);
        message.Confirmed.Should().BeTrue();
    }

    private static void ReadSnapshotAfterRoomId(ClientPacket body, int expectedRoomId)
    {
        body.PopInt().Should().Be(expectedRoomId, "roomId");
        body.PopBoolean().Should().BeTrue("enabled");
        body.PopInt().Should().Be(2, "detectionSensitivity");
        body.PopInt().Should().Be(1, "actionType");
        body.PopInt().Should().Be(86400, "banDurationSeconds");
        body.PopBoolean().Should().BeTrue("guardEnabled");
        body.PopInt().Should().Be(1800, "guardDurationSeconds");
        body.PopInt().Should().Be(0, "guardSensitivity");
        body.PopBoolean().Should().BeTrue("incidentActive");
        body.PopInt().Should().Be(1_760_000_000, "lastRaidAtEpochSeconds");
    }

    private static RoomRaidProtectionSnapshot Snapshot() =>
        new()
        {
            RoomId = 42,
            Enabled = true,
            DetectionSensitivity = 2,
            ActionType = 1,
            BanDurationSeconds = 86400,
            GuardEnabled = true,
            GuardDurationSeconds = 1800,
            GuardSensitivity = 0,
            IncidentActive = true,
            LastRaidAtEpochSeconds = 1_760_000_000,
        };

    private static ClientPacket Serialize(IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composer.GetType()].Serialize(composer).ToArray();

        byte[] payload = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, payload, 0, payload.Length);

        return new ClientPacket(0, payload);
    }

    private static class Headers
    {
        /// <summary>
        /// The AIR client's own id for the save, which this revision keeps (WIN63 sends nothing on
        /// it). Spelled out here rather than imported: <c>MessageEvent</c> is internal to the
        /// revision, and a test that hardcodes the number is one that fails if the number moves.
        /// </summary>
        public const int SaveRaidProtection = 2687;
    }
}
