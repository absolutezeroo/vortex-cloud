using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// What became of a chest upgrade purchase.
/// </summary>
/// <remarks>
/// Zero is success; anything else is looked up as <c>wiredchests.upgrade.result.error.N</c> and
/// shown as the reason. The texts name them: 1 feature disabled, 2 at maximum capacity, 3 safety
/// locked, 5 insufficient credits, 6 insufficient diamonds, 7 not the owner, 10 a starter chest.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredChestUpgradeResultMessageComposer : IComposer
{
    [Id(0)]
    public required int ChestId { get; init; }

    [Id(1)]
    public required int ResultCode { get; init; }
}
