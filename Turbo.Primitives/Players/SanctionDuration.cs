using System;

namespace Turbo.Primitives.Players;

/// <summary>
/// Shared convention for any time-bound sanction (account ban, trading lock, ...): <c>null</c> means
/// "not sanctioned", <see cref="PermanentThreshold"/> or later means "permanent" — there's no
/// separate "is this permanent" column, just a far-future sentinel, so every call site that creates
/// or displays a sanction expiry needs to agree on it.
/// </summary>
public static class SanctionDuration
{
    // Whole seconds, no fractional ticks — MySQL datetime(6) only keeps microsecond precision, and
    // DateTime.MaxValue's 7-digit tick fraction risks a rounding surprise at the exact top of the
    // column's representable range.
    public static readonly DateTime Permanent = new(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    private static readonly DateTime PermanentThreshold = new(
        9000,
        1,
        1,
        0,
        0,
        0,
        DateTimeKind.Utc
    );

    public static bool IsPermanent(DateTime bannedUntil) => bannedUntil >= PermanentThreshold;
}
