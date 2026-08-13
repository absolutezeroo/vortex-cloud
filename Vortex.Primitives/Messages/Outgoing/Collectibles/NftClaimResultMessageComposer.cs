using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

/// <summary>
/// What came of a claim attempt. A single short, and nothing in the client explains the codes — it
/// stores the value and re-renders — so this hotel sends only <see cref="NftClaimStatus.Failed"/>
/// rather than guessing at a success code it cannot honour.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftClaimResultMessageComposer : IComposer
{
    [Id(0)]
    public required NftClaimStatus Status { get; init; }
}

/// <summary>
/// Deliberately minimal. Zero is the only value whose meaning is safe to assume — every other code
/// would be an invention, and a wrongly-optimistic one would have the tab report a claim that never
/// happened.
/// </summary>
public enum NftClaimStatus : short
{
    Failed = 0,
}
