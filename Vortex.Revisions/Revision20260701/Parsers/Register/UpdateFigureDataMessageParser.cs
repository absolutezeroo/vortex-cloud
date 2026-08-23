using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Register;

namespace Vortex.Revisions.Revision20260701.Parsers.Register;

internal class UpdateFigureDataMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UpdateFigureDataMessage { Gender = packet.PopString(), Figure = packet.PopString() };
}
