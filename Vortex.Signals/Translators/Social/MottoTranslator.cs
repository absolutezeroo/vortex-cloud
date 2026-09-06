using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Motto changes.</summary>
public sealed class MottoTranslator : ISignalTranslator<PlayerMottoChangedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ChangeMotto, [])];

    public ImmutableArray<ProgressSignal> Translate(PlayerMottoChangedEvent e) =>
        [new(e.PlayerId.Value, SignalActions.ChangeMotto, 1, Target: null, [])];
}
