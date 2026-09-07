using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One Habbicon a player holds.
/// </summary>
/// <param name="Code">Empty when the Habbicon row behind it is gone -- the ownership survives the
/// definition, which is exactly the case an operator opens this page to see.</param>
public sealed record PlayerHabbiconRow(
    int HabbiconId,
    string Code,
    int CollectionId,
    string State,
    string Source,
    DateTime AcquiredAt,
    DateTime? LastUsedAt
);
