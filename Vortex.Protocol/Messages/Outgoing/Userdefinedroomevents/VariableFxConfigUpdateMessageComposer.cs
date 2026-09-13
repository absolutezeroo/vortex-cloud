using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

/// <summary>
/// Declares the Variable FX displays a room has, so the client knows how to draw them before any
/// value arrives.
/// </summary>
/// <remarks>
/// Sent as a whole set rather than one at a time: the client replaces its config table from what
/// this carries, so a config left out of a later send is a config the room no longer has.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VariableFxConfigUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<WiredVariableFxConfigSnapshot> Configs { get; init; }
}
