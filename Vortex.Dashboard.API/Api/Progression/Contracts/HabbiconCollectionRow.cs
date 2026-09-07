using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One collection, with its members.
/// </summary>
/// <param name="LocalizationKey">The key the client renders the name from, shown beside the code so
/// an operator does not have to guess what to add to the texts.</param>
/// <param name="RewardHabbiconId">The Habbicon completing the set awards, 0 when there is none.</param>
/// <param name="CompletedBy">How many players hold every entry.</param>
/// <param name="Habbicons">The entries, with the reward last when there is one.</param>
public sealed record HabbiconCollectionRow(
    int Id,
    string Code,
    string LocalizationKey,
    int SortOrder,
    bool Enabled,
    bool Hidden,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    string CampaignCode,
    int EntryCount,
    int RewardHabbiconId,
    string RewardCode,
    int CompletedBy,
    HabbiconSprite? Sprite,
    IReadOnlyList<HabbiconRow> Habbicons
);
