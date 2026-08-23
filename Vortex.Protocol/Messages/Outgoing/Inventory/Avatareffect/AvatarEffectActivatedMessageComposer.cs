using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Avatareffect;

[GenerateSerializer, Immutable]
public sealed record AvatarEffectActivatedMessageComposer : IComposer
{
    [Id(0)]
    public required int Type { get; init; }

    [Id(1)]
    public required int Duration { get; init; }

    [Id(2)]
    public required bool IsPermanent { get; init; }
}
