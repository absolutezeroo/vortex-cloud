using Vortex.Primitives.Messages.Incoming.Register;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Register;

internal class UpdateFigureDataMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UpdateFigureDataMessage { Gender = packet.PopString(), Figure = packet.PopString() };
}
