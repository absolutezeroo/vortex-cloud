using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// What a currency chest holds.
/// </summary>
/// <remarks>
/// <see cref="IsUpdate"/> is not decoration: false is the answer to an open request and switches the
/// client's screen to open, true is a live change to a screen already open. Sending true first
/// leaves the chest closed on the player's side, showing nothing.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredChestCoinsMessageComposer : IComposer
{
    [Id(0)]
    public required int ChestId { get; init; }

    [Id(1)]
    public required int Coins { get; init; }

    [Id(2)]
    public required bool IsUpdate { get; init; }
}
