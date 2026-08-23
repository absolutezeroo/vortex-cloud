using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public record WiredGetAllVariablesDiffsMessage : IMessageEvent
{
    [Id(0)]
    public required List<(
        WiredVariableId Id,
        WiredVariableHash Hash
    )> VariableIdsWithHash { get; init; } = [];
}
