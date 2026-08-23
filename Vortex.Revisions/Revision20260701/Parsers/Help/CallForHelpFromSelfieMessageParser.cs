using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class CallForHelpFromSelfieMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        // Same argument shuffle as the photo variant: reportSelfie(url, message, roomId, authorId,
        // furniId) puts the message fourth.
        string url = packet.PopString();
        int roomId = packet.PopInt();
        int photoAuthorId = packet.PopInt();
        string message = packet.PopString();
        int furniId = packet.PopInt();

        return new CallForHelpFromSelfieMessage
        {
            Url = url,
            RoomId = roomId,
            PhotoAuthorId = photoAuthorId,
            Message = message,
            FurniId = furniId,
        };
    }
}
