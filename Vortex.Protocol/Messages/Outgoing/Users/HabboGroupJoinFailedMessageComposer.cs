using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record HabboGroupJoinFailedMessageComposer : IComposer
{
    [Id(0)]
    public required int Reason { get; init; }
}
