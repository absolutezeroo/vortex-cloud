using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

/// <summary>
/// What each Variable FX display reads now.
/// </summary>
/// <remarks>
/// Batched on purpose: a room where a wired variable drives twenty health bars changes all twenty
/// on one tick, and the client is built to take them together.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VariableFxStatusUpdateMessageComposer : IComposer
{
    /// <summary>
    /// Forces every entry in this message to count as a first value.
    /// </summary>
    /// <remarks>
    /// The client ORs this into each entry's own flag, so it is how a room answers "draw all of
    /// these from scratch" — someone entering the room, or a display that was just declared —
    /// without stamping the flag on every entry.
    /// </remarks>
    [Id(0)]
    public bool ForceInitialize { get; init; }

    [Id(1)]
    public required ImmutableArray<WiredVariableFxStatusSnapshot> Statuses { get; init; }
}
