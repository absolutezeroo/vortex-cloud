using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Callforhelp;

[GenerateSerializer, Immutable]
public sealed record CfhTopicsInitMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<CfhCategorySnapshot> Categories { get; init; }
}
