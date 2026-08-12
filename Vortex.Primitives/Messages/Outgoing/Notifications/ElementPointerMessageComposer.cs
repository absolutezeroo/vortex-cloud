using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Notifications;

/// <summary>
/// Points the UI hint arrow at a named element (header 1807).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1810/_SafeCls_4146.as): a single string. An empty
/// key hides the current hint.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ElementPointerMessageComposer : IComposer
{
    [Id(0)]
    public required string Key { get; init; }
}
