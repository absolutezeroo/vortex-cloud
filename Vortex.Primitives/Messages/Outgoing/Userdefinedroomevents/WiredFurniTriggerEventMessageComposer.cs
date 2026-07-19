using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;

[GenerateSerializer, Immutable]
public sealed record WiredFurniTriggerEventMessageComposer : IComposer
{
    [Id(0)]
    public required WiredDataSnapshot WiredData { get; init; }
}
