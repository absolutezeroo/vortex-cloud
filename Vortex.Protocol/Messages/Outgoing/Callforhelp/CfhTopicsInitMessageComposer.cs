using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Callforhelp;

[GenerateSerializer, Immutable]
public sealed record CfhTopicsInitMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<CfhCategorySnapshot> Categories { get; init; }
}
