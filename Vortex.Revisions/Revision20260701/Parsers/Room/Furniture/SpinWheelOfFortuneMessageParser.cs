using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class SpinWheelOfFortuneMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SpinWheelOfFortuneMessage { ObjectId = packet.PopInt() };
}
