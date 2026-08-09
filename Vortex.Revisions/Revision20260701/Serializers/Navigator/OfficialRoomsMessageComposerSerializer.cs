using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Navigator.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

/// <summary>
/// Layout re-derived from the client's parser (entries, then an int guard followed by the optional
/// ad entry, then the promoted groups). The guard is an int, not a boolean, and the client only
/// reads the ad entry when it is greater than zero.
/// </summary>
internal class OfficialRoomsMessageComposerSerializer(int header)
    : AbstractSerializer<OfficialRoomsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, OfficialRoomsMessageComposer message)
    {
        packet.WriteInteger(message.Entries.Length);

        foreach (OfficialRoomEntrySnapshot entry in message.Entries)
        {
            SerializeEntry(packet, entry);
        }

        if (message.AdRoom is null)
        {
            packet.WriteInteger(0);
        }
        else
        {
            packet.WriteInteger(1);
            SerializeEntry(packet, message.AdRoom);
        }

        packet.WriteInteger(message.PromotedRooms.Length);

        foreach (PromotedRoomCategorySnapshot promoted in message.PromotedRooms)
        {
            packet.WriteString(promoted.Code).WriteString(promoted.LeaderFigure);

            // The client reads the first room before entering its loop, so a zero-room group would
            // leave it decoding a block that was never written. Groups are built with at least one
            // room; guard the count anyway so a future caller cannot desynchronize the stream.
            packet.WriteInteger(promoted.Rooms.Length);

            foreach (RoomInfoSnapshot room in promoted.Rooms)
            {
                RoomSettingsSerializer.Serialize(packet, room);
            }
        }
    }

    private static void SerializeEntry(IServerPacket packet, OfficialRoomEntrySnapshot entry)
    {
        // The type decides what the client reads next, so a "room" entry with no room attached
        // would leave it waiting on a block that never arrives. Demote it before announcing a type
        // the payload cannot honour.
        OfficialRoomEntryType type =
            entry.Type == OfficialRoomEntryType.Room && entry.Room is null
                ? OfficialRoomEntryType.Folder
                : entry.Type;

        packet
            .WriteInteger(entry.Index)
            .WriteString(entry.PopupCaption)
            .WriteString(entry.PopupDescription)
            .WriteInteger(entry.ShowDetails ? 1 : 0)
            .WriteString(entry.PictureText)
            .WriteString(entry.PictureRef)
            .WriteInteger(entry.FolderId)
            .WriteInteger(entry.UserCount)
            .WriteInteger((int)type);

        switch (type)
        {
            case OfficialRoomEntryType.Tag:
                packet.WriteString(entry.Tag);
                break;

            case OfficialRoomEntryType.Room:
                RoomSettingsSerializer.Serialize(packet, entry.Room!);
                break;

            default:
                packet.WriteBoolean(entry.IsOpen);
                break;
        }
    }
}
