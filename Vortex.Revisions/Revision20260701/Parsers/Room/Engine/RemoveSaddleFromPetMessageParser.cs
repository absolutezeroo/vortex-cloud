using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class RemoveSaddleFromPetMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RemoveSaddleFromPetMessage();
}
