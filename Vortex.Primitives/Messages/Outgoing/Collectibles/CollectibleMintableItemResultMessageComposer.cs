using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

/// <summary>
/// How a mint attempt ended. One short. The client stores the code without interpreting it, so
/// only the refusal below is safe to send -- a guessed success code would have the interface
/// report a token that was never minted.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CollectibleMintableItemResultMessageComposer : IComposer
{
    [Id(0)]
    public required short Status { get; init; }

    /// <summary>No chain to mint against.</summary>
    public const short NotAvailable = 1;
}
