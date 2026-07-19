using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredUserPermanentVariablesComposer : IComposer
{
    [Id(0)]
    public required WiredPermanentVariablesSnapshot Snapshot { get; init; }
}
