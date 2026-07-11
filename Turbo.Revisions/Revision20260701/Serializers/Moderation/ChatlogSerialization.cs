using Turbo.Primitives.Moderation;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Moderation;

/// <summary>
/// Shared wire writer for the WIN63 "chatlog block" shape (decoded from the client source as
/// class_4059: a record-type byte, a small typed context map, then the chat records), used by both
/// RoomChatlogEventMessageComposer and UserChatlogEventMessageComposer. Only the (roomId, roomName)
/// context keys are populated — the other keys the client supports (groupId/threadId/messageId)
/// belong to forum-post moderation, out of scope here.
/// </summary>
internal static class ChatlogSerialization
{
    private const byte RecordTypeRoom = 0;
    private const byte ContextValueTypeInt = 1;
    private const byte ContextValueTypeString = 2;

    public static void WriteBlock(IServerPacket packet, ChatlogBlockSnapshot block)
    {
        packet.WriteByte(RecordTypeRoom);

        packet.WriteShort(2);
        packet.WriteString("roomId").WriteByte(ContextValueTypeInt).WriteInteger(block.RoomId);
        packet
            .WriteString("roomName")
            .WriteByte(ContextValueTypeString)
            .WriteString(block.RoomName);

        packet.WriteShort((short)block.Records.Length);

        foreach (ChatlogRecordSnapshot record in block.Records)
        {
            packet
                .WriteString(record.TimeStampUtc.ToString("HH:mm"))
                .WriteInteger(record.ChatterId)
                .WriteString(record.ChatterName)
                .WriteString(record.Message)
                .WriteBoolean(false);
        }
    }
}
