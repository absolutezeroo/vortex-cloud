using System;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredPolicy
{
    public WiredConditionModeType ConditionMode { get; }
    public WiredEffectModeType EffectMode { get; }
    public WiredAnimationModeType AnimationMode { get; set; }
    public int AnimationTimeMs { get; set; }
    public TimeSpan Delay { get; set; }
    public bool ShortCircuitOnFirstEffectSuccess { get; set; }
}
