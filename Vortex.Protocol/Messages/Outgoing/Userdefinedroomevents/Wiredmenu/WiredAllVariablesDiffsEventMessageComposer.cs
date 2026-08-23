using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredAllVariablesDiffsEventMessageComposer : IComposer
{
    [Id(0)]
    public required WiredVariableHash AllVariablesHash { get; init; }

    [Id(1)]
    public required bool IsLastChunk { get; init; }

    [Id(2)]
    public required List<WiredVariableId> RemovedVariableIds { get; init; }

    [Id(3)]
    public required List<WiredVariableSnapshot> AddedOrUpdated { get; init; }
}
