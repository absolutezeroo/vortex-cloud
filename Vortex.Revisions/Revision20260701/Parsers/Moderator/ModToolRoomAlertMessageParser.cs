using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class ModToolRoomAlertMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        // _SafeCls_3239(param1:int, param2:String, param3:String). The third is pushed as an empty
        // string by the only call site and is read purely to consume it off the buffer.
        int actionType = packet.PopInt();
        string message = packet.PopString();

        packet.PopString();

        return new ModToolRoomAlertMessage { ActionType = actionType, Message = message };
    }
}
