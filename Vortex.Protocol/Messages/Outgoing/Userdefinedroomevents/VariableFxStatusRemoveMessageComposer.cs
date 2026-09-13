using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

/// <summary>
/// Takes Variable FX displays off the entities they were hanging on.
/// </summary>
/// <remarks>
/// Keyed by the same composite string a status update carries, and nothing else: the client splits
/// it back into config, variable, entity kind and entity id. Build them with
/// <see cref="Vortex.Primitives.Rooms.Snapshots.Wired.WiredVariableFxKey"/> — a key assembled any
/// other way removes a display nobody has.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VariableFxStatusRemoveMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<string> StatusKeys { get; init; }
}
