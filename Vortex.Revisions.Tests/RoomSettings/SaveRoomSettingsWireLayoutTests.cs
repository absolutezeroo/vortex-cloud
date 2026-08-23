using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.RoomSettings;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.RoomSettings;

/// <summary>
/// The other half of the room-settings dialog. Its composer
/// (<c>_SafePkg_2789._SafeCls_3383</c> in the WIN63-202607011411 client) tails off after
/// <c>whoCanBan</c> with a single flood-sensitivity integer and six idle/door/pet fields, where
/// the parser used to expect the old five-field chat block plus a dynamic-categories flag. Reading
/// four integers out of what is really boolean/int/boolean/int/boolean fed the room grain garbage
/// chat settings on every save.
/// </summary>
public sealed class SaveRoomSettingsWireLayoutTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void Parser_ReadsWhatTheClientComposerSends()
    {
        SaveRoomSettingsMessage message = Parse(ComposeAsClient());

        message.RoomId.Should().Be(42);
        message.RoomName.Should().Be("HQ");
        message.RoomDescription.Should().Be("desc");
        message.DoorMode.Should().Be((int)RoomDoorModeType.Password);
        message.Password.Should().Be("hunter2");
        message.MaxVisitors.Should().Be(25);
        message.CategoryId.Should().Be(3);
        message.Tags.Should().Equal("build", "chat");
        message.TradeMode.Should().Be(RoomTradeModeType.Everyone);
        message.AllowPets.Should().BeTrue();
        message.AllowFoodConsume.Should().BeFalse();
        message.AllowWalkThrough.Should().BeTrue();
        message.HideWalls.Should().BeFalse();
        message.WallThickness.Should().Be(RoomThicknessType.Thin);
        message.FloorThickness.Should().Be(RoomThicknessType.Thick);
        message.WhoCanMute.Should().Be(ModSettingType.Rights);
        message.WhoCanKick.Should().Be(ModSettingType.GroupRights);
        message.WhoCanBan.Should().Be(ModSettingType.Owner);
        message.ChatFloodSensitivity.Should().Be(ChatFloodSensitivityType.Minimal);
        message.LeaveOnDoorTileEnabled.Should().BeTrue();
        message.IdleSleepEnabled.Should().BeFalse();
        message.IdleSleepTimeoutSeconds.Should().Be(600);
        message.IdleAutokickEnabled.Should().BeTrue();
        message.IdleAutokickTimeoutSeconds.Should().Be(900);
        message.MuteAllPets.Should().BeTrue();
    }

    /// <summary>Replays the client composer's push order.</summary>
    private static byte[] ComposeAsClient()
    {
        ServerPacket packet = new(0);

        packet
            .WriteInteger(42)
            .WriteString("HQ")
            .WriteString("desc")
            .WriteInteger((int)RoomDoorModeType.Password)
            .WriteString("hunter2")
            .WriteInteger(25)
            .WriteInteger(3)
            .WriteInteger(2)
            .WriteString("build")
            .WriteString("chat")
            .WriteInteger((int)RoomTradeModeType.Everyone)
            .WriteBoolean(true) // allowPets
            .WriteBoolean(false) // allowFoodConsume
            .WriteBoolean(true) // allowWalkThrough
            .WriteBoolean(false) // hideWalls
            .WriteInteger((int)RoomThicknessType.Thin)
            .WriteInteger((int)RoomThicknessType.Thick)
            .WriteInteger((int)ModSettingType.Rights)
            .WriteInteger((int)ModSettingType.GroupRights)
            .WriteInteger((int)ModSettingType.Owner)
            .WriteInteger((int)ChatFloodSensitivityType.Minimal)
            .WriteBoolean(true) // leaveOnDoorTileEnabled
            .WriteBoolean(false) // idleSleepEnabled
            .WriteInteger(600)
            .WriteBoolean(true) // idleAutokickEnabled
            .WriteInteger(900)
            .WriteBoolean(true); // muteAllPets

        return packet.Stream.ToArray();
    }

    private static SaveRoomSettingsMessage Parse(byte[] payload)
    {
        IMessageEvent parsed = Revision
            .Parsers[MessageEventHeader]
            .Parse(new ClientPacket(MessageEventHeader, payload));

        return parsed.Should().BeOfType<SaveRoomSettingsMessage>().Subject;
    }

    private const int MessageEventHeader = 725;
}
