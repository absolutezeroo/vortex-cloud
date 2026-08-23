using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredAllVariablesHashEventMessageComposer : IComposer
{
    [Id(0)]
    public required WiredVariableHash AllVariablesHash { get; init; }
}
