using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Chat;

[GenerateSerializer, Immutable]
public sealed record FloodControlMessageComposer : IComposer
{
    [Id(0)]
    public required int Seconds { get; init; }
}
