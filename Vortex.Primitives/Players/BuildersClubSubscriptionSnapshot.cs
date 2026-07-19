using System;

namespace Vortex.Primitives.Players;

public readonly record struct BuildersClubSubscriptionSnapshot(
    bool IsActive,
    DateTime? ExpiresAt,
    int FurniLimit
);
