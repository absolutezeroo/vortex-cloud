using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>One transaction's full breakdown.</summary>
[GenerateSerializer, Immutable]
public sealed record WiredTransactionDetailsMessageComposer : IComposer
{
    [Id(0)]
    public required WiredTransactionDetailsSnapshot Details { get; init; }
}
