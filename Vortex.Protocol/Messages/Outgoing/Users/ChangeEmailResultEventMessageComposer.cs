using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

/// <summary>
/// Whether an e-mail change was accepted (header 2050).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1891/_SafeCls_4028.as): a single int.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ChangeEmailResultEventMessageComposer : IComposer
{
    [Id(0)]
    public required int Result { get; init; }
}
