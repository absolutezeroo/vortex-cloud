using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildMembershipsMessageComposer : IComposer
{
    [Id(0)]
    public required List<GuildInfoSnapshot> Memberships { get; init; }
}
