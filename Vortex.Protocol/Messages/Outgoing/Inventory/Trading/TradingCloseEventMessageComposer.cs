using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Trading;

[GenerateSerializer, Immutable]
public sealed record TradingCloseEventMessageComposer : IComposer
{
    /// <summary>Room-instance id of the user who caused the close (the canceller). The client shows
    /// "the other user cancelled" when this differs from its own id.</summary>
    [Id(0)]
    public required int UserId { get; init; }

    /// <summary>Close reason. <c>1</c> = commit/server error (special alert); any other value = a
    /// user cancelled the trade.</summary>
    [Id(1)]
    public required int Reason { get; init; }
}
