using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Action;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.RoomSettings;

/// <summary>
///     The room settings window always names the room it is editing, because it is normally opened
///     from the navigator while the player stands in the hotel view or in a different room. Two of
///     its buttons had parsers that read nothing (or not everything) and handlers that fell back to
///     the session's current room, so the button either did nothing or hit the wrong room.
///
///     Client truth, WIN63-202607011411:
///     <list type="bullet">
///         <item>
///             <c>RemoveAllRights</c> (159): <c>RoomSettingsCtrl.as:1480</c> sends
///             <c>new _SafeCls_3226(this._flatId)</c>, and the composer pushes that one int.
///         </item>
///         <item>
///             <c>UnbanUserFromRoom</c> (2804): <c>RoomSettingsCtrl.as:1511</c> sends
///             <c>new _SafeCls_2593(userId, _flatId)</c>, pushed in that order.
///         </item>
///     </list>
/// </summary>
public sealed class FlatControllerRoomIdWireTests
{
    private const int RemoveAllRightsMessageEvent = 159;
    private const int UnbanUserFromRoomMessageEvent = 2804;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    [Fact]
    public void RemoveAllRightsParser_ReadsTheRoomIdTheDialogNames()
    {
        ClientPacket packet = BuildClientPacket(
            RemoveAllRightsMessageEvent,
            sp => sp.WriteInteger(4271)
        );

        RemoveAllRightsMessage message = Revision
            .Parsers[RemoveAllRightsMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<RemoveAllRightsMessage>()
            .Subject;

        message.RoomId.Should().Be(4271);
    }

    [Fact]
    public void UnbanUserFromRoomParser_ReadsTheUserThenTheRoom()
    {
        ClientPacket packet = BuildClientPacket(
            UnbanUserFromRoomMessageEvent,
            sp => sp.WriteInteger(77).WriteInteger(4271)
        );

        UnbanUserFromRoomMessage message = Revision
            .Parsers[UnbanUserFromRoomMessageEvent]
            .Parse(packet)
            .Should()
            .BeOfType<UnbanUserFromRoomMessage>()
            .Subject;

        message.UserId.Should().Be(77);
        message.RoomId.Should().Be(4271);
    }
}
