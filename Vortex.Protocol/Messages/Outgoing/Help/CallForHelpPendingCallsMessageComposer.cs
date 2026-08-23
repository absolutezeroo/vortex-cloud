using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// The reports this player already has open. The client asks for these before letting them file a
/// new one and shows them instead if any come back, so an empty list is what unlocks the report
/// dialog — this is a gate, not a notification.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CallForHelpPendingCallsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<CfhPendingCallSnapshot> Calls { get; init; }
}
