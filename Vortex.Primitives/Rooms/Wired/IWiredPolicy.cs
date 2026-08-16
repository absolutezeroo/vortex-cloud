using System;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredPolicy
{
    public WiredConditionModeType ConditionMode { get; set; }

    /// <summary>The N used by the counting condition modes ("less than / exactly / more than N").</summary>
    public int ConditionCompareValue { get; set; }
    public WiredEffectModeType EffectMode { get; set; }

    /// <summary>How many effects the random mode draws. One unless the random add-on asks for
    /// more.</summary>
    public int EffectPickCount { get; set; }

    /// <summary>How many past firings the random mode avoids repeating, from the same add-on's
    /// second slider. Zero lets it draw the same effect twice in a row.</summary>
    public int EffectAvoidRecentExecutions { get; set; }
    public WiredAnimationModeType AnimationMode { get; set; }
    public int AnimationTimeMs { get; set; }
    public TimeSpan Delay { get; set; }
    public bool ShortCircuitOnFirstEffectSuccess { get; set; }
}
