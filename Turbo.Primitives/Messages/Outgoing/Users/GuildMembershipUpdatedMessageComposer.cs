using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildMembershipUpdatedMessageComposer : IComposer
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required GroupMemberSnapshot Member { get; init; }
}
