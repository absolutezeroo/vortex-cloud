using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Avatareffect;

public record AvatarEffectActivatedMessage : IMessageEvent
{
    public required int EffectType { get; init; }
}
