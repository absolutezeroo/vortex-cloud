using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Avatareffect;

[GenerateSerializer, Immutable]
public sealed record AvatarEffectsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<AvatarEffectSnapshot> Effects { get; init; }
}
