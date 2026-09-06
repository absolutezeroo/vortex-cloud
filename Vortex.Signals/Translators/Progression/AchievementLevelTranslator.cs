using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Achievement level-ups. The other progression system feeding this one.</summary>
public sealed class AchievementLevelTranslator : ISignalTranslator<AchievementLevelUpEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.AchievementLevel, [])];

    public ImmutableArray<ProgressSignal> Translate(AchievementLevelUpEvent e) =>
        [new(e.PlayerId.Value, SignalActions.AchievementLevel, 1, Target: null, [])];
}
