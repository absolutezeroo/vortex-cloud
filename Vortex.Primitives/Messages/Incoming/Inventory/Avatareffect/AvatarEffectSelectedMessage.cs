using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Avatareffect;

public record AvatarEffectSelectedMessage : IMessageEvent
{
    public required int EffectType { get; init; }
}
