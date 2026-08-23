using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Notifications;

[GenerateSerializer, Immutable]
public sealed record ActivityPointsMessageComposer : IComposer
{
    [Id(0)]
    public required Dictionary<int, int> PointsByCategoryId { get; init; }
}
