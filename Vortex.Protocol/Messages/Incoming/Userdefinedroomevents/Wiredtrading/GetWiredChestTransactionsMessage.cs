using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>One chest's history, a page at a time.</summary>
public record GetWiredChestTransactionsMessage : IMessageEvent
{
    public required int ChestId { get; init; }

    public required int PageSize { get; init; }

    public required int Page { get; init; }
}
