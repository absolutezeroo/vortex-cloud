using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Inventory.Snapshots;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>Where a chest deposit currently stands, as the room hands it back to the player.</summary>
/// <remarks>
/// One shape serves the whole flow — staking, un-staking and accepting all answer with it — because
/// the client redraws the same screen from it every time. <see cref="Completed"/> is the one thing
/// that changes what the caller sends: the table state otherwise, the trade's end when set.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredDepositSnapshot
{
    [Id(0)]
    public required int ChestId { get; init; }

    /// <summary>What the player has put on the table, in the shape the trade screen draws.</summary>
    [Id(1)]
    public required ImmutableArray<FurnitureItemSnapshot> Items { get; init; }

    /// <summary>Whether the accept button may be live. The client does not decide this.</summary>
    [Id(2)]
    public required bool CanAccept { get; init; }

    /// <summary>Set once the items have actually moved into the chest.</summary>
    [Id(3)]
    public required bool Completed { get; init; }

    /// <summary>
    /// What a contract hands back, drawn on the other side of the table.
    /// </summary>
    /// <remarks>
    /// Empty for a plain deposit, which gives nothing back and shows an empty far side. For a
    /// contract it is what the chest can pay <em>right now</em>, so a shop that has run out says so
    /// by showing less rather than by accepting into a refusal.
    /// </remarks>
    [Id(4)]
    public ImmutableArray<FurnitureItemSnapshot> RewardItems { get; init; } = [];

    [Id(5)]
    public int RewardCredits { get; init; }
}
