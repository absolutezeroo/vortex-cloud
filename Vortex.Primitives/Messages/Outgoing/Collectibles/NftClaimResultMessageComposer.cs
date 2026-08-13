using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

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
/// Deliberately minimal. Zero is the success code and the only one the client names; every failure
/// code beyond "not zero" would be an invention, so this hotel sends exactly one refusal value.
/// </summary>
public enum NftClaimStatus : short
{
    Failed = 1,
}
