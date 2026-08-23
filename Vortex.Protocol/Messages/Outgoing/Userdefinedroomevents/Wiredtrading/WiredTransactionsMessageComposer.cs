using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// A page of transaction log, for a chest or for a room.
/// </summary>
/// <remarks>
/// One message answers both requests; the page itself says which list it belongs to.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTransactionsMessageComposer : IComposer
{
    [Id(0)]
    public required WiredTransactionsSnapshot Page { get; init; }
}
