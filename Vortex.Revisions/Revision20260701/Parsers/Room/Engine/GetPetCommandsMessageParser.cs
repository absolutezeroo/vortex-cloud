using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class GetPetCommandsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetPetCommandsMessage { PetId = packet.PopInt() };
}
