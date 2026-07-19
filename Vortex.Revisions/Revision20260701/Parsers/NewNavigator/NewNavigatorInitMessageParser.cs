using Vortex.Primitives.Messages.Incoming.NewNavigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.NewNavigator;

internal class NewNavigatorInitMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new NewNavigatorInitMessage();
}
