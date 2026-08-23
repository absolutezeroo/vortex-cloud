using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// What came of a claim attempt. A single short whose sign matters: the client reads success as
/// <c>resultCode == 0</c>, announces "claiming succeeded" and clears the claims list on it — and on
/// anything else shows a failure toast with the code printed into it. This hotel only ever refuses
/// (there is no chain to claim from), so the one code it sends must not be the success one.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftClaimResultMessageComposer : IComposer
{
    [Id(0)]
    public required NftClaimStatus Status { get; init; }
}

/// <summary>
/// Zero is the success code, and the client keys everything on it: on success it announces the
/// reward and <em>clears the list</em>. Every failure code beyond "not zero" would be an invention,
/// so there is exactly one refusal value.
/// </summary>
public enum NftClaimStatus : short
{
    Succeeded = 0,
    Failed = 1,
}
