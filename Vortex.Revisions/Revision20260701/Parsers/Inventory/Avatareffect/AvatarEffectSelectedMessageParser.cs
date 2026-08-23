using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Inventory.Avatareffect;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Avatareffect;

internal class AvatarEffectSelectedMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new AvatarEffectSelectedMessage { EffectType = packet.PopInt() };
}
