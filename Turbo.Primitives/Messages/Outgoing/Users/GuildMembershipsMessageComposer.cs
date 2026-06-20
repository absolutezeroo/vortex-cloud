using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildMembershipsMessageComposer : IComposer
{
    [Id(0)]
    public required List<GuildInfoSnapshot> Memberships { get; init; }
}
