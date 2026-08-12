using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

/// <summary>
/// The account safety lock was engaged or released (header 3913).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1891/_SafeCls_2001.as): a single int. The client
/// treats 0 as unlocked and 1 as locked.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record AccountSafetyLockStatusChangeMessageComposer : IComposer
{
    [Id(0)]
    public required int Status { get; init; }
}
