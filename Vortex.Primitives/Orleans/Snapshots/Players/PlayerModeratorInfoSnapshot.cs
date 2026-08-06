using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Players;

/// <summary>
/// The account facts the staff mod tool's user card shows. Deliberately separate from
/// <see cref="PlayerExtendedProfileSnapshot"/>: this one carries the email address and the sanction
/// history, which no ordinary player may ever see.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PlayerModeratorInfoSnapshot
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required string UserName { get; init; }

    [Id(2)]
    public required string Figure { get; init; }

    [Id(3)]
    public required int RegistrationAgeInMinutes { get; init; }

    /// <summary>0 when they have never logged in since the <c>last_login_at</c> column existed.
    /// Online status is not part of this snapshot — it lives on the presence grain, so the caller
    /// resolves it and overrides this to 0 for a player who is connected right now.</summary>
    [Id(4)]
    public required int MinutesSinceLastLogin { get; init; }

    /// <summary>Reports this player filed.</summary>
    [Id(6)]
    public int CfhCount { get; init; }

    /// <summary>Reports this player filed that were closed as abusive.</summary>
    [Id(7)]
    public int AbusiveCfhCount { get; init; }

    /// <summary>Reports against this player that were closed with a sanction.</summary>
    [Id(8)]
    public int CautionCount { get; init; }

    [Id(9)]
    public int BanCount { get; init; }

    [Id(10)]
    public int TradingLockCount { get; init; }

    [Id(11)]
    public string TradingExpiryDate { get; init; } = string.Empty;

    [Id(12)]
    public string PrimaryEmailAddress { get; init; } = string.Empty;

    /// <summary>The account behind the player, so staff can see alt accounts sharing a login.</summary>
    [Id(13)]
    public int IdentityId { get; init; }
}
