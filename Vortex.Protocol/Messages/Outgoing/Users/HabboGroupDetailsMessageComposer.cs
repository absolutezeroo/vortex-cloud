using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record HabboGroupDetailsMessageComposer : IComposer
{
    [Id(0)]
    public required GroupDetailsSnapshot Details { get; init; }
}
