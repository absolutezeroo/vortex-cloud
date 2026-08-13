using System;
using Orleans;

namespace Vortex.Primitives.Moderation;

/// <summary>
/// One entry in a player's own sanction history.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PlayerSanctionSnapshot
{
    /// <summary>
    /// The name the client matches on. It recognises ALERT, MUTE and BAN_PERMANENT and treats
    /// anything else as a generic timed ban, so this is a wire token and not a label to translate.
    /// </summary>
    [Id(0)]
    public required string TypeName { get; init; }

    [Id(1)]
    public required string Reason { get; init; }

    /// <summary>Whole hours the sanction runs for, zero when it does not expire.</summary>
    [Id(2)]
    public required int DurationHours { get; init; }

    /// <summary>Whole hours left, zero once it has been served.</summary>
    [Id(3)]
    public required int HoursLeft { get; init; }

    [Id(4)]
    public required DateTime ExpiresAtUtc { get; init; }

    /// <summary>Whether the sanction is still in force — the client turns this into the "you are on
    /// probation" block rather than a plain history line.</summary>
    [Id(5)]
    public required bool IsActive { get; init; }
}
