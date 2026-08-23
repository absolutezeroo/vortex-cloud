using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Notifications;

/// <summary>
/// A hotel-wide broadcast (header 334).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1810/_SafeCls_2614.as): a single string.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record HabboBroadcastMessageComposer : IComposer
{
    [Id(0)]
    public required string MessageText { get; init; }
}
