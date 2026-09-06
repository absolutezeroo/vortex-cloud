using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Claimed a finished daily task. The other progression system, feeding this one.</summary>
public sealed class DailyTaskClaimedTranslator : ISignalTranslator<DailyTaskClaimedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ClaimDailyTask, [])];

    public ImmutableArray<ProgressSignal> Translate(DailyTaskClaimedEvent e) =>
        [new(e.PlayerId.Value, SignalActions.ClaimDailyTask, 1, Target: null, [])];
}
