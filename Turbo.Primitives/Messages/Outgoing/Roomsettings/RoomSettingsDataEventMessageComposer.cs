using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans.Snapshots.Room;

namespace Turbo.Primitives.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record RoomSettingsDataEventMessageComposer : IComposer
{
    [Id(0)]
    public required RoomSnapshot Settings { get; init; }
}
