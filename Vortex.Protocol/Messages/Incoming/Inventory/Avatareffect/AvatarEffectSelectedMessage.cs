using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Inventory.Avatareffect;

public record AvatarEffectSelectedMessage : IMessageEvent
{
    public required int EffectType { get; init; }
}
