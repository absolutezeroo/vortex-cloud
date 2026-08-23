using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>One row of the transaction log, opened.</summary>
/// <remarks>
/// The id is a long on the wire, and the client sends nothing else: which chest and which room the
/// row belongs to are the server's to remember.
/// </remarks>
public record GetWiredTransactionDetailsMessage : IMessageEvent
{
    public required long TransactionId { get; init; }
}
