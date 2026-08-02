using System;
using System.Collections.Generic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Hydration-time repair of a wired box's persisted <see cref="IWiredData.IntParams"/> list.
/// <para>
/// <c>FurnitureWiredLogic.TryNormalizeIntParams</c> rejects a whole list, which is right for a
/// client update — a malformed packet must never overwrite a good configuration. It is wrong for
/// hydration: a rejected list is left as persisted, so a box can come up with fewer entries than
/// it has rules, and every leaf that reads <c>GetIntParam(i)</c> then throws on each fire. Live
/// that showed up as a per-tick "malformed params" warning storm from freshly placed,
/// never-configured boxes, whose persisted list is empty.
/// </para>
/// </summary>
public static class WiredIntParamRepair
{
    /// <summary>
    /// Returns a list that always has one entry per fixed rule: a slot the persisted list is
    /// missing, or holds an out-of-range value for, falls back to that rule's default. Tail
    /// entries are sanitized in place rather than dropped — leaves that read the tail as a bit
    /// mask (e.g. the neighborhood selector) depend on their positions — and are capped at
    /// <paramref name="maxIntParams"/>.
    /// </summary>
    public static List<int> Repair(
        IReadOnlyList<IWiredParamRule> fixedRules,
        IWiredParamRule? tailRule,
        int maxIntParams,
        IReadOnlyList<int> persisted
    )
    {
        List<int> repaired = new(fixedRules.Count);

        for (int i = 0; i < fixedRules.Count; i++)
        {
            IWiredParamRule rule = fixedRules[i];

            repaired.Add(rule.Sanitize(i < persisted.Count ? persisted[i] : rule.DefaultValue));
        }

        if (tailRule is null)
        {
            return repaired;
        }

        int max = Math.Max(fixedRules.Count, maxIntParams);

        for (int i = fixedRules.Count; i < persisted.Count && repaired.Count < max; i++)
        {
            repaired.Add(tailRule.Sanitize(persisted[i]));
        }

        return repaired;
    }
}
