using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Chat;

[GenerateSerializer, Immutable]
public sealed record RemainingMutePeriodMessageComposer : IComposer
{
    [Id(0)]
    public required int SecondsRemaining { get; init; }
}
