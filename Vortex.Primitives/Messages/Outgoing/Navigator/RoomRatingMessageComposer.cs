using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record RoomRatingMessageComposer : IComposer
{
    [Id(0)]
    public int Rating { get; init; }

    [Id(1)]
    public bool CanRate { get; init; }
}
