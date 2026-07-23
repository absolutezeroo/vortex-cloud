using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Vortex.Primitives.Inventory.Snapshots;

namespace Vortex.Players.Effects;

/// <summary>One owned effect instance, decoupled from EF so the inventory projection is unit-testable
/// without a database.</summary>
public readonly record struct PlayerEffectRow(
    int EffectId,
    int SubType,
    int TotalDuration,
    DateTime? ActivatedAt,
    bool IsSelected
);

/// <summary>Pure projection from owned effect rows to the client <c>AvatarEffects</c> payload. One entry
/// per distinct <c>(effectId, subType)</c> — mirroring Habbo's grouped inventory list, where several
/// copies of the same effect collapse into a single entry with a stack count.</summary>
public static class AvatarEffectListBuilder
{
    public static ImmutableArray<AvatarEffectSnapshot> Build(
        IEnumerable<PlayerEffectRow> rows,
        DateTime nowUtc
    )
    {
        return rows.GroupBy(r => (r.EffectId, r.SubType))
            .OrderBy(g => g.Key.EffectId)
            .ThenBy(g => g.Key.SubType)
            .Select(g =>
            {
                int totalDuration = g.Max(r => r.TotalDuration);
                bool isPermanent = totalDuration <= 0;
                int inactiveInInventory = g.Count(r => r.ActivatedAt is null);

                int secondsLeftIfActive = 0;
                List<PlayerEffectRow> activeRows = g.Where(r => r.ActivatedAt is not null).ToList();

                if (!isPermanent && activeRows.Count > 0)
                {
                    PlayerEffectRow active = activeRows
                        .OrderByDescending(r => r.ActivatedAt)
                        .First();
                    secondsLeftIfActive = RemainingSeconds(active, nowUtc);
                }

                return new AvatarEffectSnapshot
                {
                    Type = g.Key.EffectId,
                    SubType = g.Key.SubType,
                    Duration = totalDuration,
                    InactiveEffectsInInventory = inactiveInInventory,
                    SecondsLeftIfActive = secondsLeftIfActive,
                    IsPermanent = isPermanent,
                };
            })
            .ToImmutableArray();
    }

    /// <summary>Seconds remaining on an activated, timed effect (0 for permanent, inactive, or elapsed).</summary>
    public static int RemainingSeconds(PlayerEffectRow row, DateTime nowUtc)
    {
        if (row.TotalDuration <= 0 || row.ActivatedAt is null)
        {
            return 0;
        }

        int elapsed = (int)Math.Floor((nowUtc - row.ActivatedAt.Value).TotalSeconds);
        int remaining = row.TotalDuration - elapsed;

        return remaining > 0 ? remaining : 0;
    }
}
