using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredAllVariableHoldersEventMessageComposer : IComposer
{
    [Id(0)]
    public required WiredVariableSnapshot VariableSnapshot { get; init; }

    [Id(1)]
    public required List<(RoomObjectId objectId, int value)> ObjectValues { get; init; }
}
