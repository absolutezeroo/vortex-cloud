using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Avatareffect;

[GenerateSerializer, Immutable]
public sealed record AvatarEffectExpiredMessageComposer : IComposer
{
    [Id(0)]
    public required int Type { get; init; }
}
