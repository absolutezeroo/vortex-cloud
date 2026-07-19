using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record ForumThreadsMessageComposer : IComposer
{
    [Id(0)]
    public required ForumThreadsPageSnapshot Page { get; init; }
}
