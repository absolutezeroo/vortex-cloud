using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record ConsoleMessageHistoryMessageComposer : IComposer
{
    [Id(0)]
    public required int ChatId { get; init; }

    [Id(1)]
    public required List<MessageHistoryEntrySnapshot> Messages { get; init; }
}
