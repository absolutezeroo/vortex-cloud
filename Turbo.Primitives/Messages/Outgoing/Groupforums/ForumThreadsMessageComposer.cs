using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record ForumThreadsMessageComposer : IComposer
{
    [Id(0)]
    public required ForumThreadsPageSnapshot Page { get; init; }
}
