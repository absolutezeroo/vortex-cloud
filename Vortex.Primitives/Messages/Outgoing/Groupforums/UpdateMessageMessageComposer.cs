using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record UpdateMessageMessageComposer : IComposer
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required int ThreadId { get; init; }

    [Id(2)]
    public required ForumPostSnapshot Post { get; init; }
}
