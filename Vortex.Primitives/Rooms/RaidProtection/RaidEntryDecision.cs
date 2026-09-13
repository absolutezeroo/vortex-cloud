using Orleans;

namespace Vortex.Primitives.Rooms.RaidProtection;

/// <summary>
/// Everything the entry path needs from the room about one arriving player, in one grain call.
/// </summary>
/// <remarks>
/// The two facts travel together because they are both asked on every single room entry. Splitting
/// them into <c>EvaluateEntry</c> and <c>CanManage</c> would double the room's busiest call for a
/// tidier signature, and a raid is precisely when that room is least able to afford it.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RaidEntryDecision
{
    [Id(0)]
    public required RaidEntryVerdict Verdict { get; init; }

    /// <summary>How long to ban for. Only meaningful when <see cref="Verdict" /> is a ban.</summary>
    [Id(1)]
    public required int BanDurationSeconds { get; init; }

    /// <summary>Whether this player gets the protection button once they are inside.</summary>
    [Id(2)]
    public required bool CanManage { get; init; }

    public static RaidEntryDecision Allowed(bool canManage) =>
        new()
        {
            Verdict = RaidEntryVerdict.Allow,
            BanDurationSeconds = 0,
            CanManage = canManage,
        };
}
