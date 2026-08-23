using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class ModToolPreferencesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int windowX = packet.PopInt();
        int windowY = packet.PopInt();
        int windowWidth = packet.PopInt();
        int windowHeight = packet.PopInt();

        return new ModToolPreferencesMessage
        {
            WindowX = windowX,
            WindowY = windowY,
            WindowWidth = windowWidth,
            WindowHeight = windowHeight,
        };
    }
}
