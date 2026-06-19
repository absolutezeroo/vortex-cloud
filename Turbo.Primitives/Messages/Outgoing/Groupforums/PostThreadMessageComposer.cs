using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record PostThreadMessageComposer : IComposer
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required ForumThreadSnapshot Thread { get; init; }
}
