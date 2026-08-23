using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record HabboGroupDeactivatedMessageComposer : IComposer
{
    [Id(0)]
    public required int GroupId { get; init; }
}
