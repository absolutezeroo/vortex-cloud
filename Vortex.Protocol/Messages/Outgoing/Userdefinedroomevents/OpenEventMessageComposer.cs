using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;

[GenerateSerializer, Immutable]
public sealed record OpenEventMessageComposer : IComposer
{
    [Id(0)]
    public required RoomObjectId ItemId { get; init; }
}
