using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Help;

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

        // Trailing pair (`_SafeCls_2702` = [p1..p7]).
        string reporterName = packet.PopString();
        string reporterEmail = packet.PopString();

        return new CallForHelpFromPhotoMessage
        {
            PhotoId = photoId,
            RoomId = roomId,
            PhotoAuthorId = photoAuthorId,
            TopicId = topicId,
            FurniId = furniId,
            ReporterName = reporterName,
            ReporterEmail = reporterEmail,
        };
    }
}
