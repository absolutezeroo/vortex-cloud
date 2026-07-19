using System.Collections.Generic;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredGetAllVariablesDiffsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        List<(WiredVariableId Id, WiredVariableHash Hash)> variables =
            new List<(WiredVariableId Id, WiredVariableHash Hash)>();
        int count = packet.PopInt();

        while (count > 0)
        {
            variables.Add(
                (WiredVariableId.Parse(packet.PopString()), new WiredVariableHash(packet.PopInt()))
            );

            count--;
        }

        return new WiredGetAllVariablesDiffsMessage { VariableIdsWithHash = variables };
    }
}
