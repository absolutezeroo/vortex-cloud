using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One Habbicon, and how many players hold it.</summary>
public sealed record HabbiconRow(
    HabbiconSprite? Sprite,
    int Id,
    string Code,
    string LocalizationKey,
    int CollectionId,
    int SortOrder,
    bool IsCollectionReward,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    bool Enabled,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    int Owners
);
