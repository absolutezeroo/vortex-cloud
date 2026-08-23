using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class CallForHelpFromPhotoMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        // reportPhoto(photoId, topicId, roomId, authorId, furniId) reorders its arguments on the
        // way out: the topic lands fourth on the wire, not second.
        string photoId = packet.PopString();
        int roomId = packet.PopInt();
        int photoAuthorId = packet.PopInt();
        int topicId = packet.PopInt();
        int furniId = packet.PopInt();

        return new CallForHelpFromPhotoMessage
        {
            PhotoId = photoId,
            RoomId = roomId,
            PhotoAuthorId = photoAuthorId,
            TopicId = topicId,
            FurniId = furniId,
        };
    }
}
