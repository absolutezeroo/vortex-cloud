using System.Collections.Generic;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Wired.Variable;

namespace Turbo.Revisions.Revision20260112.Parsers.Userdefinedroomevents.Wiredmenu;

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
