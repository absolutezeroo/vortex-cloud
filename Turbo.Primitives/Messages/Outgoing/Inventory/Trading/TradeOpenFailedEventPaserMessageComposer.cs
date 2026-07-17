using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Trading;

[GenerateSerializer, Immutable]
public sealed record TradeOpenFailedEventPaserMessageComposer : IComposer
{
    /// <summary>Why the trade could not be opened. Reasons <c>7</c>/<c>8</c> trigger a generic alert
    /// on the client; other values show the templated "could not trade with {name}" popup.</summary>
    [Id(0)]
    public required int Reason { get; init; }

    [Id(1)]
    public required string OtherUserName { get; init; }
}
