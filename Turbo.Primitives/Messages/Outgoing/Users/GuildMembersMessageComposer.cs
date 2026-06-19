using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildMembersMessageComposer : IComposer
{
    [Id(0)]
    public required GroupMembersPageSnapshot Page { get; init; }
}
