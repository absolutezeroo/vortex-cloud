using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Trading;

[GenerateSerializer, Immutable]
public sealed record TradingOpenEventMessageComposer : IComposer
{
    /// <summary>Room-instance id of the trade initiator. Every id in the trade protocol is a room
    /// object id (not a web id), matching what <c>OpenTrading</c> sends, so the client resolves it
    /// to a name through its in-room user data manager.</summary>
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required bool UserCanTrade { get; init; }

    [Id(2)]
    public required int OtherUserId { get; init; }

    [Id(3)]
    public required bool OtherUserCanTrade { get; init; }
}
