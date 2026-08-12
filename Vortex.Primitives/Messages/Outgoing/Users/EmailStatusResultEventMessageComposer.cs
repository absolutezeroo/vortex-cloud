using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

/// <summary>
/// The address on the account and what may be done with it (header 2343).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1891/_SafeCls_1994.as): a string then two
/// booleans.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record EmailStatusResultEventMessageComposer : IComposer
{
    [Id(0)]
    public required string Email { get; init; }

    [Id(1)]
    public required bool IsVerified { get; init; }

    /// <summary>Whether the player is allowed to change the address at all.</summary>
    [Id(2)]
    public required bool AllowChange { get; init; }
}
