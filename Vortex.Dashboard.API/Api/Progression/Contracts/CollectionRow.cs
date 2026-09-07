using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One collection and the items in it.
/// </summary>
/// <param name="Status">The stored number, not a name: the table keeps this as an int and
/// nothing on either side maps it to an enum.</param>
/// <param name="Completable">Has items and every one of them resolves. A collection that is not
/// completable is one no player can ever finish, whatever they collect.</param>
public sealed record CollectionRow(
    int Id,
    string CollectionCode,
    string Name,
    int BoostScore,
    DateTime? ReleasedAt,
    DateTime? SnapshotAt,
    int Status,
    string? RewardProductCode,
    string? BonusProductCode,
    int ItemCount,
    int TotalScore,
    int UnresolvedItems,
    bool Completable,
    IReadOnlyList<CollectionItemRow> Items
);
