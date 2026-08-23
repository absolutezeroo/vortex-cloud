using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Inventory.Avatareffect;

public record AvatarEffectActivatedMessage : IMessageEvent
{
    public required int EffectType { get; init; }
}
