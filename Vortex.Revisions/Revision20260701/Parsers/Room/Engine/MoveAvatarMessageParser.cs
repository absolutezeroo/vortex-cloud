using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class MoveAvatarMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int x = packet.PopInt();
        int y = packet.PopInt();

        // Optional third field, so a sender that predates it still parses: Vortex.LoadGen's
        // SyntheticClient sends the two-int form, and so does any older client build.
        int? zKey = packet.Remaining >= sizeof(int) ? packet.PopInt() : null;

        return new MoveAvatarMessage
        {
            TargetX = x,
            TargetY = y,
            TargetZKey = zKey,
        };
    }
}
