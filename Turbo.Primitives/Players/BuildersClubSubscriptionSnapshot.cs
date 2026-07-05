using System;

namespace Turbo.Primitives.Players;

public readonly record struct BuildersClubSubscriptionSnapshot(
    bool IsActive,
    DateTime? ExpiresAt,
    int FurniLimit
);
