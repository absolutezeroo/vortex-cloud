using Vortex.Primitives.Messages.Incoming.Mysterybox;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.MysteryBox;

internal class MysteryBoxWaitingCanceledMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new MysteryBoxWaitingCanceledMessage();
}
