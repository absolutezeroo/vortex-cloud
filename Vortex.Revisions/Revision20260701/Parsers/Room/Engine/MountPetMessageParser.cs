using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class MountPetMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new MountPetMessage { PetId = packet.PopInt(), Mount = packet.PopBoolean() };
}
