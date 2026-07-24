using System;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredPolicy
{
    public WiredConditionModeType ConditionMode { get; set; }

    /// <summary>The N used by the counting condition modes ("less than / exactly / more than N").</summary>
    public int ConditionCompareValue { get; set; }
    public WiredEffectModeType EffectMode { get; set; }
    public WiredAnimationModeType AnimationMode { get; set; }
    public int AnimationTimeMs { get; set; }
    public TimeSpan Delay { get; set; }
    public bool ShortCircuitOnFirstEffectSuccess { get; set; }
}
