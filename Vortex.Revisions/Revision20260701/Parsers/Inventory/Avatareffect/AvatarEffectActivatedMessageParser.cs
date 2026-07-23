using Vortex.Primitives.Messages.Incoming.Inventory.Avatareffect;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Avatareffect;

internal class AvatarEffectActivatedMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new AvatarEffectActivatedMessage { EffectType = packet.PopInt() };
}
