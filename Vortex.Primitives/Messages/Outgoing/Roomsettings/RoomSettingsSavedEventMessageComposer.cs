using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record RoomSettingsSavedEventMessageComposer : IComposer
{
    [Id(0)]
    public required RoomId RoomId { get; init; }
}
