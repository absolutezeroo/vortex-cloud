using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record ForumDataMessageComposer : IComposer
{
    [Id(0)]
    public required ForumSnapshot Forum { get; init; }
}
