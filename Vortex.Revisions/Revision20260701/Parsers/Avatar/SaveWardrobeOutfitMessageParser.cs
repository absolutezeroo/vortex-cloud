using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Avatar;

namespace Vortex.Revisions.Revision20260701.Parsers.Avatar;

internal class SaveWardrobeOutfitMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SaveWardrobeOutfitMessage
        {
            SlotId = packet.PopInt(),
            Figure = packet.PopString(),
            Gender = packet.PopString(),
        };
}
