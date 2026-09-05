using System;
using System.Collections.Generic;

namespace Vortex.Primitives.Habbicons.Admin;

/// <summary>The outcome of one content write, with the row id when it made or changed one.</summary>
public sealed record HabbiconAdminResult(bool Success, int? Id, string? ErrorCode)
{
    public static HabbiconAdminResult Ok(int id) => new(true, id, null);

    public static HabbiconAdminResult Fail(string errorCode) => new(false, null, errorCode);
}

/// <summary>Create/update spec for a Habbicon collection. Its members are managed separately.</summary>
public sealed record HabbiconCollectionSpec(
    string Code,
    int SortOrder,
    bool Enabled,
    bool Hidden,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    string CampaignCode
);

/// <summary>
/// Create/update spec for one Habbicon.
/// </summary>
/// <param name="IsCollectionReward">
/// Makes this the set's bonus. A collection may have at most one, and the service refuses a second
/// rather than letting the catalog pick one arbitrarily.
/// </param>
public sealed record HabbiconSpec(
    string Code,
    int CollectionId,
    int SortOrder,
    bool IsCollectionReward,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    bool Enabled,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil
);

/// <summary>What an operator sees when they look up one player's Habbicons.</summary>
public sealed record PlayerHabbiconAdminRow(
    int HabbiconId,
    string Code,
    int CollectionId,
    HabbiconState State,
    HabbiconSource Source,
    DateTime AcquiredAt,
    DateTime? LastUsedAt
);

/// <summary>Ownership and completion counts for one collection, for the content list.</summary>
public sealed record HabbiconCollectionStats(
    int CollectionId,
    string Code,
    int EntryCount,
    bool HasReward,
    int OwnersOfAnyEntry,
    int PlayersWhoCompleted
);

/// <summary>
/// A content problem the validator found. Reported rather than thrown so an operator sees every
/// problem at once instead of the first one.
/// </summary>
public sealed record HabbiconContentProblem(string Code, string Detail);

/// <summary>The validator's whole answer.</summary>
public sealed record HabbiconContentReport(IReadOnlyList<HabbiconContentProblem> Problems)
{
    public bool IsValid => Problems.Count == 0;
}
