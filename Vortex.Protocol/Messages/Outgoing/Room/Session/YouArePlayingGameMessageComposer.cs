using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Session;

[GenerateSerializer, Immutable]
public sealed record YouArePlayingGameMessageComposer : IComposer
{
    [Id(0)]
    public required bool IsPlaying { get; init; }
}
