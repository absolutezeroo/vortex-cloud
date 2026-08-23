using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record BlockListMessageComposer : IComposer
{
    [Id(0)]
    public required List<int> BlockedPlayerIds { get; init; }
}
