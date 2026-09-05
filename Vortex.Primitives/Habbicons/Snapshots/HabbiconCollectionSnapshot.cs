using System;
using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Habbicons.Snapshots;

/// <summary>
/// One collection: a named set of Habbicons plus, optionally, the bonus Habbicon completing the set
/// unlocks. <see cref="Entries"/> excludes the bonus.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record HabbiconCollectionSnapshot
{
    [Id(0)]
    public required int CollectionId { get; init; }

    /// <summary>Localization stem: the client renders <c>habbicon_collection_{Code}_name</c>.</summary>
    [Id(1)]
    public required string Code { get; init; }

    [Id(2)]
    public required int SortOrder { get; init; }

    [Id(3)]
    public required bool Enabled { get; init; }

    /// <summary>Hidden collections are served to a player who owns something in them, and to nobody else.</summary>
    [Id(4)]
    public required bool Hidden { get; init; }

    [Id(5)]
    public DateTime? AvailableFromUtc { get; init; }

    [Id(6)]
    public DateTime? AvailableUntilUtc { get; init; }

    /// <summary>Price to buy every entry still missing, in credits. 0 = the set is not sold whole.</summary>
    [Id(7)]
    public required int PriceCredits { get; init; }

    [Id(8)]
    public required int PriceActivityPoints { get; init; }

    [Id(9)]
    public required int ActivityPointType { get; init; }

    /// <summary>The ordinary entries, in display order. Never contains the bonus Habbicon.</summary>
    [Id(10)]
    public required ImmutableArray<HabbiconDefinitionSnapshot> Entries { get; init; }

    /// <summary>The bonus Habbicon completing the set unlocks, or null when the set has none.</summary>
    [Id(11)]
    public HabbiconDefinitionSnapshot? RewardHabbicon { get; init; }

    /// <summary>Free-form campaign tag, so a campaign can find its own collections.</summary>
    [Id(12)]
    public string CampaignCode { get; init; } = string.Empty;

    public bool IsAvailableAt(DateTime nowUtc) =>
        Enabled
        && (AvailableFromUtc is null || AvailableFromUtc <= nowUtc)
        && (AvailableUntilUtc is null || AvailableUntilUtc > nowUtc);

    public bool HasPrice => PriceCredits > 0 || PriceActivityPoints > 0;
}
