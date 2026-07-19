using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.Primitives.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record RoomSettingsDataEventMessageComposer : IComposer
{
    [Id(0)]
    public required RoomSnapshot Settings { get; init; }
}
