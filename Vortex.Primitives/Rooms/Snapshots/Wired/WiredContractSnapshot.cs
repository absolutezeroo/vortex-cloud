using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>
/// One wired contract, as its own editor writes it and reads it back.
/// </summary>
/// <remarks>
/// The three types share a head and differ in the tail: a payment contract carries a mode, the text
/// the player is shown and a layout; a reward contract an earnings category, a pop-up flag and its
/// text; a trade contract nothing beyond the rules. Reading the wrong tail does not throw, it
/// consumes the next message — so the type is honoured exactly on both sides.
/// <para>
/// The rules here are the <em>definition</em> only. What multiplies them is not the contract's: it
/// comes from the box that offers it, which is why <see cref="TradeContract"/> carries a mode and
/// this does not.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredContractSnapshot
{
    /// <summary>The contract furni.</summary>
    [Id(0)]
    public required int ContractId { get; init; }

    /// <summary>0 payment, 1 trade, 2 reward.</summary>
    [Id(1)]
    public required int ContractType { get; init; }

    /// <summary>The ways to pay. Null is a side that was never written, which is not the same as empty.</summary>
    [Id(2)]
    public ImmutableArray<TradeContractRule>? YouGiveRules { get; init; }

    [Id(3)]
    public TradeContractRule? YouGetRule { get; init; }

    [Id(4)]
    public int PaymentMode { get; init; }

    [Id(5)]
    public string ReceiveText { get; init; } = string.Empty;

    [Id(6)]
    public string LayoutType { get; init; } = string.Empty;

    [Id(7)]
    public int RewardCategory { get; init; }

    [Id(8)]
    public bool ShowDialog { get; init; }

    [Id(9)]
    public string RewardText { get; init; } = string.Empty;
}
