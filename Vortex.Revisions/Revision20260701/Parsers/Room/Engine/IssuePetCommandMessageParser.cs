using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class IssuePetCommandMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new IssuePetCommandMessage { PetId = packet.PopInt(), CommandId = packet.PopInt() };
}
