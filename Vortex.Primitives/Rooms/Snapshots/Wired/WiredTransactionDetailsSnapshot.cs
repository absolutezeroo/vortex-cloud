using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>How many of one kind of furniture a transaction moved.</summary>
/// <remarks>
/// Keyed by kind rather than by id, the way the details window renders it: the same furniture in
/// two states is two lines, and two of the same is one line reading "2x".
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTransactionItemCount
{
    [Id(0)]
    public required bool IsWallItem { get; init; }

    [Id(1)]
    public required int SpriteId { get; init; }

    /// <summary>Empty for anything that is not a poster.</summary>
    [Id(2)]
    public required string LegacyPosterId { get; init; }

    [Id(3)]
    public required int Count { get; init; }
}

/// <summary>Everything behind one row of the transaction log.</summary>
/// <remarks>
/// The summary line comes back with it — the window is opened from a row but reads the row again
/// from this message rather than from what it already had.
/// <para>
/// <see cref="IsIncompleteData"/> is load-bearing: a breakdown too long to send is cut, and the
/// flag is how the window knows to show a "+N more" cell instead of quietly under-reporting. The
/// count it prints is the row's own furni count minus what actually arrived, so the two have to
/// disagree honestly.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredTransactionDetailsSnapshot
{
    [Id(0)]
    public required WiredTransactionSnapshot Info { get; init; }

    /// <summary>The chests involved, as the player sees them — furniture ids, printed as a list.</summary>
    [Id(1)]
    public required ImmutableArray<int> ChestIds { get; init; }

    [Id(2)]
    public required ImmutableArray<WiredTransactionItemCount> Deposited { get; init; }

    [Id(3)]
    public required ImmutableArray<WiredTransactionItemCount> Withdrawn { get; init; }

    [Id(4)]
    public required bool IsIncompleteData { get; init; }
}
