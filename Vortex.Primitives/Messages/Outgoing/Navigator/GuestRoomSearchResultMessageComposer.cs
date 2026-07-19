using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record GuestRoomSearchResultMessageComposer : IComposer
{
    [Id(0)]
    public object? Data { get; init; }
}
