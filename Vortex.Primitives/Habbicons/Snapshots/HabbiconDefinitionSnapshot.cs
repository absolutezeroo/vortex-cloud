using System;
using Orleans;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Primitives.Habbicons.Snapshots;

/// <summary>
/// One Habbicon definition: everything true about it regardless of who is looking. The artwork is
/// not here — the client resolves it from its own <c>habbicons.json</c> manifest by id, and
/// localizes the name as <c>habbicon_{Code}_name</c>.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record HabbiconDefinitionSnapshot
{
    /// <summary>The client-facing id. Also the key into the client's asset manifest.</summary>
    [Id(0)]
    public required int HabbiconId { get; init; }

    /// <summary>Asset/localization stem, e.g. <c>duck_01</c>.</summary>
    [Id(1)]
    public required string Code { get; init; }

    [Id(2)]
    public required int CollectionId { get; init; }

    [Id(3)]
    public required int SortOrder { get; init; }

    /// <summary>
    /// A collection's bonus Habbicon rather than one of its ordinary entries. Bonus Habbicons do not
    /// count towards their own collection's completion — that would make it uncompletable.
    /// </summary>
    [Id(4)]
    public required bool IsCollectionReward { get; init; }

    /// <summary>Price in credits; 0 means not individually purchasable for credits.</summary>
    [Id(5)]
    public required int PriceCredits { get; init; }

    /// <summary>Price in the activity-point currency named by <see cref="ActivityPointType"/>.</summary>
    [Id(6)]
    public required int PriceActivityPoints { get; init; }

    /// <summary>
    /// Which activity-point currency <see cref="PriceActivityPoints"/> is denominated in (0 =
    /// duckets, 5 = diamonds, …), matching <see cref="CurrencyKind.ActivityPointType"/>.
    /// </summary>
    [Id(7)]
    public required int ActivityPointType { get; init; }

    [Id(8)]
    public required bool Enabled { get; init; }

    /// <summary>Inclusive availability window. Null on either side means unbounded.</summary>
    [Id(9)]
    public DateTime? AvailableFromUtc { get; init; }

    [Id(10)]
    public DateTime? AvailableUntilUtc { get; init; }

    /// <summary>True when the definition is inside its availability window at <paramref name="nowUtc"/>.</summary>
    public bool IsAvailableAt(DateTime nowUtc) =>
        Enabled
        && (AvailableFromUtc is null || AvailableFromUtc <= nowUtc)
        && (AvailableUntilUtc is null || AvailableUntilUtc > nowUtc);

    /// <summary>Whether this Habbicon can be bought on its own at all.</summary>
    public bool HasPrice => PriceCredits > 0 || PriceActivityPoints > 0;
}
