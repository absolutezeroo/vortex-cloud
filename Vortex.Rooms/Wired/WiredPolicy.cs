using System;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Wired;

public sealed class WiredPolicy : IWiredPolicy
{
    public WiredConditionModeType ConditionMode { get; set; } = WiredConditionModeType.All;
    public WiredEffectModeType EffectMode { get; set; } = WiredEffectModeType.All;
    public WiredAnimationModeType AnimationMode { get; set; } = WiredAnimationModeType.Smooth;
    public int AnimationTimeMs { get; set; } = 50;
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;
    public bool ShortCircuitOnFirstEffectSuccess { get; set; }
}
