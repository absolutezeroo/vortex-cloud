using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record HabboGroupBadgesMessageComposer : IComposer
{
    [Id(0)]
    public required List<GroupBadgeSnapshot> Badges { get; init; }
}
