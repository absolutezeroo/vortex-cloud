using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Outgoing.Room.Session;

[GenerateSerializer, Immutable]
public sealed record CantConnectMessageComposer : IComposer
{
    [Id(0)]
    public required RoomConnectionErrorType ErrorType { get; init; }

    [Id(1)]
    public string? AdditionalInfo { get; init; }
}
