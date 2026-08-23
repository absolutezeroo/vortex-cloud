using Orleans;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// A loot box being opened, pushed to the room while the animation runs. The client reads the state
/// as a short and derives its own <c>start</c> and <c>finish</c> flags from it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RedeemNftLootBoxStateMessageComposer : IComposer
{
    [Id(0)]
    public required short State { get; init; }

    /// <summary>Whose box it is — the avatar the animation plays on.</summary>
    [Id(1)]
    public required int OpenerAvatarId { get; init; }

    /// <summary>What came out. Always written, even mid-animation: the client builds the struct
    /// unconditionally, so omitting it truncates the packet.</summary>
    [Id(2)]
    public required CollectibleProductItemSnapshot Reward { get; init; }
}
