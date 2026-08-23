using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredUserVariablesListComposer : IComposer
{
    [Id(0)]
    public required WiredVariableOwnersPageSnapshot Page { get; init; }
}
