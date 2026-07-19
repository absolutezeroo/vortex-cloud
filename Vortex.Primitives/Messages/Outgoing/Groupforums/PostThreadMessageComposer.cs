using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record PostThreadMessageComposer : IComposer
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required ForumThreadSnapshot Thread { get; init; }
}
