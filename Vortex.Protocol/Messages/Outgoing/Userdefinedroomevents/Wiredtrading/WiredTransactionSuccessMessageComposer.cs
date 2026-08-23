using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// A contract went through, and what the player got for it.
/// </summary>
/// <remarks>
/// The type is the contract's own — 0 payment, 1 trade, 2 reward — and only a reward one carries a
/// block after it. The client reads that block on the type <em>and</em> on there being bytes left,
/// so sending nothing after a type 2 is legal and shows a bare notification; sending a block after
/// any other type is not, and would be read as the next message.
/// <para>
/// <see cref="Reward"/> is what the contract promised rather than a tally of what moved. They are
/// the same thing by construction: a settlement that cannot pay in full does not complete.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTransactionSuccessMessageComposer : IComposer
{
    [Id(0)]
    public required int TransactionSuccessTypeId { get; init; }

    /// <summary>Null for anything but a reward contract, and for one that promises nothing.</summary>
    [Id(1)]
    public TradeContractRule? Reward { get; init; }

    [Id(2)]
    public string RewardText { get; init; } = string.Empty;

    /// <summary>Whether the notification opens itself rather than waiting to be clicked.</summary>
    [Id(3)]
    public bool OpenByDefault { get; init; }
}
