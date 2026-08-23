using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>The trade is off, and why.</summary>
/// <remarks>
/// The client hands <see cref="TransactionFailureTypeId"/> to its own error text table; 0 is the
/// plain "cancelled" it uses when the player closed the screen themselves.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTradeCancelledMessageComposer : IComposer
{
    [Id(0)]
    public required int TransactionFailureTypeId { get; init; }
}
